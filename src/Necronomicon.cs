using System.Diagnostics;
using System.Text.RegularExpressions;
using necronomicon.model;
using necronomicon.model.engine;
using necronomicon.model.frames;
using necronomicon.source;
using Steam.Protos.Dota2;

namespace necronomicon;

public class Necronomicon
{
    public NecronomiconCallbacks Callbacks { get; } = new();
    public Dictionary<string, Serializer> Serializers;
    public Dictionary<int, ClassInfo> ClassInfos;
    public Dictionary<int, Class> ClassesById;
    public Dictionary<string, Class> ClassesByName;
    public Dictionary<int, Entity> Entities;
    public StringTables StringTables;
    public Dictionary<int, byte[]> ClassBaselines;
    public int EntityFullPackets;
    public uint ClassIdSize;
    public uint GameBuild;
    private readonly InputStreamSource _inputStreamSource;
    private HashSet<FrameSkeleton> _frameSkeletons;
    public Necronomicon(string path)
    {
        _inputStreamSource = new InputStreamSource(path);
        _frameSkeletons = new HashSet<FrameSkeleton>();
        ClassInfos = new Dictionary<int, ClassInfo>();
        ClassesById = new Dictionary<int, Class>();
        ClassesByName = new Dictionary<string, Class>();
        Serializers = new Dictionary<string, Serializer>();
        Entities = new Dictionary<int, Entity>();
        StringTables = new StringTables();
        ClassBaselines = new Dictionary<int, byte[]>();
        EntityFullPackets = 0;
        ClassIdSize = 0;
        GameBuild = 0;
    }
    public void Parse()
    {
        var engineType = determineEngineType(_inputStreamSource);
        if (engineType != "dota")
        {
            throw new NecronomiconException($"Unable to parse engine type: {engineType}");
        }

        foreach (var frameSkeleton in _frameSkeletons)
        {
            if (frameSkeleton.FrameCommand == EDemoCommands.DemSendTables)
            {
                CDemoSendTables? sendTables = frameSkeleton.GetAsProtobuf<CDemoSendTables>(frameSkeleton.FrameCommand);
                if (sendTables != null)
                {
                    foreach (var handler in Callbacks.OnDemSendTables)
                    {
                        handler(sendTables);
                    }
                }
            }
            if (frameSkeleton.FrameCommand == EDemoCommands.DemClassInfo)
            {
                CDemoClassInfo? classInfo = frameSkeleton.GetAsProtobuf<CDemoClassInfo>(frameSkeleton.FrameCommand);
                if (classInfo != null)
                {
                    foreach (var handler in Callbacks.OnDemClassInfo)
                    {
                        handler(classInfo);
                    }
                }
            }
            if (frameSkeleton.FrameCommand == EDemoCommands.DemPacket || frameSkeleton.FrameCommand == EDemoCommands.DemSignonPacket)
            {
                CDemoPacket? packet = frameSkeleton.GetAsProtobuf<CDemoPacket>(frameSkeleton.FrameCommand);

                if (packet != null)
                {
                    foreach (var handler in Callbacks.OnDemPacket)
                    {
                        handler(packet);
                    }
                }
            }
            if (frameSkeleton.FrameCommand == EDemoCommands.DemFullPacket)
            {
                CDemoFullPacket? fullPacket = frameSkeleton.GetAsProtobuf<CDemoFullPacket>(frameSkeleton.FrameCommand);

                if (fullPacket != null)
                {
                    foreach (var handler in Callbacks.OnDemPacket)
                    {
                        handler(fullPacket.Packet);
                    }
                }
            }
            // Debug.WriteLine($"Tick {frameSkeleton.FrameTick}");
            // Debug.WriteLine($"Command {frameSkeleton.FrameCommand}");
        }
    }

    public void UpdateInstanceBaseline()
    {
        // We can't update the instancebaseline until we have class info.
        if (ClassInfos.Count == 0)
        {
            return;
        }

        if (!StringTables.NameIndex.ContainsKey("instancebaseline"))
        {
            // Skipping updateInstanceBaseline, no instancebaseline string table
            return;
        }

        var baselineStringTableIndex = StringTables.NameIndex["instancebaseline"];
        var baselineStringTable = StringTables.Tables[baselineStringTableIndex];

        foreach (var baselineItem in baselineStringTable.Items.Values)
        {
            Debug.WriteLine($"Baseline item: {baselineItem.Key}");
            if (baselineItem.Key != string.Empty)
            {
                var classId = Atoi32(baselineItem.Key);
                ClassBaselines[classId] = baselineItem.Value;
            }
        }
    }

    private int Atoi32(string s)
    {
        try
        {
            if (int.TryParse(s, out int value))
            {
                return value;
            }
            else
            {
                throw new NecronomiconException($"Unable to parse string {s} to int");
            }
            // return Convert.ToInt32(s, s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 16 :
            //                           s.StartsWith("0") && s.Length > 1 ? 8 : 10);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"Unable to parse '{s}' as Int32.", ex);
        }
    }

    private string determineEngineType(InputStreamSource source)
    {
        var buf = source.ReadEngineHeader();
        string engineMagicHeader = System.Text.Encoding.UTF8.GetString(buf);
        if (!EngineMagicHeaderExtensions.TryParseStringValue(engineMagicHeader, out var magicHeaderEnum))
        {
            throw new InvalidDataException("Invalid Header");
        }
        DemFile demFile = new DemFile(source);
        _frameSkeletons = demFile.ParseFilePackets();
        CDemoFileHeader cDemoFileHeader = demFile.GetFileHeader();

        string gameId = string.Empty;
        if (cDemoFileHeader.HasGame)
        {
            gameId = cDemoFileHeader.Game;
        }
        else if (cDemoFileHeader.HasGameDirectory)
        {
            var matchPattern = Regex.Match(cDemoFileHeader.GameDirectory, @".*[/\\](\w+)$");
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
                return "dota";
            case "citadel":
                throw new NotImplementedException();
            default:
                throw new NecronomiconException($"Unexpected new game type: {gameId}");
        }
    }


}

public delegate Task OnCDemoPacket(CDemoPacket packet);
public delegate Task OnCDemoClassInfo(CDemoClassInfo classInfo);
public delegate Task OnCDemoSendTables(CDemoSendTables sendTable);
public delegate Task OnSvcPacketEntities(CSVCMsg_PacketEntities packetEntities);
public delegate Task OnSvcCreateStringTable(CSVCMsg_CreateStringTable createStringTable);
public delegate Task OnSvcUpdateStringTable(CSVCMsg_UpdateStringTable updateStringTable);

public class NecronomiconCallbacks
{
    public List<OnCDemoPacket> OnDemPacket { get; } = new();
    public List<OnCDemoClassInfo> OnDemClassInfo { get; } = new();
    public List<OnCDemoSendTables> OnDemSendTables { get; } = new();
    public List<OnSvcPacketEntities> OnSvcPacketEntities { get; } = new();
    public List<OnSvcCreateStringTable> OnSvcCreateStringTable { get; } = new();
    public List<OnSvcUpdateStringTable> OnSvcUpdateStringTable { get; } = new();
}
