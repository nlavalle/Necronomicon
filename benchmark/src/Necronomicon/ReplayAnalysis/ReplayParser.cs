using BenchmarkDotNet.Attributes;
using necronomicon;
using necronomicon.model;
using necronomicon.model.engine;
using Steam.Protos.Dota2;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Benchmarks.Necronomicon.ReplayAnalysis;

[MemoryDiagnoser]
public class ReplayParser
{
    public string FileName { get; set; }
    public string GameId { get; private set; }
    public StringTables StringTables { get; private set; }
    public ReplayStringTables ReplayStringTables = new ReplayStringTables();
    public ReplayPacketEntities? ReplayPacketEntities;
    public ReplaySendTables ReplaySendTables = new ReplaySendTables();
    private uint GameBuild;
    private int ClassIdSize;
    private int FrameTick;
    private Dictionary<int, Class> ClassesById;
    private Dictionary<string, Class> ClassesByName = new Dictionary<string, Class>();
    private Dictionary<string, Serializer> Serializers = new Dictionary<string, Serializer>();

    // Replay parsed data
    private string TestFileName = string.Empty;
    private Stopwatch stopwatch = new Stopwatch();
    private Class? _playerResourceClass;
    private PlayerResourceLookup[] _playerResourceLookup = new PlayerResourceLookup[10];
    private HeroEntity[] _heroEntities = new HeroEntity[10];
    public List<HeroPosition> HeroPositions = new List<HeroPosition>();
    public List<ItemUse> ItemUses = new List<ItemUse>();
    public List<Death> Deaths = new List<Death>();
    public ReplayParser()
    {
        FileName = string.Empty;
        GameId = string.Empty;
        StringTables = new StringTables();
        ClassIdSize = 0;
        ClassesById = new Dictionary<int, Class>();

        var cwd = Environment.CurrentDirectory;
        var root = Path.GetPathRoot(cwd) ?? throw new Exception();
        var relative = Path.GetRelativePath(root, cwd);

        var paths = relative.Split(Path.DirectorySeparatorChar);
        var index = Array.LastIndexOf(paths, "benchmark");
        TestFileName = Path.Combine(root, Path.Combine(paths[0..(index + 1)]), "test_replay.dem");
    }

    [Benchmark]
    public void FullReplayAnalysis()
    {
        using var file = File.Open(TestFileName, FileMode.Open, FileAccess.Read);
        StartAsync(file).Wait();
    }

    public async Task StartAsync(Stream stream)
    {
        Console.WriteLine("Stopwatch started");
        stopwatch.Start();
        await Replay.ParseFileAsync(stream, OnEngineKnown);
    }

    private void OnEngineKnown(Replay.File file)
    {
        file.RegisterCompletionTask(OnCompletion);

        switch (file.MagicValue)
        {
            case EngineMagicHeader.SOURCE_2:
                file.ParseAsSource2Replay(CheckHeaderFrame);
                break;
            default:
                throw new Exception();
        }
    }

    private void CheckHeaderFrame(Source2Replay.Frame frame)
    {
        if (frame.FrameCommand != EDemoCommands.DemFileHeader)
            throw new Exception();

        frame.RegisterCallbackForData(HandleFileHeader);
    }

    private void HandleFileHeader(Source2Replay.FrameData frame)
    {
        Debug.Assert(frame.FrameCommand == EDemoCommands.DemFileHeader);

        CDemoFileHeader fileHeader = frame.GetAsProtobuf<CDemoFileHeader>();
        string gameId = string.Empty;
        if (fileHeader.HasGame)
        {
            gameId = fileHeader.Game;
        }
        else if (fileHeader.HasGameDirectory)
        {
            var matchPattern = Regex.Match(fileHeader.GameDirectory, @".*[/\\](\w+)$");
            if (matchPattern.Success)
            {
                gameId = matchPattern.Groups[1].Value;
            }
        }

        if (gameId == string.Empty)
        {
            throw new NecronomiconException("Unable to get Game ID");
        }
        switch (gameId)
        {
            case "csgo":
                throw new NotImplementedException();
            case "dota":
                GameId = "dota";
                break;
            case "citadel":
                GameId = "citadel";
                break;
            default:
                throw new NecronomiconException($"Unexpected new game type: {gameId}");
        }

        frame.ChangeCallbackForNextFrame(CheckTheFrame);
    }

    private void CheckTheFrame(Source2Replay.Frame frame)
    {
        FrameTick = frame.FrameTick;
        switch (frame.FrameCommand)
        {
            case EDemoCommands.DemPacket:
            case EDemoCommands.DemSignonPacket:
                frame.RegisterCallbackForData(HandlePacket);
                break;
            case EDemoCommands.DemSendTables:
                frame.RegisterCallbackForData(HandleSendTables);
                break;
            case EDemoCommands.DemFullPacket:
                frame.RegisterCallbackForData(HandleFullPacket);
                break;
            case EDemoCommands.DemClassInfo:
                frame.RegisterCallbackForData(HandleClassInfo);
                break;
            case EDemoCommands.DemFileInfo:
                frame.RegisterCallbackForData(HandleFileInfo);
                break;
            default:
                break;
        }
    }

