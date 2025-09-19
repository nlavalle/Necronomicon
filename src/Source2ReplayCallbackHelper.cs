namespace necronomicon;

public readonly struct Source2ReplayCallbackHelper
{
    public Source2ReplayFrameCallbacks FrameCallbacks { get; }
    public Source2ReplayMessageCallbacks.Builder MessageCallbacksBuilder { get; }

    public Source2ReplayCallbackHelper(Source2ReplayFrameCallbacks frameCallbacks, Source2ReplayMessageCallbacks.Builder? builder = null)
    {
        FrameCallbacks = frameCallbacks;
        MessageCallbacksBuilder = builder ?? new Source2ReplayMessageCallbacks.Builder();
    }
}
