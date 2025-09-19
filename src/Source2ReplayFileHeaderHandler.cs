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

    public override void OnElementData(Source2Replay.FrameData data)
    {
        if (_onFileHeader is not null)
        {
            var header = data.GetAsProtobuf<CDemoFileHeader>();
            var pair = new Source2ReplayCallbackHelper(_callbacks);

            _onFileHeader(new InterestHelper(pair, header));

            var messageCallbacks = pair.MessageCallbacksBuilder.Complete();
            messageCallbacks.AttachToFrameCallbacks(pair.FrameCallbacks);
        }

        _callbacks[EDemoCommands.DemFileHeader] = null;
    }

    public readonly ref struct InterestHelper
    {
        public CDemoFileHeader FileHeader { get; }
        public Source2ReplayCallbackHelper CallbackHelper { get; }

        internal InterestHelper(Source2ReplayCallbackHelper callbacks, CDemoFileHeader header)
        {
            CallbackHelper = callbacks;
            FileHeader = header;
        }
    }
}
