using necronomicon;

namespace necronomicon_test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.Equal(1, 1);
    }

    [Fact]
    public void TestParsing()
    {
        string path = Path.GetFullPath(@"test_replay.dem");
        Necronomicon necronomicon = new Necronomicon(path);
        necronomicon.infoForFile();
        // DemParser parser = new DemParser();
        // parser.parseFile(path);
    }
}
