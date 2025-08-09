using System.Diagnostics;
using System.Text;
using necronomicon.model;

namespace necronomicon_test;

public class FieldOpsTest
{
    [Fact]
    public async Task TestHuffmanSerlization()
    {
        var freqs = FieldPathOps.Table.Select(op => op.Weight).ToArray();
        var huffy = HuffmanBuilder.Build(freqs);
        var huffyString = DumpHuffmanTree(huffy);
        Debug.WriteLine(huffyString);

        string path = Path.GetFullPath(@"huffmanTreeNecronomicon.txt");
        var testString = File.ReadAllText(path);
        Debug.Assert(huffyString == testString);

        await Task.CompletedTask;
    }

    public static string DumpHuffmanTree(HuffmanNode node)
    {
        var sb = new StringBuilder();
        Serialize(node, "", sb);
        return sb.ToString();
    }

    private static void Serialize(HuffmanNode node, string path, StringBuilder sb)
    {
        if (node == null)
        {
            sb.AppendLine($"{path}:null");
            return;
        }

        if (node.IsLeaf)
        {
            sb.AppendLine($"{path}:{node.Symbol}");
        }
        else
        {
            sb.AppendLine($"{path}:*"); // * denotes internal node
            Serialize(node.Left!, path + "0", sb);
            Serialize(node.Right!, path + "1", sb);
        }
    }
}
