namespace necronomicon.model;

public class HuffmanNode
{
    public int? Symbol;
    public HuffmanNode Left;
    public HuffmanNode Right;

    public bool IsLeaf => Symbol.HasValue;
}

public static class HuffmanBuilder
{
    public static HuffmanNode Build(int[] frequencies)
    {
        var pq = new PriorityQueue<HuffmanNode, int>();

        for (int i = 0; i < frequencies.Length; i++)
        {
            if (frequencies[i] > 0)
            {
                var node = new HuffmanNode { Symbol = i };
                pq.Enqueue(node, frequencies[i]);
            }
        }

        while (pq.Count > 1)
        {
            var left = pq.Dequeue();
            var right = pq.Dequeue();
            var parent = new HuffmanNode
            {
                Left = left,
                Right = right
            };
            pq.Enqueue(parent, GetWeight(left) + GetWeight(right));
        }

        return pq.Count > 0 ? pq.Dequeue() : null;
    }

    private static int GetWeight(HuffmanNode node)
    {
        if (node.IsLeaf) return 0; // or store weights explicitly if needed
        return GetWeight(node.Left) + GetWeight(node.Right);
    }
}