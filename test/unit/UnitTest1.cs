using System.Diagnostics;
using necronomicon;
using necronomicon.model;
using necronomicon.model.dem;
using necronomicon.model.engine;
using Steam.Protos.Dota2;

namespace necronomicon_test;

public class UnitTest1
{
    [Fact]
    public async Task TestCallback()
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();

        string path = Path.GetFullPath(@"test_replay.dem");
        Necronomicon parser = new Necronomicon(path);
        DemPackets packets = new DemPackets(parser);
        DemSendTables sendTables = new DemSendTables(parser);
        DemClassInfo classInfo = new DemClassInfo(parser);
        SvcPacketEntities packetEntities = new SvcPacketEntities(parser);
        SvcStringTable stringTable = new SvcStringTable(parser);
        parser.Parse();

        stopwatch.Stop();

        var commandCount = packets._embeddedMessages
            .SelectMany(em => em.Commands)
            .GroupBy(em => em)
            .OrderBy(grp => grp.Key)
            .Select(grp => (Value: grp.Key, Count: grp.Count()))
            .ToList();
        Debug.WriteLine($"Total time to execute: {stopwatch.Elapsed}");
        Debug.WriteLine(packets._embeddedMessages.Count);

        await Task.CompletedTask;
    }
}
