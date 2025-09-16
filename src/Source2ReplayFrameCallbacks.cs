using System.Runtime.CompilerServices;
using Google.Protobuf;
using Steam.Protos.Dota2;

namespace necronomicon;

public sealed class Source2ReplayFrameCallbacks
{
    private const int Length = (int)EDemoCommands.DemMax;

    private Callbacks _callbacks;
    private Action<Source2Replay.Frame>? _unknown;

    public Source2ReplayFrameCallbacks()
    {
        this[EDemoCommands.DemFileHeader] = new Source2ReplayFileHeaderHandler(this);
    }

    public Source2ReplayFrameCallbacks(Action<Source2Replay.FrameData> headerCallback)
    {
        this[EDemoCommands.DemFileHeader] = headerCallback;
    }

    public Source2ReplayFrameCallbacks(Action<Source2ReplayFileHeaderHandler.InterestHelper> onFileHeader)
    {
        this[EDemoCommands.DemFileHeader] = new Source2ReplayFileHeaderHandler(this, onFileHeader);
    }

    public Action<Source2Replay.FrameData>? this[EDemoCommands command]
    {
        get
        {
            var index = (int)command;
            if ((uint)index >= Length)
                return null;

            return _callbacks[index];
        }

        set
        {
            var index = (int)command;
            if ((uint)index >= Length)
                throw new ArgumentOutOfRangeException(nameof(command));

            _callbacks[index] = value;
        }
    }

    public void SetUnknownCallback(Action<Source2Replay.Frame>? callback)
    {
        _unknown = callback;
    }

    public void ProcessFrame(Source2Replay.Frame frame)
    {
        var callback = this[frame.FrameCommand];
        if (callback is not null)
        {
            frame.RegisterCallbackForData(callback);
        }
        else if (_unknown is not null)
        {
            _unknown(frame);
        }
    }

    public static implicit operator Action<Source2Replay.Frame>(Source2ReplayFrameCallbacks callbacks)
        => callbacks.ProcessFrame;

    [InlineArray(Length)]
    private struct Callbacks
    {
        private Action<Source2Replay.FrameData>? _first;
    }
}
