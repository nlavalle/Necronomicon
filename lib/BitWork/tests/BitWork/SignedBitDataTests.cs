using BitWork;

namespace Tests.BitWork;

public class SignedBitDataTests
{
    [Theory]
    [InlineData(0L, 0, 0UL)]
    [InlineData(0L, 1, 0UL)]
    [InlineData(0L, 63, 0UL)]
    [InlineData(0L, 64, 0UL)]
    [InlineData(-1L, 1, 1UL)]
    [InlineData(-1L, 63, ~0UL >> 1)]
    [InlineData(-1L, 64, ~0UL)]
    [InlineData(short.MaxValue, 16, (ulong)short.MaxValue)]
    [InlineData(short.MaxValue, 63, (ulong)short.MaxValue)]
    [InlineData(short.MaxValue, 64, (ulong)short.MaxValue)]
    [InlineData(short.MinValue, 16, unchecked((ulong)(ushort)short.MinValue))]
    [InlineData(short.MinValue, 63, unchecked((ulong)(long)short.MinValue) ^ 1UL << 63)]
    [InlineData(short.MinValue, 64, unchecked((ulong)(long)short.MinValue))]
    public void AsUnsignedLsBitDataTruncated(long value, int length, ulong expected)
    {
        var data = new SignedBitData(value, length);

        var result = data.AsUnsignedLsBitData(true);

        Assert.Equal(length, result.Length);
        Assert.Equal(expected, result.Data);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 65)]
    [InlineData(1, 0)]
    [InlineData(1, 65)]
    [InlineData(64, 63)]
    [InlineData(64, 65)]
    public void AsUnsignedLsBitDataThrows(int dataLength, int unsignedLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var data = new SignedBitData(0L, dataLength);
            var udata = data.AsUnsignedLsBitData(unsignedLength);
        });
    }
}
