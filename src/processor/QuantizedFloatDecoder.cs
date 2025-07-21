namespace necronomicon.processor;

public class QuantizedFloatDecoder
{
    private const uint QFF_ROUNDDOWN = 1 << 0;
    private const uint QFF_ROUNDUP = 1 << 1;
    private const uint QFF_ENCODE_ZERO = 1 << 2;
    private const uint QFF_ENCODE_INTEGERS = 1 << 3;

    public float Low;
    public float High;
    public float HighLowMul;
    public float DecMul;
    public float Offset;
    public uint Bitcount;
    public uint Flags;
    public bool NoScale;

    public void ValidateFlags()
    {
        if (Flags == 0) return;

        if ((Low == 0.0f && (Flags & QFF_ROUNDDOWN) != 0) || (High == 0.0f && (Flags & QFF_ROUNDUP) != 0))
            Flags &= ~QFF_ENCODE_ZERO;

        if (Low == 0.0f && (Flags & QFF_ENCODE_ZERO) != 0)
        {
            Flags |= QFF_ROUNDDOWN;
            Flags &= ~QFF_ENCODE_ZERO;
        }

        if (High == 0.0f && (Flags & QFF_ENCODE_ZERO) != 0)
        {
            Flags |= QFF_ROUNDUP;
            Flags &= ~QFF_ENCODE_ZERO;
        }

        if (Low > 0.0f || High < 0.0f)
            Flags &= ~QFF_ENCODE_ZERO;

        if ((Flags & QFF_ENCODE_INTEGERS) != 0)
            Flags &= ~(QFF_ROUNDUP | QFF_ROUNDDOWN | QFF_ENCODE_ZERO);

        if ((Flags & (QFF_ROUNDDOWN | QFF_ROUNDUP)) == (QFF_ROUNDDOWN | QFF_ROUNDUP))
            throw new InvalidOperationException("Roundup / Rounddown are mutually exclusive");
    }

    public void AssignMultipliers(uint steps)
    {
        float range = High - Low;
        uint high = Bitcount == 32 ? 0xFFFFFFFE : (1u << (int)Bitcount) - 1;

        float highMul = Math.Abs(range) <= 0.0f
            ? high
            : high / range;

        if (highMul * range > high || (float)(highMul * range) > high)
        {
            foreach (float mult in new float[] { 0.9999f, 0.99f, 0.9f, 0.8f, 0.7f })
            {
                highMul = (high / range) * mult;
                if (highMul * range <= high) break;
            }
        }

        HighLowMul = highMul;
        DecMul = 1.0f / (steps - 1);

        if (HighLowMul == 0.0f)
            throw new InvalidOperationException("Error computing high / low multiplier");
    }

    public float Quantize(float val)
    {
        if (val < Low)
        {
            if ((Flags & QFF_ROUNDUP) == 0)
                throw new InvalidOperationException("Out of range low quantization");
            return Low;
        }
        else if (val > High)
        {
            if ((Flags & QFF_ROUNDDOWN) == 0)
                throw new InvalidOperationException("Out of range high quantization");
            return High;
        }

        uint i = (uint)((val - Low) * HighLowMul);
        return Low + (High - Low) * (i * DecMul);
    }

    public float Decode(BitReaderWrapper r)
    {
        if ((Flags & QFF_ROUNDDOWN) != 0 && r.Reader.ReadBitLSB())
            return Low;

        if ((Flags & QFF_ROUNDUP) != 0 && r.Reader.ReadBitLSB())
            return High;

        if ((Flags & QFF_ENCODE_ZERO) != 0 && r.Reader.ReadBitLSB())
            return 0.0f;

        return Low + (High - Low) * r.Reader.ReadUInt32LSB((int)Bitcount) * DecMul;
    }

    public static QuantizedFloatDecoder New(int? bitCount, int? flags, float? lowValue, float? highValue)
    {
        var qfd = new QuantizedFloatDecoder();

        if (bitCount == null || bitCount == 0 || bitCount >= 32)
        {
            qfd.NoScale = true;
            qfd.Bitcount = 32;
            return qfd;
        }

        qfd.Bitcount = (uint)bitCount.Value;
        qfd.NoScale = false;
        qfd.Offset = 0.0f;

        qfd.Low = lowValue ?? 0.0f;
        qfd.High = highValue ?? 1.0f;
        qfd.Flags = (uint)(flags ?? 0);

        qfd.ValidateFlags();

        uint steps = 1u << (int)qfd.Bitcount;
        float range;

        if ((qfd.Flags & QFF_ROUNDDOWN) != 0)
        {
            range = qfd.High - qfd.Low;
            qfd.Offset = range / steps;
            qfd.High -= qfd.Offset;
        }
        else if ((qfd.Flags & QFF_ROUNDUP) != 0)
        {
            range = qfd.High - qfd.Low;
            qfd.Offset = range / steps;
            qfd.Low += qfd.Offset;
        }

        if ((qfd.Flags & QFF_ENCODE_INTEGERS) != 0)
        {
            float delta = qfd.High - qfd.Low;
            if (delta < 1) delta = 1;

            double deltaLog2 = Math.Ceiling(Math.Log(delta, 2));
            uint range2 = (uint)(1 << (int)deltaLog2);

            uint bc = qfd.Bitcount;
            while ((1u << (int)bc) <= range2)
                bc++;

            if (bc > qfd.Bitcount)
            {
                qfd.Bitcount = bc;
                steps = 1u << (int)qfd.Bitcount;
            }

            qfd.Offset = (float)range2 / steps;
            qfd.High = qfd.Low + range2 - qfd.Offset;
        }

        qfd.AssignMultipliers(steps);

        if ((qfd.Flags & QFF_ROUNDDOWN) != 0 && qfd.Quantize(qfd.Low) == qfd.Low)
            qfd.Flags &= ~QFF_ROUNDDOWN;

        if ((qfd.Flags & QFF_ROUNDUP) != 0 && qfd.Quantize(qfd.High) == qfd.High)
            qfd.Flags &= ~QFF_ROUNDUP;

        if ((qfd.Flags & QFF_ENCODE_ZERO) != 0 && qfd.Quantize(0.0f) == 0.0f)
            qfd.Flags &= ~QFF_ENCODE_ZERO;

        return qfd;
    }

}