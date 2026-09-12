using BitWork;

namespace Tests.BitWork;

public class UnsignedLsBitDataTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(~0UL)]
    [InlineData(unchecked((ulong)long.MaxValue))]
    [InlineData(unchecked((ulong)long.MinValue))]
    public void Create1(ulong value)
    {
        var data = UnsignedLsBitData.Create(value);

        Assert.Equal(64, data.Length);
        Assert.Equal(value, data.Data);
    }

    [Theory]
    [InlineData(0UL, 64, 0UL)]
    [InlineData(0UL, 63, 0UL)]
    [InlineData(0UL, 1, 0UL)]
    [InlineData(1UL, 64, 1UL)]
    [InlineData(1UL, 63, 1UL)]
    [InlineData(1UL, 1, 1UL)]
    [InlineData(1UL, 0, 0UL)]
    [InlineData(~0UL, 64, ~0UL)]
    [InlineData(~0UL, 63, ~0UL >> 1)]
    [InlineData(~0UL, 1, 1UL)]
    [InlineData(~0UL, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 64, unchecked((ulong)long.MaxValue))]
    [InlineData(unchecked((ulong)long.MaxValue), 63, unchecked((ulong)long.MaxValue))]
    [InlineData(unchecked((ulong)long.MaxValue), 1, 1UL)]
    [InlineData(unchecked((ulong)long.MinValue), 64, unchecked((ulong)long.MinValue))]
    [InlineData(unchecked((ulong)long.MinValue), 63, 0L)]
    [InlineData(unchecked((ulong)long.MinValue), 1, 0L)]
    public void Create2(ulong value, int length, ulong expected)
    {
        var data = UnsignedLsBitData.Create(value, length);

        Assert.Equal(length, data.Length);
        Assert.Equal(expected, data.Data);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65)]
    public void Create2Throws(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnsignedLsBitData.Create(0UL, length));
    }

    [Theory]
    [InlineData(0, 0, 0UL)]
    [InlineData(0, 1, 0UL)]
    [InlineData(0, 15, 0UL)]
    [InlineData(0, 16, 0UL)]
    [InlineData(1, 0, 1UL)]
    [InlineData(1, 1, 0UL)]
    [InlineData(1, 15, 0UL)]
    [InlineData(1, 16, 0UL)]
    [InlineData(ushort.MaxValue, 0, ushort.MaxValue)]
    [InlineData(ushort.MaxValue, 1, ushort.MaxValue >> 1)]
    [InlineData(ushort.MaxValue, 15, 1UL)]
    [InlineData(ushort.MaxValue, 16, 0UL)]
    [InlineData(unchecked((ushort)short.MaxValue), 0, unchecked((ushort)short.MaxValue))]
    [InlineData(unchecked((ushort)short.MaxValue), 1, unchecked((ushort)short.MaxValue) >> 1)]
    [InlineData(unchecked((ushort)short.MaxValue), 15, 0U)]
    [InlineData(unchecked((ushort)short.MaxValue), 16, 0U)]
    [InlineData(unchecked((ushort)short.MinValue), 0, unchecked((ushort)short.MinValue))]
    [InlineData(unchecked((ushort)short.MinValue), 1, unchecked((ushort)short.MinValue) >> 1)]
    [InlineData(unchecked((ushort)short.MinValue), 15, 1U)]
    [InlineData(unchecked((ushort)short.MinValue), 16, 0U)]
    public void Slice1Partial(ushort value, int bitCount, ulong expected)
    {
        var data = UnsignedLsBitData.Create(value, 16);

        data = data.Slice(bitCount);

        Assert.Equal(16 - bitCount, data.Length);
        Assert.Equal(expected, data.Data);
    }

    [Theory]
    [InlineData(0, 0, 0L)]
    [InlineData(0, 1, 0L)]
    [InlineData(0, 15, 0L)]
    [InlineData(0, 16, 0L)]
    [InlineData(1, 0, 1L)]
    [InlineData(1, 1, 0L)]
    [InlineData(1, 15, 0L)]
    [InlineData(1, 16, 0L)]
    [InlineData(ushort.MaxValue, 0, ushort.MaxValue)]
    [InlineData(ushort.MaxValue, 1, ushort.MaxValue >> 1)]
    [InlineData(ushort.MaxValue, 15, 1U)]
    [InlineData(ushort.MaxValue, 16, 0U)]
    [InlineData(unchecked((ushort)short.MaxValue), 0, unchecked((ushort)short.MaxValue))]
    [InlineData(unchecked((ushort)short.MaxValue), 1, unchecked((ushort)short.MaxValue) >> 1)]
    [InlineData(unchecked((ushort)short.MaxValue), 15, 0U)]
    [InlineData(unchecked((ushort)short.MaxValue), 16, 0U)]
    [InlineData(unchecked((ushort)short.MinValue), 0, unchecked((ushort)short.MinValue))]
    [InlineData(unchecked((ushort)short.MinValue), 1, unchecked((ushort)short.MinValue) >> 1)]
    [InlineData(unchecked((ushort)short.MinValue), 15, 1U)]
    [InlineData(unchecked((ushort)short.MinValue), 16, 0U)]
    public void Slice2Partial(ushort value, int bitCount, ulong expected)
    {
        var data = UnsignedLsBitData.Create(value, 16);

        data = data.Slice(bitCount, 16 - bitCount);

        Assert.Equal(16 - bitCount, data.Length);
        Assert.Equal(expected, data.Data);
    }

    [Theory]
    [InlineData(0UL, 0, 0UL)]
    [InlineData(0UL, 1, 0UL)]
    [InlineData(0UL, 63, 0UL)]
    [InlineData(0UL, 64, 0UL)]
    [InlineData(1UL, 0, 1UL)]
    [InlineData(1UL, 1, 0UL)]
    [InlineData(1UL, 63, 0UL)]
    [InlineData(1UL, 64, 0UL)]
    [InlineData(~0UL, 0, ~0UL)]
    [InlineData(~0UL, 1, ~0UL >> 1)]
    [InlineData(~0UL, 63, 1UL)]
    [InlineData(~0UL, 64, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 0, unchecked((ulong)long.MaxValue))]
    [InlineData(unchecked((ulong)long.MaxValue), 1, unchecked((ulong)long.MaxValue) >> 1)]
    [InlineData(unchecked((ulong)long.MaxValue), 63, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 64, 0UL)]
    [InlineData(unchecked((ulong)long.MinValue), 0, unchecked((ulong)long.MinValue))]
    [InlineData(unchecked((ulong)long.MinValue), 1, unchecked((ulong)long.MinValue) >> 1)]
    [InlineData(unchecked((ulong)long.MinValue), 63, 1UL)]
    [InlineData(unchecked((ulong)long.MinValue), 64, 0UL)]
    public void Slice1Full(ulong value, int bitCount, ulong expected)
    {
        var data = UnsignedLsBitData.Create(value);

        data = data.Slice(bitCount);

        Assert.Equal(64 - bitCount, data.Length);
        Assert.Equal(expected, data.Data);
    }

    [Theory]
    [InlineData(0L, 0, 0, 0L)]
    [InlineData(0L, 0, 64, 0L)]
    [InlineData(0L, 1, 0, 0L)]
    [InlineData(0L, 1, 63, 0L)]
    [InlineData(0L, 63, 0, 0L)]
    [InlineData(0L, 63, 1, 0L)]
    [InlineData(0L, 64, 0, 0L)]
    [InlineData(1L, 0, 0, 0L)]
    [InlineData(1L, 0, 64, 1L)]
    [InlineData(1L, 1, 0, 0L)]
    [InlineData(1L, 1, 63, 0L)]
    [InlineData(1L, 63, 0, 0L)]
    [InlineData(1L, 63, 1, 0L)]
    [InlineData(1L, 64, 0, 0L)]
    [InlineData(~0UL, 0, 0, 0UL)]
    [InlineData(~0UL, 0, 64, ~0UL)]
    [InlineData(~0UL, 1, 0, 0UL)]
    [InlineData(~0UL, 1, 63, ~0UL >> 1)]
    [InlineData(~0UL, 63, 0, 0UL)]
    [InlineData(~0UL, 63, 1, 1UL)]
    [InlineData(~0UL, 64, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 0, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 0, 64, unchecked((ulong)long.MaxValue))]
    [InlineData(unchecked((ulong)long.MaxValue), 1, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 1, 63, unchecked((ulong)long.MaxValue) >> 1)]
    [InlineData(unchecked((ulong)long.MaxValue), 63, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 63, 1, 0UL)]
    [InlineData(unchecked((ulong)long.MaxValue), 64, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MinValue), 0, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MinValue), 0, 64, unchecked((ulong)long.MinValue))]
    [InlineData(unchecked((ulong)long.MinValue), 1, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MinValue), 1, 63, unchecked((ulong)long.MinValue) >> 1)]
    [InlineData(unchecked((ulong)long.MinValue), 63, 0, 0UL)]
    [InlineData(unchecked((ulong)long.MinValue), 63, 1, 1UL)]
    [InlineData(unchecked((ulong)long.MinValue), 64, 0, 0UL)]
    public void Slice2Full(ulong value, int bitOffset, int bitCount, ulong expected)
    {
        var data = UnsignedLsBitData.Create(value);

        data = data.Slice(bitOffset, bitCount);

        Assert.Equal(bitCount, data.Length);
        Assert.Equal(expected, data.Data);
    }

    [Theory]
    [InlineData(0L, 0UL)]
    [InlineData(1L, 0UL)]
    [InlineData(~0UL, 0UL)]
    public void Slice1FullTwice(ulong value, ulong expected)
    {
        var data = UnsignedLsBitData.Create(value);

        data = data.Slice(32);
        data = data.Slice(32);

        Assert.Equal(0, data.Length);
        Assert.Equal(expected, data.Data);
    }

    [Theory]
    [InlineData(64, -1)]
    [InlineData(64, 65)]
    [InlineData(63, 64)]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    public void Slice1Throws(int length, int start)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var data = UnsignedLsBitData.Create(0UL, length);
            data = data.Slice(start);
        });
    }

    [Theory]
    [InlineData(64, -1, -1)]
    [InlineData(64, -1, 0)]
    [InlineData(64, -1, 1)]
    [InlineData(64, -1, 63)]
    [InlineData(64, -1, 64)]
    [InlineData(64, -1, 65)]
    [InlineData(64, 0, -1)]
    [InlineData(64, 0, 65)]
    [InlineData(64, 1, 64)]
    [InlineData(64, 63, 2)]
    [InlineData(64, 64, 1)]
    [InlineData(64, 65, 0)]
    [InlineData(63, 0, -1)]
    [InlineData(63, 0, 64)]
    [InlineData(63, 1, 63)]
    [InlineData(63, 62, 2)]
    [InlineData(63, 63, 1)]
    [InlineData(63, 64, 0)]
    [InlineData(1, 0, -1)]
    [InlineData(1, 0, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(1, 2, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(0, 1, 0)]
    public void Slice2Throws(int length, int start, int slice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var data = UnsignedLsBitData.Create(0UL, length);
            data = data.Slice(start, slice);
        });
    }

    [Theory]
    [InlineData(64, -1)]
    [InlineData(64, 65)]
    [InlineData(63, 64)]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    public void TruncateThrows(int length, int truncate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var data = UnsignedLsBitData.Create(0UL, length);
            data = data.Truncate(truncate);
        });
    }


}
