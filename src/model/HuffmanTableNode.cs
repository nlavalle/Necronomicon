namespace necronomicon.model;

public readonly struct HuffmanTableNode
{
    internal readonly int Index { get; }

    public bool IsLeaf => Index < 0;

    public int Value => ~Index;

    internal HuffmanTableNode(int index)
    {
        Index = index;
    }

    public HuffmanTableNode GetLeft(HuffmanTable table)
    {
        return table.GetLeft(this);
    }

    public HuffmanTableNode GetRight(HuffmanTable table)
    {
        return table.GetRight(this);
    }
}
