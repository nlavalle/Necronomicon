using System.Diagnostics;

namespace necronomicon.model;

public readonly struct HuffmanTable
{
    private readonly Node[] _table;

    public HuffmanTableNode Root => default;

    private HuffmanTable(Node[] table)
    {
        _table = table;
    }

    public HuffmanTableNode GetRight(HuffmanTableNode node)
    {
        return new(_table[node.Index].Right);
    }

    public HuffmanTableNode GetLeft(HuffmanTableNode node)
    {
        return new(_table[node.Index].Left);
    }

    private sealed class HuffmanPriorityComparer
        : IComparer<HuffmanPriority>
    {
        public int Compare(HuffmanPriority x, HuffmanPriority y)
        {
            int result = x.Weight - y.Weight;
            if (result == 0)
                result = y.Order - x.Order;

            return result;
        }
    }

    public static HuffmanTable Create((int Value, int Weight)[] values)
    {
        var length = values.Length;
        if (length < 2)
            throw new ArgumentOutOfRangeException(nameof(values));
            
        var queue = new PriorityQueue<int, HuffmanPriority>(length, new HuffmanPriorityComparer());


        int index;
        for (index = 0; index < values.Length; index++)
        {
            ref var value = ref values[index];
            queue.Enqueue(~value.Value, new HuffmanPriority(index, Math.Max(value.Weight, 1)));
        }

        Debug.Assert(index == values.Length);

        var result = new Node[--index];
        length += index;

        while (true)
        {
            if (!queue.TryDequeue(out var left, out var leftPri) || !queue.TryDequeue(out var right, out var rightPri))
                throw new UnreachableException();

            result[--index] = new Node(left, right);

            if (index == 0)
                break;

            var weight = leftPri.Weight + rightPri.Weight;
            queue.Enqueue(index, new HuffmanPriority(length - index, weight));

            Debug.Assert(queue.Count >= 2);
        }

        Debug.Assert(queue.Count == 0);

        return new HuffmanTable(result);
    }
    
    private readonly record struct Node(int Left, int Right);
    private readonly record struct HuffmanPriority(int Order, int Weight);
}
