using Steam.Protos.Dota2;

namespace necronomicon.model;

public class DemPacket
{
    private static readonly int isCompressedConstant = (int)EDemoCommands.DemIsCompressed;
    private static readonly int notCompressedConstant = ~isCompressedConstant;
    private uint _frameTick;
    public bool IsCompressed { get; private set; }
    public EDemoCommands DemoCommand { get; private set; }
    public uint FrameTick
    {
        get { return _frameTick; }
    }

    internal DemPacket(int command, uint tick)
    {
        DemoCommand = (EDemoCommands)(command & notCompressedConstant);
        if ((command & isCompressedConstant) == isCompressedConstant)
            IsCompressed = true;
        _frameTick = tick;
    }

}