using System.Diagnostics;
using necronomicon.model.engine;
using necronomicon.processor;

namespace necronomicon.model;

public class FieldPath
{
    public int[] Path { get; set; } = Array.Empty<int>();
    public int Last { get; set; }
    public bool Done { get; set; }

    public void Pop(int n)
    {
        for (int i = 0; i < n; i++)
        {
            Path[Last] = 0;
            Last--;
        }
    }

    public FieldPath Copy()
    {
        var x = FieldPathPool.Get();
        Array.Copy(Path, x.Path, Path.Length);
        x.Last = Last;
        x.Done = Done;
        return x;
    }


    public override string ToString()
    {
        return string.Join("/", Path.Take(Last + 1));
    }

    public void Reset()
    {
        Array.Copy(FieldPathPool.ResetTemplate, Path, Path.Length);
        Last = 0;
        Done = false;
    }

    public void Release()
    {
        FieldPathPool.Put(this);
    }
}

public static class FieldPathPool
{
    private static readonly Stack<FieldPath> _pool = new();
    public static readonly int[] ResetTemplate = [-1, 0, 0, 0, 0, 0, 0];

    public static FieldPath Get()
    {
        if (_pool.Count > 0)
        {
            var fp = _pool.Pop();
            fp.Reset();
            return fp;
        }

        return new FieldPath { Path = [-1, 0, 0, 0, 0, 0, 0] };
    }

    public static void Put(FieldPath fp) => _pool.Push(fp);
}

public delegate void FieldPathOpDelegate(ref FastBitReader reader, FieldPath path);

public class FieldPathOp
{
    public string Name { get; set; } = string.Empty;
    public int Weight { get; set; }

    // Delegate to match signature: void(reader, fieldPath)
    public FieldPathOpDelegate Fn { get; set; }

    public FieldPathOp(string name, int weight, FieldPathOpDelegate function)
    {
        Name = name;
        Weight = weight;
        Fn = function;
    }
}

public static class FieldPathOps
{
    public static readonly (int Value, int Weight)[] OtherTable = [
        ((int)FieldPathOpTypes.PlusOne, 36271),
        ((int)FieldPathOpTypes.PlusTwo, 10334),
        ((int)FieldPathOpTypes.PlusThree, 1375),
        ((int)FieldPathOpTypes.PlusFour, 646),
        ((int)FieldPathOpTypes.PlusN, 4128),
        ((int)FieldPathOpTypes.PushOneLeftDeltaZeroRightZero, 35),
        ((int)FieldPathOpTypes.PushOneLeftDeltaZeroRightNonZero, 3),
        ((int)FieldPathOpTypes.PushOneLeftDeltaOneRightZero, 521),
        ((int)FieldPathOpTypes.PushOneLeftDeltaOneRightNonZero, 2942),
        ((int)FieldPathOpTypes.PushOneLeftDeltaNRightZero, 560),
        ((int)FieldPathOpTypes.PushOneLeftDeltaNRightNonZero, 471),
        ((int)FieldPathOpTypes.PushOneLeftDeltaNRightNonZeroPack6Bits, 10530),
        ((int)FieldPathOpTypes.PushOneLeftDeltaNRightNonZeroPack8Bits, 251),
        ((int)FieldPathOpTypes.PushTwoLeftDeltaZero, 0),
        ((int)FieldPathOpTypes.PushTwoPack5LeftDeltaZero, 0),
        ((int)FieldPathOpTypes.PushThreeLeftDeltaZero, 0),
        ((int)FieldPathOpTypes.PushThreePack5LeftDeltaZero, 0),
        ((int)FieldPathOpTypes.PushTwoLeftDeltaOne, 0),
        ((int)FieldPathOpTypes.PushTwoPack5LeftDeltaOne, 0),
        ((int)FieldPathOpTypes.PushThreeLeftDeltaOne, 0),
        ((int)FieldPathOpTypes.PushThreePack5LeftDeltaOne, 0),
        ((int)FieldPathOpTypes.PushTwoLeftDeltaN, 0),
        ((int)FieldPathOpTypes.PushTwoPack5LeftDeltaN, 0),
        ((int)FieldPathOpTypes.PushThreeLeftDeltaN, 0),
        ((int)FieldPathOpTypes.PushThreePack5LeftDeltaN, 0),
        ((int)FieldPathOpTypes.PushN, 0),
        ((int)FieldPathOpTypes.PushNAndNonTopological, 310),
        ((int)FieldPathOpTypes.PopOnePlusOne, 2),
        ((int)FieldPathOpTypes.PopOnePlusN, 0),
        ((int)FieldPathOpTypes.PopAllButOnePlusOne, 1837),
        ((int)FieldPathOpTypes.PopAllButOnePlusN, 149),
        ((int)FieldPathOpTypes.PopAllButOnePlusNPack3Bits, 300),
        ((int)FieldPathOpTypes.PopAllButOnePlusNPack6Bits, 634),
        ((int)FieldPathOpTypes.PopNPlusOne, 0),
        ((int)FieldPathOpTypes.PopNPlusN, 0),
        ((int)FieldPathOpTypes.PopNAndNonTopographical, 1),
        ((int)FieldPathOpTypes.NonTopoComplex, 76),
        ((int)FieldPathOpTypes.NonTopoPenultimatePlusOne, 271),
        ((int)FieldPathOpTypes.NonTopoComplexPack4Bits, 99),
        ((int)FieldPathOpTypes.FieldPathEncodeFinish, 25474),
    ];

