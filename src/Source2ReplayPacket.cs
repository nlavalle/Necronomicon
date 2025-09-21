using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using necronomicon.processor;

namespace necronomicon;

public static class Source2ReplayPacket
{
    public static void ProcessPacketMessages(ReadOnlySpan<byte> buffer, int tick, Action<Message> callback)
    {
        var messageCallback = callback;
        var bitReader = BitSpanLSBReader.Create(buffer);
        byte[]? messageBuffer = null;

        try
        {
            while (bitReader.TryReadEmbeddedInt(out uint messageType))
            {
                if (messageType == 0 || !bitReader.TryReadVarInt32(out var dataSize))
                    break;

                Debug.Assert(
                    dataSize >= 0);
                Debug.Assert(
                    bitReader.Remaining >= dataSize,
                    "Fucked up your embedded packet size bro");

                Action<MessageData>? dataCallback = null;

                messageCallback(new Message(ref dataCallback, ref messageCallback, messageType, tick));

                if (dataCallback is not null)
                {
                    ReadOnlySpan<byte> messageSpan;

                    // If bitReader is aligned to byte, no copy needed
                    if (bitReader.TryGetByteAlignedBuffer(dataSize, out messageSpan))
                    {
                        bitReader.TryAdvance(dataSize * 8);
                    }
                    else
                    {
                        if (messageBuffer is null)
                        {
                            messageBuffer = ArrayPool<byte>.Shared.Rent(dataSize);
                        }
                        else if (messageBuffer.Length < dataSize)
                        {
                            ArrayPool<byte>.Shared.Return(messageBuffer);
                            messageBuffer = ArrayPool<byte>.Shared.Rent(dataSize);
                        }

                        var copyTo = messageBuffer.AsSpan(0, dataSize);
                        if (!bitReader.TryCopyTo(copyTo))
                            throw new Exception();

                        messageSpan = copyTo;
                    }

                    dataCallback(new MessageData(ref messageCallback, messageType, tick, messageSpan));
                }
                else
                {
                    if (!bitReader.TryAdvance(dataSize * 8))
                        throw new Exception();
                }
            }
        }
        finally
        {
            if (messageBuffer is not null)
                ArrayPool<byte>.Shared.Return(messageBuffer);
        }
    }

    public readonly ref struct Message
        : IReplayMetaElement<MessageData>
    {
        private readonly ref Action<MessageData>? _callback;
        private readonly ref Action<Message> _next;

        public uint MessageType { get; }
        public int Tick { get; }

        internal Message(ref Action<MessageData>? callback, ref Action<Message> next, uint encodedType, int tick)
        {
            _callback = ref callback;
            _next = ref next;
            MessageType = encodedType;
            Tick = tick;
        }

        public void ChangeCallbackForNextMessage(Action<Message> callback)
        {
            if (Unsafe.IsNullRef(ref _next))
                throw new NullReferenceException();

            _next = callback;
        }

        public void RegisterCallbackForData(Action<MessageData> callback)
        {
            if (Unsafe.IsNullRef(ref _callback))
                throw new NullReferenceException();

            _callback = callback;
        }
    }

    public readonly ref struct MessageData
    {
        private readonly ref Action<Message> _next;
        private readonly ReadOnlySpan<byte> _buffer;

        public uint MessageType { get; }
        public int Tick { get; }

        internal MessageData(ref Action<Message> next, uint encodedType, int tick, ReadOnlySpan<byte> buffer)
        {
            _next = ref next;
            _buffer = buffer;
            MessageType = encodedType;
            Tick = tick;
        }

        public TProtobuf GetAsProtobuf<TProtobuf>()
            where TProtobuf : class, IMessage<TProtobuf>, new()
        {
            var protobuf = new TProtobuf();

            protobuf.MergeFrom(_buffer);

            return protobuf;
        }
    }
}
