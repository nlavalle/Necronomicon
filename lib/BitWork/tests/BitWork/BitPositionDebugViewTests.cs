using BitWork;

namespace Tests.BitWork;

public class BitPositionDebugViewTests
{
    [Fact]
    public void ConstructorFromBits64()
    {
        // Given
        var view = new BitPositionDebugView(BitPositionDebugView.InterpretationType.LsBit, "test", [0, 0, 0, 0, 0, 0, 0, 0], 0, 64);

        // Then
        Assert.Equal(string.Create(64, 64, (span, guy) => span.Fill('0')), view.Binary);
    }

    [Fact]
    public void ConstructorFromBits40()
    {
        // Given
        var view = new BitPositionDebugView(BitPositionDebugView.InterpretationType.LsBit, "test", [0, 0, 0, 0, 0, 0, 0, 0], 0, 40);

        // Then
        Assert.Equal(string.Create(40, 40, (span, guy) => span.Fill('0')), view.Binary);
    }

    [Fact]
    public void ConstructorFromBits10()
    {
        // Given
        var view = new BitPositionDebugView(BitPositionDebugView.InterpretationType.LsBit, "test", [0, 0, 0, 0, 0, 0, 0, 0], 0, 10);

        // Then
        Assert.Equal(string.Create(10, 10, (span, guy) => span.Fill('0')), view.Binary);
    }

    [Fact]
    public void ConstructorFromBytes72()
    {
        // Given
        var view = new BitPositionDebugView(BitPositionDebugView.InterpretationType.Byte, "test", [0, 0, 0, 0, 0, 0, 0, 0, 0], 0, 72);

        // Then
        Assert.Equal("00 00 00 00 00 00 00 00 ...", view.Binary);
    }

    [Fact]
    public void ConstructorFromBytes64()
    {
        // Given
        var view = new BitPositionDebugView(BitPositionDebugView.InterpretationType.Byte, "test", [0, 0, 0, 0, 0, 0, 0, 0], 0, 64);

        // Then
        Assert.Equal("00 00 00 00 00 00 00 00", view.Binary);
    }

    [Fact]
    public void ConstructorFromBytes40()
    {
        // Given
        var view = new BitPositionDebugView(BitPositionDebugView.InterpretationType.Byte, "test", [0, 0, 0, 0, 0], 0, 40);

        // Then
        Assert.Equal("00 00 00 00 00", view.Binary);
    }

}
