using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using necronomicon.model;
using necronomicon.source;
using Snappier;
using Steam.Protos.Dota2;

namespace necronomicon;

public static class Source2Replay
{
    internal static void ThrowIfMagicInvalid(EngineMagicHeader magic)
    {
        if (magic != EngineMagicHeader.SOURCE_2)
            throw new Exception(); // TODO Specify
    }

    internal async static ValueTask Start(PipeReader reader, ReadResult result, Action<Frame> callback, Func<ValueTask>? completion)
    {
        var frameCallback = callback;
        var writer = new ArrayBufferWriter<byte>(4096);

        result = await SkipBytes(reader, result, 8).ConfigureAwait(false);

        while (true)
        {
            while (!result.Buffer.IsEmpty)
            {
                var frame = await ReadFrameMetadata(reader, result).ConfigureAwait(false);
                result = frame.ReadResult;

                Action<FrameData>? dataCallback = null;

                frameCallback(new Frame(ref dataCallback, ref frameCallback, in frame.FrameMetadata));

                var dataSize = frame.FrameMetadata.DataSize;

                if (dataCallback == null)
                {
                    result = await SkipBytes(reader, result, dataSize).ConfigureAwait(false);
                }
                else
                {
                    result = await ReadBytes(reader, result, dataSize).ConfigureAwait(false);

                    dataCallback(new FrameData(ref frameCallback, result.Buffer.Slice(0, dataSize), writer, in frame.FrameMetadata));

                    writer.ResetWrittenCount();

                    result = result.Slice(dataSize);
                }

            }

            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            if (result.IsCompleted)
            {
                await reader.CompleteAsync().ConfigureAwait(false);
                break;
            }

            result = await reader.ReadAsync().ConfigureAwait(false);
        }

        if (completion is not null)
            await completion();
    }

    private static ValueTask<ReadResult> SkipBytes(PipeReader reader, ReadResult result, long count)
    {
        var buffer = result.Buffer;

        while (buffer.Length < count)
        {
            count -= buffer.Length;

            reader.AdvanceTo(buffer.End, buffer.End);

            if (result.IsCompleted)
                return ValueTask.FromException<ReadResult>(new Exception());

            var task = reader.ReadAsync();
            if (task.IsCompletedSuccessfully)
            {
                result = task.Result;
                buffer = result.Buffer;
            }
            else
            {
                return new ValueTask<ReadResult>(ConvertToAsync(reader, task, count));
            }
        }

        return new ValueTask<ReadResult>(result.Slice(count));

        static async Task<ReadResult> ConvertToAsync(PipeReader reader, ValueTask<ReadResult> task, long count)
        {
            var result = await task;
            var buffer = result.Buffer;

            while (buffer.Length < count)
            {
                count -= buffer.Length;

                reader.AdvanceTo(buffer.End, buffer.End);

                if (result.IsCompleted)
                    throw new Exception();

                result = await reader.ReadAsync();
                buffer = result.Buffer;
            }

            return result.Slice(count);
        }
    }

    private static ValueTask<ReadResult> ReadBytes(PipeReader reader, ReadResult result, int count)
    {
        if (result.Buffer.Length < count)
        {
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            if (result.IsCompleted)
                return ValueTask.FromException<ReadResult>(new Exception());

            return reader.ReadAtLeastAsync(count);
        }
        else
        {
            return new ValueTask<ReadResult>(result);
        }
    }

    private static ValueTask<FrameMetadataResult> ReadFrameMetadata(PipeReader input, ReadResult result)
    {
        FrameMetadata frame;
        var buffer = result.Buffer;

        var position = buffer.Start;

        while (!TryReadFrameMetadata(buffer, ref position, out frame))
        {
            input.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                return ValueTask.FromException<FrameMetadataResult>(new InvalidDataException());
            }

            var task = input.ReadAsync();
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask<FrameMetadataResult>(ConvertToAsync(input, task));
            }