    public static readonly FieldPathOp[] Table = {
        new("PlusOne", 36271, (ref FastBitReader r, FieldPath fp) => fp.Path[fp.Last] += 1),
        new("PlusTwo", 10334, (ref FastBitReader r, FieldPath fp) => fp.Path[fp.Last] += 2),
        new("PlusThree", 1375, (ref FastBitReader r, FieldPath fp) => fp.Path[fp.Last] += 3),
        new("PlusFour", 646, (ref FastBitReader r, FieldPath fp) => fp.Path[fp.Last] += 4),
        new("PlusN", 4128, (ref FastBitReader r, FieldPath fp) => fp.Path[fp.Last] += r.ReadUBitVarFieldPath() + 5),
        new("PushOneLeftDeltaZeroRightZero", 35, (ref FastBitReader r, FieldPath fp) => {
            fp.Last++;
            fp.Path[fp.Last] = 0;
        }),
        new("PushOneLeftDeltaZeroRightNonZero", 3, (ref FastBitReader r, FieldPath fp) => {
            fp.Last++;
            fp.Path[fp.Last] = r.ReadUBitVarFieldPath();
        }),
        new("PushOneLeftDeltaOneRightZero", 521, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] = 0;
        }),
        new("PushOneLeftDeltaOneRightNonZero", 2942, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] = r.ReadUBitVarFieldPath();
        }),
        new("PushOneLeftDeltaNRightZero", 560, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] = 0;
        }),
        new("PushOneLeftDeltaNRightNonZero", 471, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath() + 2;
            fp.Last++;
            fp.Path[fp.Last] = r.ReadUBitVarFieldPath() + 1;
        }),
        new("PushOneLeftDeltaNRightNonZeroPack6Bits", 10530, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(3) + 2;
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(3) + 1;
        }),
        new("PushOneLeftDeltaNRightNonZeroPack8Bits", 251, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(4) + 2;
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(4) + 1;
        }),
        new("PushTwoLeftDeltaZero", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushTwoPack5LeftDeltaZero", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushThreeLeftDeltaZero", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushThreePack5LeftDeltaZero", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushTwoLeftDeltaOne", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushTwoPack5LeftDeltaOne", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushThreeLeftDeltaOne", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushThreePack5LeftDeltaOne", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushTwoLeftDeltaN", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushTwoPack5LeftDeltaN", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushThreeLeftDeltaN", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushThreePack5LeftDeltaN", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushN", 0, (ref FastBitReader r, FieldPath fp) => {
            var n = r.ReadUBitVar();
            fp.Path[fp.Last] += (int)r.ReadUBitVar();
            for (int i = 0; i < n; i++) {
                fp.Last++;
                fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            }
        }),
        new("PushNAndNonTopological", 310, (ref FastBitReader r, FieldPath fp) => {
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += r.ReadZigZagVarInt32() + 1;
                }
            }
            var count = r.ReadUBitVar();
            for (int i = 0; i < count; i++) {
                fp.Last++;
                fp.Path[fp.Last] = r.ReadUBitVarFieldPath();
            }
        }),
        new("PopOnePlusOne", 2, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(1);
            fp.Path[fp.Last] += 1;
        }),
        new("PopOnePlusN", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(1);
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath() + 1;
        }),
        new("PopAllButOnePlusOne", 1837, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += 1;
        }),
        new("PopAllButOnePlusN", 149, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += r.ReadUBitVarFieldPath() + 1;
        }),
        new("PopAllButOnePlusNPack3Bits", 300, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += (int)r.Reader.ReadUInt32LSB(3) + 1;
        }),
        new("PopAllButOnePlusNPack6Bits", 634, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += (int)r.Reader.ReadUInt32LSB(6) + 1;
        }),
        new("PopNPlusOne", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(r.ReadUBitVarFieldPath());
            fp.Path[fp.Last] += 1;
        }),
        new("PopNPlusN", 0, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(r.ReadUBitVarFieldPath());
            fp.Path[fp.Last] += r.ReadZigZagVarInt32();
        }),
        new("PopNAndNonTopographical", 1, (ref FastBitReader r, FieldPath fp) => {
            fp.Pop(r.ReadUBitVarFieldPath());
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += r.ReadZigZagVarInt32();
                }
            }
        }),
        new("NonTopoComplex", 76, (ref FastBitReader r, FieldPath fp) => {
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += r.ReadZigZagVarInt32();
                }
            }
        }),
        new("NonTopoPenultimatePlusOne", 271, (ref FastBitReader r, FieldPath fp) => {
            fp.Path[fp.Last - 1] += 1;
        }),
        new("NonTopoComplexPack4Bits", 99, (ref FastBitReader r, FieldPath fp) => {
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += (int)r.Reader.ReadUInt32LSB(4) - 7;
                }
            }
        }),
        new("FieldPathEncodeFinish", 25474, (ref FastBitReader r, FieldPath fp) => {
            fp.Done = true;
        }),
    };
}

