using System.Runtime.InteropServices;
using System.Text;
using BitsKit.IO;

namespace necronomicon.processor;

public class BitReaderWrapper
{
    public BitReader Reader;

    public BitReaderWrapper(BitReader reader)
    {
        Reader = reader;
    }

    public BitReaderWrapper(byte[] buffer)
    {
        Reader = new BitReader(buffer);
    }

    public uint ReadUBitVarFP()
    {
        if (Reader.ReadBitLSB())
        {
            return Reader.ReadUInt32LSB(2);
        }
        if (Reader.ReadBitLSB())
        {
            return Reader.ReadUInt32LSB(4);
        }
        if (Reader.ReadBitLSB())
        {
            return Reader.ReadUInt32LSB(10);
        }
        if (Reader.ReadBitLSB())
        {
            return Reader.ReadUInt32LSB(17);
        }
        return Reader.ReadUInt32LSB(31);
    }

    public int ReadUBitVarFieldPath()
    {
        return (int)ReadUBitVarFP();
    }

    public uint ReadUBitVar()
    {
        uint ret = Reader.ReadUInt32LSB(6);

        switch (ret & 0x30) // mask to check bits 4 and 5 (0x30 = 0011 0000)
        {
            case 0x10: // 16 decimal
                ret = (ret & 0x0F) | (Reader.ReadUInt32LSB(4) << 4);
                break;
            case 0x20: // 32 decimal
                ret = (ret & 0x0F) | (Reader.ReadUInt32LSB(8) << 4);
                break;
            case 0x30: // 48 decimal
                ret = (ret & 0x0F) | (Reader.ReadUInt32LSB(28) << 4);
                break;
        }

        return ret;
    }

    public uint ReadVarUInt32()
    {
        var s = 0;
        uint v = 0;
        while (true)
        {
            byte b = Reader.ReadUInt8LSB(8);
            v |= (uint)(b & 0x7FL) << s;
            s += 7;
            if ((b & 0x80L) == 0L || s == 35)
            {
                return v;
            }
        }
    }

    public ulong ReadVarUInt64()
    {
        ulong result = 0;
        long working;
        int shift;

        shift = 0;

        while (true)
        {
            if (shift > 63)
                throw new InvalidDataException();

            working = Reader.ReadUInt8LSB(8);

            unchecked
            {
                result |= (ulong)((working & 0x7F) << shift);
            }

            if ((working & 0x80) != 0x80)
                break;

            shift += 7;
        }

        return result;
    }

    public int ReadVarInt32()
    {
        uint ux = ReadVarUInt32();
        int x = (int)(ux >> 1);
        // return (int)((x >>> 1) ^ -(x ^ 1L));
        if ((ux & 1) != 0)
        {
            x = ~x;
        }
        return x;
    }

    public uint ReadEmbeddedInt()
    {
        // This is a unique header from Valve, the first two bits indicate how many bits to take after
        int[] UBV_COUNT = { 0, 4, 8, 28 };
        var v = Reader.ReadUInt32LSB(6);
        var a = v >> 4;
        if (a == 0)
        {
            return v;
        }
        else
        {
            return (v & 15) | (Reader.ReadUInt32LSB(UBV_COUNT[a]) << 4);
        }
    }

    public void ReadToSpanBuffer(Span<byte> target)
    {
        int totalBits = target.Length * 8;
        int destIndex = 0;

        Span<ulong> destination = MemoryMarshal.Cast<byte, ulong>(target);

        while (totalBits >= 64)
        {
            destination[destIndex++] = Reader.ReadUInt64LSB(64);
            totalBits -= 64;
        }

        destIndex *= sizeof(ulong);

        while (totalBits > 0)
        {
            // Store last tail as ulong
            target[destIndex++] = Reader.ReadUInt8LSB(8);
            totalBits -= 8;
        }

        return;
    }

    public float ReadCoord()
    {
        var Value = 0.0F;

        uint IntVal = (uint)(Reader.ReadBitLSB() ? 1 : 0);
        uint FractVal = (uint)(Reader.ReadBitLSB() ? 1 : 0);
        if (IntVal != 0 || FractVal != 0)
        {
            bool SignBit = Reader.ReadBitLSB();
            if (IntVal != 0)
            {
                IntVal = Reader.ReadUInt32LSB(14) + 1;
            }

            if (FractVal != 0)
            {
                FractVal = Reader.ReadUInt32LSB(5);
            }

            Value = (float)(IntVal + FractVal * (1.0 / (1 << 5)));

            if (SignBit)
            {
                Value = -Value;
            }
        }

        return Value;
    }

    public float ReadAngle(int n)
    {
        uint value = Reader.ReadUInt32LSB(n);
        return value * 360f / (1 << n);
    }

    public float ReadNormal()
    {
        var isNeg = Reader.ReadBitLSB();
        var len = Reader.ReadUInt32LSB(11);
        var ret = len * 1.0 / ((1 << 11) - 1.0);

        if (isNeg)
        {
            return (float)-ret;
        }
        else
        {
            return (float)ret;
        }
    }

    public float[] Read3BitNormal()
    {
        var ret = new float[] { 0.0F, 0.0F, 0.0F };

        var hasX = Reader.ReadBitLSB();
        var hasY = Reader.ReadBitLSB();

        if (hasX)
        {
            ret[0] = ReadNormal();
        }

        if (hasY)
        {
            ret[1] = ReadNormal();
        }

        var negZ = Reader.ReadBitLSB();
        var prodSum = ret[0] * ret[0] + ret[1] * ret[1];

        if (prodSum < 1.0)
        {
            ret[2] = (float)Math.Sqrt(1.0 - prodSum);
        }
        else
        {
            ret[2] = 0.0F;
        }

        if (negZ)
        {
            ret[2] = -ret[2];
        }

        return ret;
    }

    public string ReadString()
    {
        var bytes = new List<byte>();
        while (true)
        {
            byte b = Reader.ReadUInt8LSB(8);
            if (b == 0)
                break;
            bytes.Add(b);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public string ReadStringN(int bytes)
    {
        var o = 0;
        byte[] stringBytes = new byte[bytes];
        while (o < bytes)
        {
            stringBytes[o++] = Reader.ReadUInt8LSB(8);
        }

        return Encoding.UTF8.GetString(stringBytes);
    }

    public void ReadBitsAsBytes(byte[] dest, int n)
    {
        var o = 0;
        while (n >= 8)
        {
            dest[o++] = Reader.ReadUInt8LSB(8);
            n -= 8;
        }

        if (n > 0)
        {
            dest[o] = Reader.ReadUInt8LSB(n);
        }
    }
}