            result = task.Result;
            buffer = result.Buffer;
            position = buffer.Start;
        }

        return new ValueTask<FrameMetadataResult>(new FrameMetadataResult(result.Slice(position), frame));

        static async Task<FrameMetadataResult> ConvertToAsync(PipeReader input, ValueTask<ReadResult> task)
        {
            FrameMetadata frame;

            var result = await task.ConfigureAwait(false);

            var buffer = result.Buffer;
            var position = buffer.Start;

            while (!TryReadFrameMetadata(buffer, ref position, out frame))
            {
                input.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    throw new InvalidDataException();
                }

                result = await input.ReadAsync();
                buffer = result.Buffer;
                position = buffer.Start;
            }

            return new FrameMetadataResult(result.Slice(position), frame);
        }
    }

    public static bool TryReadFrameMetadata(ref ReadOnlySequence<byte> buffer, out FrameMetadata frame)
    {
        var position = buffer.Start;

        if (TryReadFrameMetadata(buffer, ref position, out frame))
        {
            buffer = buffer.Slice(position);
            return true;
        }

        return false;
    }

    public static bool TryReadFrameMetadata(ReadOnlySequence<byte> buffer, ref SequencePosition position, out FrameMetadata frame)
    {
        var local = position;

        if (InputStreamSource.TryReadVarInt32(buffer, ref local, out var encodedCommand)
            && InputStreamSource.TryReadVarInt32(buffer, ref local, out var frameTick)
            && InputStreamSource.TryReadVarInt32(buffer, ref local, out var dataSize)
        )
        {
            position = local;
            frame = new FrameMetadata(encodedCommand, frameTick, dataSize);
            return true;
        }

        frame = default;
        return false;
    }

    public readonly ref struct Frame
        : IReplayMetaElement<FrameData>
    {
        private readonly ref Action<FrameData>? _callback;
        private readonly ref Action<Frame> _next;
        private readonly ref readonly FrameMetadata _frame;

        public EDemoCommands FrameCommand => !Unsafe.IsNullRef(in _frame) ? _frame.Command : default;
        public int FrameTick => !Unsafe.IsNullRef(in _frame) ? _frame.FrameTick : default;

        internal Frame(ref Action<FrameData>? callback, ref Action<Frame> next, ref readonly FrameMetadata frame)
        {
            _callback = ref callback;
            _next = ref next;
            _frame = ref frame;
        }

        public void ChangeCallbackForNextFrame(Action<Frame> callback)
        {
            if (Unsafe.IsNullRef(ref _next))
                throw new NullReferenceException();

            _next = callback;
        }

        public void RegisterCallbackForData(Action<FrameData> callback)
        {
            if (Unsafe.IsNullRef(ref _callback))
                throw new NullReferenceException();

            _callback = callback;
        }
    }

    public readonly ref struct FrameData
    {
        private readonly ref Action<Frame> _next;
        private readonly ReadOnlySequence<byte> _buffer;
        private readonly ArrayBufferWriter<byte> _writer;
        private readonly ref readonly FrameMetadata _frame;

        private bool IsCompressed => !Unsafe.IsNullRef(in _frame) && _frame.IsCompressed;

        public EDemoCommands FrameCommand => !Unsafe.IsNullRef(in _frame) ? _frame.Command : default;
        public int FrameTick => !Unsafe.IsNullRef(in _frame) ? _frame.FrameTick : default;

        internal FrameData(ref Action<Frame> next, ReadOnlySequence<byte> buffer, ArrayBufferWriter<byte> writer, ref readonly FrameMetadata frame)
        {
            _next = ref next;
            _buffer = buffer;
            _writer = writer;
            _frame = ref frame;
        }

        public void ChangeCallbackForNextFrame(Action<Frame> callback)
        {
            if (Unsafe.IsNullRef(ref _next))
                throw new NullReferenceException();

            _next = callback;
        }

        public TProtobuf GetAsProtobuf<TProtobuf>() where TProtobuf : class, IMessage<TProtobuf>, new()
        {
            var protobuf = new TProtobuf();

            if (IsCompressed)
            {
                if (_writer.WrittenCount == 0)
                {
                    Snappy.Decompress(_buffer, _writer);
                }

                protobuf.MergeFrom(_writer.WrittenSpan);
            }
            else
            {
                protobuf.MergeFrom(_buffer);
            }

            return protobuf;
        }
    }

    internal readonly struct FrameMetadataResult
    {
        public readonly ReadResult ReadResult;
        public readonly FrameMetadata FrameMetadata;

        public FrameMetadataResult(ReadResult result, FrameMetadata frameMetatdata)
        {
            ReadResult = result;
            FrameMetadata = frameMetatdata;
        }
    }

    public readonly struct FrameMetadata
    {
        private readonly int _encodedCommand;

        public int FrameTick { get; }
        public int DataSize { get; }

        public EDemoCommands Command => (EDemoCommands)_encodedCommand & ~EDemoCommands.DemIsCompressed;
        public bool IsCompressed => ((EDemoCommands)_encodedCommand & EDemoCommands.DemIsCompressed) != 0;

        public FrameMetadata(int encodedCommand, int frameTick, int dataSize)
        {
            _encodedCommand = encodedCommand;
            FrameTick = frameTick;
            DataSize = dataSize;
        }
    }
}