public static class FieldPathDecoder
{
    private static readonly HuffmanNode HuffTree = NewHuffmanTree();
    private static readonly HuffmanTable HuffTable = HuffmanTable.Create(FieldPathOps.OtherTable);

    public static List<FieldPath> ReadFieldPaths(ref FastBitReader r)
    {
        var fp = FieldPathPool.Get();
        var node = HuffTree;
        var paths = new List<FieldPath>();

        while (!fp.Done)
        {
            HuffmanNode? next = r.Reader.ReadBitLSB() ? node.Right : node.Left;
            if (next!.IsLeaf)
            {
                node = HuffTree;
                FieldPathOps.Table[next.Symbol].Fn(ref r, fp);
                if (!fp.Done)
                    paths.Add(fp.Copy());
            }
            else
            {
                node = next;
            }
        }

        fp.Release();
        return paths;
    }

    internal static void ReadFieldPaths2(ref FastBitReader r, scoped Span<int> stack)
    {
        var fp = new FieldPathList(stack);
        fp.Init();

        var table = HuffTable;
        var node = table.Root;

        while (true)
        {
            node = r.Reader.ReadBitLSB() ? table.GetRight(node) : table.GetLeft(node);
            if (!node.IsLeaf)
                continue;

            switch ((FieldPathOpTypes)node.Value)
            {
                case FieldPathOpTypes.PlusOne:
                    fp[0] += 1;
                    break;
                case FieldPathOpTypes.PlusTwo:
                    fp[0] += 2;
                    break;
                case FieldPathOpTypes.PlusThree:
                    fp[0] += 3;
                    break;
                case FieldPathOpTypes.PlusFour:
                    fp[0] += 4;
                    break;
                case FieldPathOpTypes.PlusN:
                    fp[0] += r.ReadUBitVarFieldPath() + 5;
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaZeroRightZero:
                    fp.Push(0);
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaZeroRightNonZero:
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaOneRightZero:
                    fp[0] += 1;
                    fp.Push();
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaOneRightNonZero:
                    fp[0] += 1;
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaNRightZero:
                    fp[0] += r.ReadUBitVarFieldPath();
                    fp.Push();
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaNRightNonZero:
                    fp[0] += r.ReadUBitVarFieldPath() + 2;
                    fp.Push(r.ReadUBitVarFieldPath() + 1);
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaNRightNonZeroPack6Bits:
                    fp[0] += (int)r.Reader.ReadUInt32LSB(3) + 2;
                    fp.Push((int)r.Reader.ReadUInt32LSB(3) + 1);
                    break;
                case FieldPathOpTypes.PushOneLeftDeltaNRightNonZeroPack8Bits:
                    fp[0] += (int)r.Reader.ReadUInt32LSB(4) + 2;
                    fp.Push((int)r.Reader.ReadUInt32LSB(4) + 1);
                    break;
                case FieldPathOpTypes.PushTwoLeftDeltaZero:
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushTwoPack5LeftDeltaZero:
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    break;
                case FieldPathOpTypes.PushThreeLeftDeltaZero:
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushThreePack5LeftDeltaZero:
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    break;
                case FieldPathOpTypes.PushTwoLeftDeltaOne:
                    fp[0] += 1;
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushTwoPack5LeftDeltaOne:
                    fp[0] += 1;
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    break;
                case FieldPathOpTypes.PushThreeLeftDeltaOne:
                    fp[0] += 1;
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushThreePack5LeftDeltaOne:
                    fp[0] += 1;
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    break;
                case FieldPathOpTypes.PushTwoLeftDeltaN:
                    fp[0] += (int)r.ReadUBitVar() + 2;
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushTwoPack5LeftDeltaN:
                    fp[0] += (int)r.ReadUBitVar() + 2;
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    break;
                case FieldPathOpTypes.PushThreeLeftDeltaN:
                    fp[0] += (int)r.ReadUBitVar() + 2;
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    fp.Push(r.ReadUBitVarFieldPath());
                    break;
                case FieldPathOpTypes.PushThreePack5LeftDeltaN:
                    fp[0] += (int)r.ReadUBitVar() + 2;
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    fp.Push((int)r.Reader.ReadUInt32LSB(5));
                    break;
                case FieldPathOpTypes.PushN:
                    var n = r.ReadUBitVar();
                    fp[0] += (int)r.ReadUBitVar();
                    for (int i = 0; i < n; i++)
                    {
                        fp.Push(r.ReadUBitVarFieldPath());
                    }
                    break;
                case FieldPathOpTypes.PushNAndNonTopological:
                    foreach (ref var item in fp.CurrentStack)
                    {
                        if (r.Reader.ReadBitLSB())
                        {
                            item += r.ReadZigZagVarInt32() + 1;
                        }
                    }
                    var count = r.ReadUBitVar();
                    for (int i = 0; i < count; i++)
                    {
                        fp.Push(r.ReadUBitVarFieldPath());
                    }
                    break;
                case FieldPathOpTypes.PopOnePlusOne:
                    fp.Shrink(1);
                    fp[0] += 1;
                    break;
                case FieldPathOpTypes.PopOnePlusN:
                    fp.Shrink(1);
                    fp[0] += r.ReadUBitVarFieldPath() + 1;
                    break;
                case FieldPathOpTypes.PopAllButOnePlusOne:
                    fp.SizeTo(1);
                    fp[0] += 1;
                    break;
                case FieldPathOpTypes.PopAllButOnePlusN:
                    fp.SizeTo(1);
                    fp[0] += r.ReadUBitVarFieldPath() + 1;
                    break;
                case FieldPathOpTypes.PopAllButOnePlusNPack3Bits:
                    fp.SizeTo(1);
                    fp[0] += (int)r.Reader.ReadUInt32LSB(3) + 1;
                    break;
                case FieldPathOpTypes.PopAllButOnePlusNPack6Bits:
                    fp.SizeTo(1);
                    fp[0] += (int)r.Reader.ReadUInt32LSB(6) + 1;
                    break;
                case FieldPathOpTypes.PopNPlusOne:
                    fp.Shrink(r.ReadUBitVarFieldPath());
                    fp[0] += 1;
                    break;
                case FieldPathOpTypes.PopNPlusN:
                    fp.Shrink(r.ReadUBitVarFieldPath());
                    fp[0] += r.ReadZigZagVarInt32();
                    break;
                case FieldPathOpTypes.PopNAndNonTopographical:
                    fp.Shrink(r.ReadUBitVarFieldPath());
                    foreach (ref var item in fp.CurrentStack)
                    {
                        if (r.Reader.ReadBitLSB())
                        {
                            item += r.ReadZigZagVarInt32();
                        }
                    }
                    break;
                case FieldPathOpTypes.NonTopoComplex:
                    foreach (ref var item in fp.CurrentStack)
                    {
                        if (r.Reader.ReadBitLSB())
                        {
                            item += r.ReadZigZagVarInt32();
                        }
                    }
                    break;
                case FieldPathOpTypes.NonTopoPenultimatePlusOne:
                    fp[1] += 1;
                    break;
                case FieldPathOpTypes.NonTopoComplexPack4Bits:
                    foreach (ref var item in fp.CurrentStack)
                    {
                        if (r.Reader.ReadBitLSB())
                        {
                            item += (int)r.Reader.ReadUInt32LSB(4) - 7;
                        }
                    }
                    break;
                case FieldPathOpTypes.FieldPathEncodeFinish:
                    fp.Done();
                    return;
                default:
                    throw new UnreachableException();
            }

            fp.Copy();
            node = table.Root;
        }
    }

