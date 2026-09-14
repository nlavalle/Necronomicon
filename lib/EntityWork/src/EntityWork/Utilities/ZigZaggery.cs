namespace EntityWork.Utilities;

public static class ZigZaggery
{
    public static long UnZigZagValue(ulong value)
    {
        return (long)(value >> 1) ^ -((long)value & 1);
    }

    public static int UnZigZagValue(uint value)
    {
        return (int)(value >> 1) ^ -((int)value & 1);
    }

    public static void UnZigZagReference(ref long value)
    {
        value = (long)((ulong)value >> 1) ^ -(value & 1);
    }

    public static void UnZigZagReference(ref ulong value)
    {
        value = (value >> 1) ^ (ulong)-((long)value & 1);
    }

    public static void UnZigZagReference(ref int value)
    {
        value = (int)((uint)value >> 1) ^ -(value & 1);
    }

    public static void UnZigZagReference(ref uint value)
    {
        value = (value >> 1) ^ (uint)-((int)value & 1);
    }

    public static ulong ZigZagValue(long value)
    {
        return (ulong)(value >> 63) ^ ((ulong)value << 1);
    }

    public static uint ZigZagValue(int value)
    {
        return (uint)(value >> 31) ^ ((uint)value << 1);
    }

    public static void ZigZagReference(ref long value)
    {
        value = (value >> 63) ^ (value << 1);
    }

    public static void ZigZagReference(ref ulong value)
    {
        value = (ulong)((long)value >> 63) ^ (value << 1);
    }

    public static void ZigZagReference(ref int value)
    {
        value = (value >> 31) ^ (value << 1);
    }

    public static void ZigZagReference(ref uint value)
    {
        value = (uint)((int)value >> 31) ^ (value << 1);
    }

}
