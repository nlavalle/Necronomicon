using necronomicon.processor;

namespace necronomicon_test;

public class BitReadTests
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

    public static IEnumerable<object[]> AlignedValids()
    {
        var seconds = new object[] {
            Array.Empty<byte>(),
            new byte[1] {0},
            new byte[7] {0,0,0,0,0,0,0},
            new byte[8] {0,0,0,0,0,0,0,0},
            new byte[9] {0,0,0,0,0,0,0,0,0},
            new byte[15] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            new byte[16] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            new byte[17] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
        };

        foreach (var second in seconds)
        {
            yield return new object[] { OneZero, second, true, 0 };
        }

        foreach (var second in seconds)
        {
            yield return new object[] { EightZeroes, second, true, 0 };
        }

        foreach (var second in seconds)
        {
            yield return new object[] { NineZeroes, second, true, 0 };
        }

        yield return new object[] { OneOne, seconds[0], false, 0 };
        foreach (var second in seconds.Skip(1))
        {
            yield return new object[] { OneOne, second, false, 1 };
        }

        yield return new object[] { Aych, seconds[0], false, 0 };
        foreach (var second in seconds.Skip(1))
        {
            yield return new object[] { Aych, second, true, 1 };
        }

        yield return new object[] { HelloWorld, seconds[0], false, 0 };
        yield return new object[] { HelloWorld, seconds[1], false, 1 };
        yield return new object[] { HelloWorld, seconds[2], false, 7 };
        yield return new object[] { HelloWorld, seconds[3], false, 8 };
        yield return new object[] { HelloWorld, seconds[4], false, 9 };
        foreach (var second in seconds.Skip(5))
        {
            yield return new object[] { HelloWorld, second, true, 11 };
        }
    }

    public static IEnumerable<object[]> OffsetValids()
    {
        var seconds = new object[] {
            Array.Empty<byte>(),
            new byte[1] {0},
            new byte[7] {0,0,0,0,0,0,0},
            new byte[8] {0,0,0,0,0,0,0,0},
            new byte[9] {0,0,0,0,0,0,0,0,0},
            new byte[15] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            new byte[16] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            new byte[17] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
        };

        yield return new object[] { AychOffsetOne, seconds[0], false, 0 };
        foreach (var second in seconds.Skip(1))
        {
            yield return new object[] { AychOffsetOne, second, true, 1 };
        }

        yield return new object[] { HelloWorldOffsetOne, seconds[0], false, 0 };
        yield return new object[] { HelloWorldOffsetOne, seconds[1], false, 1 };
        yield return new object[] { HelloWorldOffsetOne, seconds[2], false, 7 };
        yield return new object[] { HelloWorldOffsetOne, seconds[3], false, 8 };
        yield return new object[] { HelloWorldOffsetOne, seconds[4], false, 9 };
        foreach (var second in seconds.Skip(5))
        {
            yield return new object[] { HelloWorldOffsetOne, second, true, 11 };
        }
    }

    [Theory]
    [MemberData(nameof(AlignedValids))]
    public void ReadToZeroAligned(byte[] from, byte[] to, bool expect, int expected)
    {
        // Given
        var extractor = ReadOnlyAlignedBitSpan.Create(from);
        BitExtractor data = default;

        // When
        var result = extractor.TryCopyBytesUntilZeroLSB(ref data, 0, to, out var copied);

        // Then
        Assert.Equal(expect, result);
        Assert.Equal(expected, copied);
    }

    [Theory]
    [MemberData(nameof(OffsetValids))]
    public void ReadToZeroOffset(byte[] from, byte[] to, bool expect, int expected)
    {
        // Given
        var extractor = ReadOnlyAlignedBitSpan.Create(from, 1);
        BitExtractor data = default;

        // When
        var result = extractor.TryCopyBytesUntilZeroLSB(ref data, 0, to, out var copied);

        // Then
        Assert.Equal(expect, result);
        Assert.Equal(expected, copied);
    }

    [Fact]
    public void AlignedExtractorCreation()
    {
        // Given
        Span<byte> buffer = [0x1d];

        // When
        var extractor = ReadOnlyAlignedBitSpan.Create(buffer);

        // Then
        Assert.Equal(8, extractor.Length);
    }

    [Fact]
    public void AlignedExtractorLocalData()
    {
        // Given
        Span<byte> buffer = [0x1d];
        var extractor = ReadOnlyAlignedBitSpan.Create(buffer);

        // When
        Assert.True(extractor.TryGetExtractorLSB(0, 8, out var data));

        // Then
        Assert.True(data.TryExtractUnsignedLSB(0, 1, out int value));
        Assert.Equal(1, value);
        Assert.True(data.TryExtractUnsignedLSB(1, 2, out value));
        Assert.Equal(2, value);
        Assert.True(data.TryExtractUnsignedLSB(3, 3, out value));
        Assert.Equal(3, value);
    }
}