    private void HandleFileInfo(Source2Replay.FrameData frame)
    {
        var fileInfo = frame.GetAsProtobuf<CDemoFileInfo>();
    }

    private void HandleSendTables(Source2Replay.FrameData frame)
    {
        var sendTables = frame.GetAsProtobuf<CDemoSendTables>();
        ReplaySendTables.OnCDemoSendTables(sendTables, GameBuild, Serializers);
    }

    private void HandleClassInfo(Source2Replay.FrameData frame)
    {
        var classInfo = frame.GetAsProtobuf<CDemoClassInfo>();
        foreach (var infoClass in classInfo.Classes)
        {
            var classId = infoClass.ClassId;
            var networkName = infoClass.NetworkName;

            if (Serializers.ContainsKey(networkName))
            {
                var newClass = new Class(classId, networkName, Serializers[networkName]);
                ReplayStringTables.ClassInfos[classId] = new ClassInfo(classId, networkName, "serializer");
                ClassesById[classId] = newClass;
                ClassesByName[networkName] = newClass;
            }
            else
            {
                throw new NecronomiconException($"Missing the serializer for: {networkName}");
            }
        }

        ClassIdSize = (int)((uint)BitOperations.Log2((uint)ClassesById.Count) + 1);

        ReplayStringTables.UpdateInstanceBaseline();
        ReplayPacketEntities = new ReplayPacketEntities(ClassesById, ClassIdSize, ReplayStringTables);
        ReplayPacketEntities?.Callbacks.Add(EntityUpdated);

        ClassInfoComplete();
    }

    private void HandlePacket(Source2Replay.FrameData frame)
    {
        var packet = frame.GetAsProtobuf<CDemoPacket>();
        Source2ReplayPacket.ProcessPacketMessages(packet.Data.Span, frame.FrameTick, HandleMessage);
    }

    private void HandleFullPacket(Source2Replay.FrameData frame)
    {
        var packet = frame.GetAsProtobuf<CDemoFullPacket>();
        Source2ReplayPacket.ProcessPacketMessages(packet.Packet.Data.Span, frame.FrameTick, HandleMessage);
    }

    private void HandleMessage(Source2ReplayPacket.Message message)
    {
        switch (message.MessageType)
        {
            case (int)SVC_Messages.SvcServerInfo:
                message.RegisterCallbackForData(HandleServerInfo);
                break;
            case (int)SVC_Messages.SvcCreateStringTable:
                message.RegisterCallbackForData(HandleCreateStringTable);
                break;
            case (int)SVC_Messages.SvcUpdateStringTable:
                message.RegisterCallbackForData(HandleUpdateStringTable);
                break;
            case (int)SVC_Messages.SvcPacketEntities:
                message.RegisterCallbackForData(HandlePacketEntities);
                break;
            case (int)EDotaUserMessages.DotaUmCombatLogDataHltv:
                message.RegisterCallbackForData(HandleCombatLogEntry);
                break;
        }
    }

    private void HandleServerInfo(Source2ReplayPacket.MessageData messageData)
    {
        var serverInfo = messageData.GetAsProtobuf<CSVCMsg_ServerInfo>();
        ClassIdSize = (int)((uint)BitOperations.Log2((uint)serverInfo.MaxClasses) + 1);

        // Get game build info
        var matchPattern = Regex.Match(serverInfo.GameDir, @"[dota|citadel]_v(\d+)");
        if (matchPattern.Groups.Count < 2)
        {
            throw new NecronomiconException($"unable to determine game build from {serverInfo.GameDir}");
        }

        GameBuild = uint.Parse(matchPattern.Groups[1].Value);
    }

    private void HandleCreateStringTable(Source2ReplayPacket.MessageData messageData)
    {
        var createStringTable = messageData.GetAsProtobuf<CSVCMsg_CreateStringTable>();
        ReplayStringTables.OnCSVCMsgCreateStringTable(createStringTable);
    }

    private void HandleUpdateStringTable(Source2ReplayPacket.MessageData messageData)
    {
        var updateStringTable = messageData.GetAsProtobuf<CSVCMsg_UpdateStringTable>();
        ReplayStringTables.OnCSVCMsgUpdateStringTable(updateStringTable);
    }

    private void HandlePacketEntities(Source2ReplayPacket.MessageData messageData)
    {
        var packetEntities = messageData.GetAsProtobuf<CSVCMsg_PacketEntities>();
        ReplayPacketEntities?.OnCSVCMsgPacketEntities(packetEntities);
    }

