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

public class FieldPathOp
{
    public string Name { get; set; } = string.Empty;
    public int Weight { get; set; }

    // Delegate to match signature: void(reader, fieldPath)
    public Action<BitReaderWrapper, FieldPath> Fn { get; set; }

    public FieldPathOp(string name, int weight, Action<BitReaderWrapper, FieldPath> function)
    {
        Name = name;
        Weight = weight;
        Fn = function;
    }
}

public static class FieldPathOps
{
    public static readonly FieldPathOp[] Table = {
        new("PlusOne", 36271, (r, fp) => fp.Path[fp.Last] += 1),
        new("PlusTwo", 10334, (r, fp) => fp.Path[fp.Last] += 2),
        new("PlusThree", 1375, (r, fp) => fp.Path[fp.Last] += 3),
        new("PlusFour", 646, (r, fp) => fp.Path[fp.Last] += 4),
        new("PlusN", 4128, (r, fp) => fp.Path[fp.Last] += r.ReadUBitVarFieldPath() + 5),
        new("PushOneLeftDeltaZeroRightZero", 35, (r, fp) => {
            fp.Last++;
            fp.Path[fp.Last] = 0;
        }),
        new("PushOneLeftDeltaZeroRightNonZero", 3, (r, fp) => {
            fp.Last++;
            fp.Path[fp.Last] = r.ReadUBitVarFieldPath();
        }),
        new("PushOneLeftDeltaOneRightZero", 521, (r, fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] = 0;
        }),
        new("PushOneLeftDeltaOneRightNonZero", 2942, (r, fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] = r.ReadUBitVarFieldPath();
        }),
        new("PushOneLeftDeltaNRightZero", 560, (r, fp) => {
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] = 0;
        }),
        new("PushOneLeftDeltaNRightNonZero", 471, (r, fp) => {
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath() + 2;
            fp.Last++;
            fp.Path[fp.Last] = r.ReadUBitVarFieldPath() + 1;
        }),
        new("PushOneLeftDeltaNRightNonZeroPack6Bits", 10530, (r, fp) => {
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(3) + 2;
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(3) + 1;
        }),
        new("PushOneLeftDeltaNRightNonZeroPack8Bits", 251, (r, fp) => {
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(4) + 2;
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(4) + 1;
        }),
        new("PushTwoLeftDeltaZero", 0, (r, fp) => {
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushTwoPack5LeftDeltaZero", 0, (r, fp) => {
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushThreeLeftDeltaZero", 0, (r, fp) => {
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushThreePack5LeftDeltaZero", 0, (r, fp) => {
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] = (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushTwoLeftDeltaOne", 0, (r, fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushTwoPack5LeftDeltaOne", 0, (r, fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushThreeLeftDeltaOne", 0, (r, fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushThreePack5LeftDeltaOne", 0, (r, fp) => {
            fp.Path[fp.Last] += 1;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushTwoLeftDeltaN", 0, (r, fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushTwoPack5LeftDeltaN", 0, (r, fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushThreeLeftDeltaN", 0, (r, fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            fp.Last++;
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
        }),
        new("PushThreePack5LeftDeltaN", 0, (r, fp) => {
            fp.Path[fp.Last] += (int)r.ReadUBitVar() + 2;
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
            fp.Last++;
            fp.Path[fp.Last] += (int)r.Reader.ReadUInt32LSB(5);
        }),
        new("PushN", 0, (r, fp) => {
            var n = r.ReadUBitVar();
            fp.Path[fp.Last] += (int)r.ReadUBitVar();
            for (int i = 0; i < n; i++) {
                fp.Last++;
                fp.Path[fp.Last] += r.ReadUBitVarFieldPath();
            }
        }),
        new("PushNAndNonTopological", 310, (r, fp) => {
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += r.ReadVarInt32() + 1;
                }
            }
            var count = r.ReadUBitVar();
            for (int i = 0; i < count; i++) {
                fp.Last++;
                fp.Path[fp.Last] = r.ReadUBitVarFieldPath();
            }
        }),
        new("PopOnePlusOne", 2, (r, fp) => {
            fp.Pop(1);
            fp.Path[fp.Last] += 1;
        }),
        new("PopOnePlusN", 0, (r, fp) => {
            fp.Pop(1);
            fp.Path[fp.Last] += r.ReadUBitVarFieldPath() + 1;
        }),
        new("PopAllButOnePlusOne", 1837, (r, fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += 1;
        }),
        new("PopAllButOnePlusN", 149, (r, fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += r.ReadUBitVarFieldPath() + 1;
        }),
        new("PopAllButOnePlusNPack3Bits", 300, (r, fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += (int)r.Reader.ReadUInt32LSB(3) + 1;
        }),
        new("PopAllButOnePlusNPack6Bits", 634, (r, fp) => {
            fp.Pop(fp.Last);
            fp.Path[0] += (int)r.Reader.ReadUInt32LSB(6) + 1;
        }),
        new("PopNPlusOne", 0, (r, fp) => {
            fp.Pop(r.ReadUBitVarFieldPath());
            fp.Path[fp.Last] += 1;
        }),
        new("PopNPlusN", 0, (r, fp) => {
            fp.Pop(r.ReadUBitVarFieldPath());
            fp.Path[fp.Last] += r.ReadVarInt32();
        }),
        new("PopNAndNonTopographical", 1, (r, fp) => {
            fp.Pop(r.ReadUBitVarFieldPath());
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += r.ReadVarInt32();
                }
            }
        }),
        new("NonTopoComplex", 76, (r, fp) => {
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += r.ReadVarInt32();
                }
            }
        }),
        new("NonTopoPenultimatePlusOne", 271, (r, fp) => {
            fp.Path[fp.Last - 1] += 1;
        }),
        new("NonTopoComplexPack4Bits", 99, (r, fp) => {
            for (int i = 0; i <= fp.Last; i++) {
                if (r.Reader.ReadBitLSB()) {
                    fp.Path[i] += (int)r.Reader.ReadUInt32LSB(4) - 7;
                }
            }
        }),
        new("FieldPathEncodeFinish", 25474, (r, fp) => {
            fp.Done = true;
        }),
    };
}

public static class FieldPathDecoder
{
    private static readonly HuffmanNode HuffTree = NewHuffmanTree();
    public static List<FieldPath> ReadFieldPaths(BitReaderWrapper r)
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
                FieldPathOps.Table[next.Symbol].Fn(r, fp);
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

    private static HuffmanNode NewHuffmanTree()
    {
        var freqs = FieldPathOps.Table.Select(op => op.Weight).ToArray();
        return HuffmanBuilder.Build(freqs);
    }
}

public class FieldReader
{
    private readonly BitReaderWrapper _reader;
    private readonly Serializer _serializer;
    private readonly FieldState _state;

    public FieldReader(BitReaderWrapper reader, Serializer serializer, FieldState state)
    {
        _reader = reader;
        _serializer = serializer;
        _state = state;
    }

    public void ReadFields()
    {
        List<FieldPath> fieldPaths = FieldPathDecoder.ReadFieldPaths(_reader);

        foreach (FieldPath fieldPath in fieldPaths)
        {
            FieldDecoder decoder = _serializer.GetDecoderForFieldPath(fieldPath, 0);
            var value = decoder.Invoke(_reader);
            _state.Set(fieldPath, value);

            fieldPath.Release();
        }
    }
}