    private static HuffmanNode NewHuffmanTree()
    {
        var freqs = FieldPathOps.Table.Select(op => op.Weight).ToArray();
        return HuffmanBuilder.Build(freqs);
    }
}

public readonly ref struct FieldReader
{
    // TODO This is temporary, move to instanced allocation
    private static readonly int[] _pathStack = new int[32768];
    
    private readonly Serializer _serializer;
    private readonly FieldState _state;

    public FieldReader(Serializer serializer, FieldState state)
    {
        _serializer = serializer;
        _state = state;
    }

    public void ReadFields(ref FastBitReader reader)
    {
        List<FieldPath> fieldPaths = FieldPathDecoder.ReadFieldPaths(ref reader);

        foreach (FieldPath fieldPath in fieldPaths)
        {
            FieldDecoder decoder = _serializer.GetDecoderForFieldPath(fieldPath, 0);
            var value = decoder.Invoke(ref reader);
            _state.Set(fieldPath, value);

            fieldPath.Release();
        }
    }

    public void ReadFields2(ref FastBitReader reader)
    {
        FieldPathDecoder.ReadFieldPaths2(ref reader, _pathStack);
        var fieldPaths = new FieldPathList(_pathStack);

        foreach (var fieldPath in fieldPaths)
        {
            FieldDecoder decoder = _serializer.GetDecoderForFieldPath2(fieldPath, 0);
            var value = decoder.Invoke(ref reader);
            _state.Set2(fieldPath, value);
        }
    }
}
