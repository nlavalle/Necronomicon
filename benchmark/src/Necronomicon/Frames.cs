using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using necronomicon;
using necronomicon.model;
using necronomicon.model.dem;
using necronomicon.model.engine;
using Steam.Protos.Dota2;

namespace Benchmarks.Necronomicon;

[MemoryDiagnoser]
public class Frames
{
    private static readonly string FileName = GetPathName();

    public static string GetPathName()
    {
        var cwd = Environment.CurrentDirectory;
        var root = Path.GetPathRoot(cwd) ?? throw new Exception();
        var relative = Path.GetRelativePath(root, cwd);

        var paths = relative.Split(Path.DirectorySeparatorChar);
        var index = Array.LastIndexOf(paths, "benchmark");
        return Path.Combine(root, Path.Combine(paths[..(index + 1)]), "test_replay.dem");
    }

    [Benchmark]
    public void FrameOnly()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        FrameOnlyParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void FrameDataOnly()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        FrameDataOnlyParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void FrameDataOnlyDelStored()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        FrameDataOnlyDelStoredParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void FrameDataProtobufs()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        FrameDataProtobufsParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void FrameDataFromCallbacks()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        FrameDataFromCallbacksParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void NecroForReference()
    {
        necronomicon.Necronomicon parser = new necronomicon.Necronomicon(FileName);
        DemPackets packets = new DemPackets(parser);
        DemSendTables sendTables = new DemSendTables(parser);
        DemClassInfo classInfo = new DemClassInfo(parser);
        SvcPacketEntities packetEntities = new SvcPacketEntities(parser);
        SvcStringTable stringTable = new SvcStringTable(parser);
        parser.Parse();
    }

    public class FrameOnlyParser
    {
        public static async Task RunAsync(Stream stream)
        {
            var fella = new FrameOnlyParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            file.ParseAsSource2Replay(NoopFrameCheck);
        }

        private void NoopFrameCheck(Source2Replay.Frame frame)
        {
        }
    }

    public class FrameDataOnlyParser
    {
        public static async Task RunAsync(Stream stream)
        {
            var fella = new FrameDataOnlyParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            file.ParseAsSource2Replay(FrameCheck);
        }

        private void FrameCheck(Source2Replay.Frame frame)
        {
            frame.RegisterCallbackForData(NoopFrameDataCheck);
        }

        private void NoopFrameDataCheck(Source2Replay.FrameData data)
        {
        }

    }

    public class FrameDataOnlyDelStoredParser
    {
        private readonly Action<Source2Replay.FrameData> _delly;

        public FrameDataOnlyDelStoredParser()
        {
            _delly = NoopFrameDataCheck;
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new FrameDataOnlyDelStoredParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            file.ParseAsSource2Replay(FrameCheck);
        }

        private void FrameCheck(Source2Replay.Frame frame)
        {
            frame.RegisterCallbackForData(_delly);
        }

        private void NoopFrameDataCheck(Source2Replay.FrameData data)
        {
        }
    }

    public class FrameDataProtobufsParser
    {
        private readonly Action<Source2Replay.FrameData> _delly;

        public FrameDataProtobufsParser()
        {
            _delly = FrameDataCheck;
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new FrameDataProtobufsParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            file.ParseAsSource2Replay(FrameCheck);
        }

        private void FrameCheck(Source2Replay.Frame frame)
        {
            frame.RegisterCallbackForData(_delly);
        }

        private void FrameDataCheck(Source2Replay.FrameData data)
        {
            switch (data.FrameCommand)
            {
                case EDemoCommands.DemStop:
                    var stop = data.GetAsProtobuf<CDemoStop>();
                    break;
                case EDemoCommands.DemFileHeader:
                    var fh = data.GetAsProtobuf<CDemoFileHeader>();
                    break;
                case EDemoCommands.DemFileInfo:
                    var fi = data.GetAsProtobuf<CDemoFileInfo>();
                    break;
                case EDemoCommands.DemSyncTick:
                    var tick = data.GetAsProtobuf<CDemoSyncTick>();
                    break;
                case EDemoCommands.DemSendTables:
                    var st = data.GetAsProtobuf<CDemoSendTables>();
                    break;
                case EDemoCommands.DemClassInfo:
                    var ci = data.GetAsProtobuf<CDemoClassInfo>();
                    break;
                case EDemoCommands.DemStringTables:
                    var table = data.GetAsProtobuf<CDemoStringTables>();
                    break;
                case EDemoCommands.DemPacket:
                case EDemoCommands.DemSignonPacket:
                    var p = data.GetAsProtobuf<CDemoPacket>();
                    break;
                case EDemoCommands.DemConsoleCmd:
                    var cc = data.GetAsProtobuf<CDemoConsoleCmd>();
                    break;
                case EDemoCommands.DemCustomData:
                    var cd = data.GetAsProtobuf<CDemoCustomData>();
                    break;
                case EDemoCommands.DemCustomDataCallbacks:
                    var cdc = data.GetAsProtobuf<CDemoCustomDataCallbacks>();
                    break;
                case EDemoCommands.DemUserCmd:
                    var uc = data.GetAsProtobuf<CDemoUserCmd>();
                    break;
                case EDemoCommands.DemFullPacket:
                    var fp = data.GetAsProtobuf<CDemoFullPacket>();
                    break;
                case EDemoCommands.DemSaveGame:
                    var sg = data.GetAsProtobuf<CDemoSaveGame>();
                    break;
                case EDemoCommands.DemSpawnGroups:
                    var spawn = data.GetAsProtobuf<CDemoSpawnGroups>();
                    break;
                case EDemoCommands.DemAnimationData:
                    var ad = data.GetAsProtobuf<CDemoAnimationData>();
                    break;
                case EDemoCommands.DemAnimationHeader:
                    var ah = data.GetAsProtobuf<CDemoAnimationHeader>();
                    break;
                case EDemoCommands.DemRecovery:
                    var r = data.GetAsProtobuf<CDemoRecovery>();
                    break;
                default:
                    break;
            }
        }
    }

