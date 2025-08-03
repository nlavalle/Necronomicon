namespace necronomicon.model;

public class HuffmanNode
{
    public int Symbol;
    public int Weight;
    public HuffmanNode? Left;
    public HuffmanNode? Right;
    public bool IsLeaf => Left == null && Right == null;
}

public class HuffmanNodeComparer : IComparer<HuffmanNode>
{
    public int Compare(HuffmanNode? a, HuffmanNode? b)
    {
        if (a == null || b == null) throw new ArgumentNullException();
        
        int weightCompare = a.Weight.CompareTo(b.Weight);
        if (weightCompare != 0) return weightCompare;

        return b.Symbol.CompareTo(a.Symbol);
    }
}

public static class HuffmanBuilder
{
    public static HuffmanNode Build(int[] frequencies)
    {
        var comparer = new HuffmanNodeComparer();
        var pq = new SortedSet<HuffmanNode>(comparer);

        for (int i = 0; i < frequencies.Length; i++)
        {
            int weight = frequencies[i] == 0 ? 1 : frequencies[i];
            pq.Add(new HuffmanNode { Symbol = i, Weight = weight });
        }

        int nextSymbol = frequencies.Length;

        while (pq.Count > 1)
        {
            var left = pq.Min!;
            pq.Remove(left);
            var right = pq.Min!;
            pq.Remove(right);

            var parent = new HuffmanNode
            {
                Symbol = nextSymbol++,
                Left = left,
                Right = right,
                Weight = left.Weight + right.Weight
            };
            pq.Add(parent);
        }

        if (pq.Count <= 0)
            throw new NecronomiconException("Huffman table ended up without final node");

        return pq.Min!;
    }
}