    private void HandleCombatLogEntry(Source2ReplayPacket.MessageData messageData)
    {
        var combatLogEntry = messageData.GetAsProtobuf<CMsgDOTACombatLogEntry>();

        var combatLogStringTableIndex = ReplayStringTables.StringTables.NameIndex.FirstOrDefault(t => t.Key == "CombatLogNames");
        if (ReplayStringTables.StringTables.Tables.Count > combatLogStringTableIndex.Value)
        {
            var combatLogNames = ReplayStringTables.StringTables.Tables.ElementAt(combatLogStringTableIndex.Value);
            switch (combatLogEntry.Type)
            {
                case DOTA_COMBATLOG_TYPES.DotaCombatlogDeath:
                    var death = new Death(combatLogEntry, messageData.Tick, combatLogNames);
                    Deaths.Add(death);
                    break;
                case DOTA_COMBATLOG_TYPES.DotaCombatlogItem:
                    var itemUse = new ItemUse(combatLogEntry, messageData.Tick, combatLogNames);
                    ItemUses.Add(itemUse);
                    break;
            }
        }
    }

    private ValueTask OnCompletion()
    {
        Debug.WriteLine("I'm done");
        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}");
        return ValueTask.CompletedTask;
    }

    private void ClassInfoComplete()
    {
        _playerResourceClass = ClassesByName.First(ci => ci.Key == "CDOTA_PlayerResource").Value;
        for (int i = 0; i < 10; i++)
        {
            _playerResourceLookup[i] = new PlayerResourceLookup(_playerResourceClass, i);
            _heroEntities[i] = new HeroEntity();
        }
    }

    private async Task EntityUpdated(List<(Entity Entity, EntityOp EntityOp)> EntitiesUpdated)
    {
        var playerEntities = EntitiesUpdated.FirstOrDefault(eu => eu.Entity.EntityClass == _playerResourceClass);
        if (playerEntities.Entity != null)
        {
            for (int i = 0; i < 10; i++)
            {
                var playerHeroEntity = playerEntities.Entity.State.Get(_playerResourceLookup[i].SelectedHeroFp);
                if (playerHeroEntity != null)
                {
                    _heroEntities[i].SelectedHeroEntityId = (ulong)playerHeroEntity;
                    _heroEntities[i].HeroEntityIndex = (int)((ulong)playerHeroEntity & (1 << 14) - 1);
                }
            }
        }

        for (int i = 0; i < 10; i++)
        {
            var heroEntity = EntitiesUpdated.FirstOrDefault(eu => eu.Entity.Index == _heroEntities[i].HeroEntityIndex);
            if (heroEntity.Entity != null)
            {
                // Debug.WriteLine($"Player {i} - Class - {heroEntity.Entity.EntityClass.Name}");
                var cBodyComponentSerializer = heroEntity.Entity.EntityClass.Serializer.Fields.FirstOrDefault(f => f.VarName == "CBodyComponent");
                if (cBodyComponentSerializer != null)
                {
                    var cBodyComponentIndex = heroEntity.Entity.EntityClass.Serializer.Fields.IndexOf(cBodyComponentSerializer);
                    FieldState? cBodyComponent = (FieldState?)heroEntity.Entity.State.Get(cBodyComponentIndex);
                    var componentDictionary = BodyComponentDecoder(cBodyComponent, cBodyComponentSerializer.Serializer);
                    HeroPosition newPosition = new HeroPosition(heroEntity.Entity.EntityClass.Name, FrameTick, componentDictionary!);
                    HeroPositions.Add(newPosition);
                }
            }
        }

        await Task.CompletedTask;
    }

    private class PlayerResourceLookup
    {
        public readonly FieldPath SelectedHeroFp = new FieldPath();
        public PlayerResourceLookup(Class PlayerResourceClass, int PlayerIndex)
        {
            var teamData = PlayerResourceClass.Serializer.Fields.FirstOrDefault(f => f.VarName == "m_vecPlayerTeamData");
            if (teamData?.Serializer != null)
            {
                var teamDataIndex = PlayerResourceClass.Serializer.Fields.IndexOf(teamData);
                var selectedHeroes = teamData.Serializer.Fields.FirstOrDefault(f => f.VarName == "m_hSelectedHero");
                Debug.Assert(selectedHeroes != null);
                var selectedHeroIndex = teamData.Serializer.Fields.IndexOf(selectedHeroes);
                SelectedHeroFp.Path = [teamDataIndex, PlayerIndex, selectedHeroIndex];
                SelectedHeroFp.Last = 2;
            }
        }
    }

    private class HeroEntity
    {
        public ulong SelectedHeroEntityId;
        public int HeroEntityIndex;
        public HeroEntity() { }
    }

    private Dictionary<string, object?> BodyComponentDecoder(FieldState? components, Serializer? serializer)
    {
        var result = new Dictionary<string, object?>();
        if (components == null || serializer == null) return result;

        for (int i = 0; i < serializer.Fields.Count; i++)
        {
            Field field = serializer.Fields[i];
            // FieldDecoder fieldDecoder = FieldDecoders.FindDecoder(field);
            // var rawObject = components.Get(i);
            result[field.VarName] = components.Get(i);
        }
        return result;
    }
}