    public class FrameDataFromCallbacksParser
    {
        private Source2ReplayFrameCallbacks? _dellies;

        public FrameDataFromCallbacksParser()
        {
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new FrameDataFromCallbacksParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemStop(Source2Replay.FrameData data)
        {
            var stop = data.GetAsProtobuf<CDemoStop>();
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            callbacks[EDemoCommands.DemStop] = OnDemStop;
            callbacks[EDemoCommands.DemFileInfo] = OnDemFileInfo;
            callbacks[EDemoCommands.DemSyncTick] = OnDemSyncTick;
            callbacks[EDemoCommands.DemSendTables] = OnDemSendTables;
            callbacks[EDemoCommands.DemClassInfo] = OnDemClassInfo;
            callbacks[EDemoCommands.DemStringTables] = OnDemStringTables;
            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemConsoleCmd] = OnDemConsoleCmd;
            callbacks[EDemoCommands.DemCustomData] = OnDemCustomData;
            callbacks[EDemoCommands.DemCustomDataCallbacks] = OnDemCustomDataCallbacks;
            callbacks[EDemoCommands.DemUserCmd] = OnDemUserCmd;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
            callbacks[EDemoCommands.DemSaveGame] = OnDemSaveGame;
            callbacks[EDemoCommands.DemSpawnGroups] = OnDemSpawnGroups;
            callbacks[EDemoCommands.DemAnimationData] = OnDemAnimationData;
            callbacks[EDemoCommands.DemAnimationHeader] = OnDemAnimationHeader;
            callbacks[EDemoCommands.DemRecovery] = OnDemRecovery;
        }

        private void OnDemFileInfo(Source2Replay.FrameData data)
        {
            var fi = data.GetAsProtobuf<CDemoFileInfo>();
        }

        private void OnDemSyncTick(Source2Replay.FrameData data)
        {
            var tick = data.GetAsProtobuf<CDemoSyncTick>();
        }

        private void OnDemSendTables(Source2Replay.FrameData data)
        {
            var st = data.GetAsProtobuf<CDemoSendTables>();
        }

        private void OnDemClassInfo(Source2Replay.FrameData data)
        {
            var ci = data.GetAsProtobuf<CDemoClassInfo>();
        }

        private void OnDemStringTables(Source2Replay.FrameData data)
        {
            var table = data.GetAsProtobuf<CDemoStringTables>();
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
        }

        private void OnDemConsoleCmd(Source2Replay.FrameData data)
        {
            var cc = data.GetAsProtobuf<CDemoConsoleCmd>();
        }

        private void OnDemCustomData(Source2Replay.FrameData data)
        {
            var cd = data.GetAsProtobuf<CDemoCustomData>();
        }

        private void OnDemCustomDataCallbacks(Source2Replay.FrameData data)
        {
            var cdc = data.GetAsProtobuf<CDemoCustomDataCallbacks>();
        }

        private void OnDemUserCmd(Source2Replay.FrameData data)
        {
            var uc = data.GetAsProtobuf<CDemoUserCmd>();
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
        }

        private void OnDemSaveGame(Source2Replay.FrameData data)
        {
            var sg = data.GetAsProtobuf<CDemoSaveGame>();
        }

        private void OnDemSpawnGroups(Source2Replay.FrameData data)
        {
            var spawn = data.GetAsProtobuf<CDemoSpawnGroups>();
        }

        private void OnDemAnimationData(Source2Replay.FrameData data)
        {
            var ad = data.GetAsProtobuf<CDemoAnimationData>();
        }

        private void OnDemAnimationHeader(Source2Replay.FrameData data)
        {
            var ah = data.GetAsProtobuf<CDemoAnimationHeader>();
        }

        private void OnDemRecovery(Source2Replay.FrameData data)
        {
            var r = data.GetAsProtobuf<CDemoRecovery>();
        }


    }

}


