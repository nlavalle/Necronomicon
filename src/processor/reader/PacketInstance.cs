using Google.Protobuf;
using necronomicon.source;

namespace necronomicon.processor.reader;

public interface IPacketInstance<T> where T: IMessage {
    static readonly IPacketInstance<IMessage> EOF = new EofPacketInstance();

    int GetKind();
    int GetTick();
    Type GetMessageClass();
    ResetRelevantKind GetResetRelevantKind();
    T Parse();
    void Skip();
    private class EofPacketInstance : IPacketInstance<IMessage>
    {
        public int GetKind() => -1;

        public int GetTick() => int.MaxValue;

        public Type GetMessageClass() => null!;

        public ResetRelevantKind? GetResetRelevantKind() => null;

        public IMessage Parse()
        {
            throw new NecronomiconException("cannot parse EOF");
        }

        public void Skip()
        {
            throw new NecronomiconException("cannot skip EOF");
        }

        ResetRelevantKind IPacketInstance<IMessage>.GetResetRelevantKind()
        {
            throw new NotImplementedException();
        }
    }
}
