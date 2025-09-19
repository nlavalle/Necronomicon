using BenchmarkDotNet.Attributes;
using necronomicon;

namespace Benchmarks.Necronomicon;

[MemoryDiagnoser]
public class Frames
{
    private static readonly string FileName = Path.Combine(Environment.CurrentDirectory, "test_replay.dem");

    static Frames()
    {
        var cwd = Environment.CurrentDirectory;
        var root = Path.GetPathRoot(cwd) ?? throw new Exception();
        var relative = Path.GetRelativePath(root, cwd);

        var paths = relative.Split(Path.DirectorySeparatorChar);
        var index = Array.LastIndexOf(paths, "benchmark");
        FileName = Path.Combine(root, Path.Combine(paths[0..(index + 1)]), "test_replay.dem");
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
}


