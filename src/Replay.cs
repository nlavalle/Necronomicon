using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using necronomicon.model;

namespace necronomicon;

public static class Replay
{
    internal const int BufferSize = 65536;

    public static ValueTask ParseFileAsync(Stream stream, Action<File> callback)
    {
        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: BufferSize));

        var task = reader.ReadAtLeastAsync(sizeof(EngineMagicHeader));
        if (task.IsCompletedSuccessfully)
        {
            var result = task.Result;
            var magic = ReadEngineMagicHeader(result.Buffer);
            (var inner, var completion) = DoCallback(magic, callback);

            if (inner is not null)
            {
                return MoveToInner(reader, result.Slice(sizeof(EngineMagicHeader)), magic, inner, completion);
            }
            else
            {
                var completeTask = reader.CompleteAsync();
                if (completeTask.IsCompletedSuccessfully)
                {
                    if (completion is not null)
                        return completion();

                    return completeTask;
                }
                else
                {
                    return new ValueTask(ConvertCompletionToAsync(completeTask, completion));
                }
            }
        }
        else
        {
            return new ValueTask(ConvertToAsync(reader, task, callback));
        }

        static (object?, Func<ValueTask>?) DoCallback(EngineMagicHeader magic, Action<File> callback)
        {
            object? inner = null;
            Func<ValueTask>? completion = null;

            callback(new File(ref inner, ref completion, magic));

            return (inner, completion);
        }

        static async Task ConvertToAsync(PipeReader reader, ValueTask<ReadResult> task, Action<File> callback)
        {
            var result = await task.ConfigureAwait(false);

            var magic = ReadEngineMagicHeader(result.Buffer);
            (var inner, var completion) = DoCallback(magic, callback);

            if (inner is not null)
            {
                await MoveToInner(reader, result.Slice(sizeof(EngineMagicHeader)), magic, inner, completion).ConfigureAwait(false);
            }
            else
            {
                await reader.CompleteAsync().ConfigureAwait(false);

                if (completion is not null)
                    await completion().ConfigureAwait(false);
            }
        }

        static async Task ConvertCompletionToAsync(ValueTask task, Func<ValueTask>? completion)
        {
            await task.ConfigureAwait(false);

            if (completion is not null)
                await completion().ConfigureAwait(false);
        }
    }

    private static ValueTask MoveToInner(PipeReader reader, ReadResult result, EngineMagicHeader magic, object callback, Func<ValueTask>? completion)
    {
        return magic switch
        {
            EngineMagicHeader.SOURCE_2 => Source2Replay.Start(reader, result, (Action<Source2Replay.Frame>)callback, completion),
            _ => throw new Exception()
        };
    }

    public static EngineMagicHeader ReadEngineMagicHeader(ReadOnlySequence<byte> buffer)
    {
        const int SizeOf = sizeof(EngineMagicHeader);
        Span<byte> temp = stackalloc byte[SizeOf];

        scoped ReadOnlySpan<byte> pass = buffer.FirstSpan;
        if (pass.Length >= SizeOf)
        {
            pass = pass[..SizeOf];
        }
        else
        {
            buffer.CopyTo(temp);
            pass = temp;
        }

        return (EngineMagicHeader)BinaryPrimitives.ReadInt64BigEndian(pass);
    }

    public readonly ref struct File
    {
        private readonly ref object? _callback;
        private readonly ref Func<ValueTask>? _completion;

        public readonly EngineMagicHeader MagicValue { get; }

        internal File(ref object? callback, ref Func<ValueTask>? completion, EngineMagicHeader magic)
        {
            _callback = ref callback;
            _completion = ref completion;
            MagicValue = magic;
        }

        public void ParseAsSource2Replay(Action<Source2ReplayFileHeaderHandler.InterestHelper> callback)
        {
            ParseAsSource2Replay(new Source2ReplayFrameCallbacks(callback));
        }

        public void ParseAsSource2Replay(Action<Source2Replay.Frame> callback)
        {
            if (Unsafe.IsNullRef(ref _callback))
                throw new NullReferenceException();

            if (_callback is not null)
            {
                throw new Exception(); // TODO Specify
            }

            Source2Replay.ThrowIfMagicInvalid(MagicValue);

            _callback = callback;
        }

        public void RegisterCompletionTask(Func<ValueTask> completion)
        {
            if (Unsafe.IsNullRef(ref _completion))
                throw new NullReferenceException();

            if (_completion is not null)
            {
                throw new Exception(); // TODO Specify
            }

            _completion = completion;
        }
    }
}
