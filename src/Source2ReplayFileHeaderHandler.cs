using Steam.Protos.Dota2;

namespace necronomicon;

public sealed class Source2ReplayFileHeaderHandler
    : ReplayElementHandler<Source2Replay.Frame, Source2Replay.FrameData>
{
    private readonly Source2ReplayFrameCallbacks _callbacks;
    private readonly Action<InterestHelper>? _onFileHeader;

    public Source2ReplayFileHeaderHandler(Source2ReplayFrameCallbacks callbacks)
    {
        _callbacks = callbacks;
    }

    public Source2ReplayFileHeaderHandler(Source2ReplayFrameCallbacks callbacks, Action<InterestHelper> onFileHeader)
    {
        _callbacks = callbacks;
        _onFileHeader = onFileHeader;
    }

    public override void OnFrameData(Source2Replay.FrameData data)
    {
        var header = data.GetAsProtobuf<CDemoFileHeader>();

        if (_onFileHeader is not null)
            _onFileHeader(new InterestHelper(_callbacks, header));

        _callbacks[EDemoCommands.DemFileHeader] = null;
    }

    public readonly ref struct InterestHelper
    {
        private readonly Source2ReplayFrameCallbacks _callbacks;

        public CDemoFileHeader FileHeader { get; }

        internal InterestHelper(Source2ReplayFrameCallbacks callbacks, CDemoFileHeader header)
        {
            _callbacks = callbacks;
            FileHeader = header;
        }
    }
}
