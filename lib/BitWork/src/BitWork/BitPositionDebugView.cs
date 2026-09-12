using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitWork;

[DebuggerDisplay("{Length} b: {Desc}", Name = "@{Offset}")]
public sealed class BitPositionDebugView
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly InterpretationType _type;

    public string Binary
    {
        get
        {
            switch (_type)
            {
                case InterpretationType.Byte:
                    string bytes = Value.Length switch
                    {
                        > 8 => BitConverter.ToString(Value, 0, 8) + " ...",
                        _ => BitConverter.ToString(Value)
                    };

                    return bytes.ToLower().Replace('-', ' ');
                default:
                    var bits = MemoryMarshal.AsRef<ulong>(Value).ToString("B64");

                    var len = 64 - Length;
                    return len switch
                    {
                        < 0 => bits + "...",
                        _ => bits.Substring((int)len)
                    };
            }
        }
    }

    public string Desc { get; }
    public byte[] Value { get; }
    public long Offset { get; }
    public long Length { get; }

    public List<string> Notes { get; } = new();

    public BitPositionDebugView(InterpretationType type, string desc, byte[] data, long offset, long length)
    {
        _type = type;
        Desc = desc;
        Value = data;
        Offset = offset;
        Length = length;
    }

    public enum InterpretationType
    {
        LsBit,
        Byte,
    }
}
