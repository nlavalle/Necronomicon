using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using necronomicon;
using necronomicon.model;
using necronomicon.model.dem;
using necronomicon.model.engine;

namespace necronomicon_test;

public class PositionTests
{
    private Necronomicon? _parser;
    private DemClassInfo? _classInfo;
    private SvcPacketEntities? _packetEntities;
    private Class? _playerResourceClass;
    private PlayerResourceLookup[] _playerResourceLookup = new PlayerResourceLookup[10];
    private HeroEntity[] _heroEntities = new HeroEntity[10];
    private List<HeroPosition> _heroPositions = new List<HeroPosition>();

    [Fact]
    public async Task PositionTest()
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();

        string path = Path.GetFullPath(@"test_replay.dem");
        _parser = new Necronomicon(path);
        DemPackets packets = new DemPackets(_parser);
        DemSendTables sendTables = new DemSendTables(_parser);
        _classInfo = new DemClassInfo(_parser);
        _classInfo.Callbacks.Add(ClassInfoComplete);
        _packetEntities = new SvcPacketEntities(_parser);
        _packetEntities.Callbacks.Add(EntityUpdated);

        SvcStringTable stringTable = new SvcStringTable(_parser);
        _parser.Parse();

        stopwatch.Stop();

        var commandCount = packets._embeddedMessages
            .SelectMany(em => em.Commands)
            .GroupBy(em => em)
            .OrderBy(grp => grp.Key)
            .Select(grp => (Value: grp.Key, Count: grp.Count()))
            .ToList();
        Debug.WriteLine($"Total time to execute: {stopwatch.Elapsed}");
        Debug.WriteLine(packets._embeddedMessages.Count);

        var ordered = _heroPositions.OrderBy(hp => hp.FrameTick).ThenBy(hp => hp.HeroName).ToList();
        using var writer = new StreamWriter("./position_output.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true // Pretty print
        };

        string json = JsonSerializer.Serialize(ordered, options);
        writer.WriteLine(json);

        await Task.CompletedTask;
    }

    private async Task ClassInfoComplete()
    {
        if (_classInfo == null) return;
        _playerResourceClass = _classInfo.Classes.First(ci => ci.Key == "CDOTA_PlayerResource").Value;
        for (int i = 0; i < 10; i++)
        {
            _playerResourceLookup[i] = new PlayerResourceLookup(_playerResourceClass, i);
            _heroEntities[i] = new HeroEntity();
        }
        await Task.CompletedTask;
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
                    HeroPosition newPosition = new HeroPosition(heroEntity.Entity.EntityClass.Name, _parser!.FrameTick, componentDictionary!);
                    _heroPositions.Add(newPosition);
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

    private class HeroPosition
    {
        [JsonPropertyName("hero_name")]
        public string HeroName { get; set; } = string.Empty;

        [JsonPropertyName("frame_tick")]
        public int FrameTick { get; set; }

        [JsonPropertyName("cell_x")]
        public ulong CellX { get; set; }

        [JsonPropertyName("cell_y")]
        public ulong CellY { get; set; }

        [JsonPropertyName("cell_z")]
        public ulong CellZ { get; set; }

        [JsonPropertyName("vec_x")]
        public float VecX { get; set; }

        [JsonPropertyName("vec_y")]
        public float VecY { get; set; }

        [JsonPropertyName("vec_z")]
        public float VecZ { get; set; }
        public HeroPosition(string name, int frameTick, Dictionary<string, object> cBodyComponentDictionary)
        {
            HeroName = name;
            FrameTick = frameTick;

            CellX = (ulong)(cBodyComponentDictionary["m_cellX"] ?? 0);
            CellY = (ulong)(cBodyComponentDictionary["m_cellY"] ?? 0);
            CellZ = (ulong)(cBodyComponentDictionary["m_cellZ"] ?? 0);
            VecX = (float)(cBodyComponentDictionary["m_vecX"] ?? 0);
            VecY = (float)(cBodyComponentDictionary["m_vecY"] ?? 0);
            VecZ = (float)(cBodyComponentDictionary["m_vecZ"] ?? 0);
        }
    }
}
