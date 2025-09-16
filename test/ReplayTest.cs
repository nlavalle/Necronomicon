using System.Diagnostics;
using necronomicon.Examples;

namespace necronomicon_test;

public class ReplayTest
{
    private Stopwatch _stopwatch = new Stopwatch();

    [Fact]
    public async Task TestTheReplayDawg()
    {
        _stopwatch.Start();
        await RawDogginIt.RunAsync(File.Open(@"test_replay.dem", FileMode.Open, FileAccess.Read));
        _stopwatch.Stop();
        Debug.WriteLine($"Total time to execute: {_stopwatch.Elapsed}");        
    }
}
