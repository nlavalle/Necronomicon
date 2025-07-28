namespace necronomicon.model;

public class HuffmanNode
{
    public int? Symbol;
    public HuffmanNode Left;
    public HuffmanNode Right;
    public int Weight;

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
                var node = new HuffmanNode { Symbol = i, Weight = frequencies[i] };
                pq.Enqueue(node, node.Weight);
            }
        }

        while (pq.Count > 1)
        {
            var left = pq.Dequeue();
            var right = pq.Dequeue();
            var parent = new HuffmanNode
            {
                Left = left,
                Right = right,
                Weight = left.Weight + right.Weight
            };
            pq.Enqueue(parent, parent.Weight);
        }

        return pq.Count > 0 ? pq.Dequeue() : null;
    }
}