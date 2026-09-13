using BitWork;

namespace Tests.BitWork;

public class ReadOnlyAlignedBitSpanTests
{
    // Edge cases
    private static readonly byte[] OneZero = [0];
    private static readonly byte[] OneOne = [1];
    private static readonly byte[] EightZeroes = [0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] NineZeroes = [0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Aych = [104, 0];
    private static readonly byte[] AychOffsetOne = [104 << 1, 0, 0];
    private static readonly byte[] HelloWorld = [104, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 0];
    private static readonly byte[] HelloWorldOffsetOne = [104 << 1, 101 << 1, 108 << 1, 108 << 1, 111 << 1, 32 << 1, 119 << 1, 111 << 1, 114 << 1, 108 << 1, 100 << 1, 0, 0];

    [Fact]
    public void Create1BytesEmpty()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(Span<byte>.Empty);

        Assert.Equal(0, bitSpan.Length);
    }

    [Fact]
    public void Create1BytesShort()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneOne);

        Assert.Equal(8, bitSpan.Length);
    }

    [Fact]
    public void Create1BytesMid()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorld);

        Assert.Equal(96, bitSpan.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(64)]
    [InlineData(96)]
    public void Create2Bytes(long length)
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorld, length);

        Assert.Equal(length, bitSpan.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(97)]
    public void Create2BytesThrows(long length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorld, length);
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 8)]
    [InlineData(0, 64)]
    [InlineData(0, 96)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 8)]
    [InlineData(1, 64)]
    [InlineData(1, 95)]
    [InlineData(8, 88)]
    [InlineData(64, 32)]
    [InlineData(96, 0)]
    public void Create3Bytes(long start, long length)
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorld, start, length);

        Assert.Equal(length, bitSpan.Length);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(-1, 96)]
    [InlineData(-1, 97)]
    [InlineData(0, -1)]
    [InlineData(0, 97)]
    [InlineData(1, -1)]
    [InlineData(1, 96)]
    [InlineData(96, -1)]
    [InlineData(96, 1)]
    [InlineData(97, -1)]
    [InlineData(97, 0)]
    [InlineData(97, 96)]
    public void Create3BytesThrows(long start, long length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorld, start, length);
        });
    }

    [Fact]
    public void Create1LongsEmpty()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(Span<ulong>.Empty);

        Assert.Equal(0, bitSpan.Length);
    }

    [Fact]
    public void Create1LongsShort()
    {
        Span<ulong> span = [0];
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(span);

        Assert.Equal(64, bitSpan.Length);
    }

    [Fact]
    public void Create1LongsMid()
    {
        Span<ulong> span = [0, 0];
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(span);

        Assert.Equal(128, bitSpan.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(64)]
    [InlineData(128)]
    public void Create2Longs(long length)
    {
        Span<ulong> span = [0, 0];
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(span, length);

        Assert.Equal(length, bitSpan.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(129)]
    public void Create2LongsThrows(long length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<ulong> span = [0, 0];
            scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(span, length);
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 8)]
    [InlineData(0, 64)]
    [InlineData(0, 128)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 8)]
    [InlineData(1, 64)]
    [InlineData(1, 127)]
    [InlineData(8, 120)]
    [InlineData(64, 64)]
    [InlineData(128, 0)]
    public void Create3Longs(long start, long length)
    {
        Span<ulong> span = [0, 0];
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(span, start, length);

        Assert.Equal(length, bitSpan.Length);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(-1, 128)]
    [InlineData(-1, 129)]
    [InlineData(0, -1)]
    [InlineData(0, 129)]
    [InlineData(1, -1)]
    [InlineData(1, 128)]
    [InlineData(128, -1)]
    [InlineData(128, 1)]
    [InlineData(129, -1)]
    [InlineData(129, 0)]
    [InlineData(129, 128)]
    public void Create3LongsThrows(long start, long length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<ulong> span = [0, 0];
            scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(span, start, length);
        });
    }

    [Fact]
    public void CountUntilZeroOneZero()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneZero);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
    }

    [Fact]
    public void CopyUntilZeroOneZero()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneZero);
        Span<byte> byteSpan = [0, 0];
        scoped ReadOnlyLsBitWindow window = default;

        Assert.True(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 0, byteSpan, out var copied));
        Assert.Equal(0, copied);

        Assert.True(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 0, byteSpan, out copied));
        Assert.Equal(0, copied);

        window = default;

        Assert.True(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 0, [], out copied));
        Assert.Equal(0, copied);
    }

    [Fact]
    public void CountUntilZeroOneOne()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneOne);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
    }

    [Fact]
    public void CountUntilZeroEightZeroes()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(EightZeroes);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
    }

    [Fact]
    public void CountUntilZeroNineZeroes()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(NineZeroes);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
    }

    [Fact]
    public void CountUntilZeroAych()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(Aych);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(1, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 8));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 9));
    }

    [Fact]
    public void CountUntilZeroAychOffsetOne()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(AychOffsetOne);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(1, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(1, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 8));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 9));
    }

    [Fact]
    public void CountUntilZeroAychHelloWorld()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorld);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(11, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
        Assert.Equal(10, bitSpan.GetByteCountUntilZeroLSB(in window, 8));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 9));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 88));
        Assert.Equal(-1, bitSpan.GetByteCountUntilZeroLSB(in window, 89));
    }

    [Fact]
    public void CountUntilZeroAychHelloWorldOffsetOne()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(HelloWorldOffsetOne);
        scoped ReadOnlyLsBitWindow window = default;

        Assert.Equal(11, bitSpan.GetByteCountUntilZeroLSB(in window, 0));
        Assert.Equal(11, bitSpan.GetByteCountUntilZeroLSB(in window, 1));
        Assert.Equal(10, bitSpan.GetByteCountUntilZeroLSB(in window, 8));
        Assert.Equal(10, bitSpan.GetByteCountUntilZeroLSB(in window, 9));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 88));
        Assert.Equal(0, bitSpan.GetByteCountUntilZeroLSB(in window, 89));
    }

    [Fact]
    public void CopyUntilZeroNoSrcNoDst()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneZero);
        Span<byte> byteSpan = [];
        scoped ReadOnlyLsBitWindow window = default;

        Assert.False(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 1, byteSpan, out var copied));
        Assert.Equal(0, copied);
    }

    [Fact]
    public void CopyUntilZeroNoSrc()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneZero);
        Span<byte> byteSpan = [0, 0];
        scoped ReadOnlyLsBitWindow window = default;

        Assert.False(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 1, byteSpan, out var copied));
        Assert.Equal(0, copied);
    }

    [Fact]
    public void CopyUntilZeroNoDstTrue()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneZero);
        Span<byte> byteSpan = [];
        scoped ReadOnlyLsBitWindow window = default;

        Assert.True(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 0, byteSpan, out var copied));
        Assert.Equal(0, copied);
    }

    [Fact]
    public void CopyUntilZeroNoDstFalse()
    {
        scoped var bitSpan = ReadOnlyAlignedBitSpan.Create(OneOne);
        Span<byte> byteSpan = [];
        scoped ReadOnlyLsBitWindow window = default;

        Assert.False(bitSpan.TryCopyBytesUntilZeroLSB(ref window, 0, byteSpan, out var copied));
        Assert.Equal(0, copied);
    }

}
