using System.IO.Pipelines;

namespace necronomicon;

public static class ReadResultExtensions
{
    public static ReadResult Slice(this ReadResult result, long start)
    {
        return new ReadResult(result.Buffer.Slice(start), result.IsCanceled, result.IsCompleted);
    }

    public static ReadResult Slice(this ReadResult result, SequencePosition start)
    {
        return new ReadResult(result.Buffer.Slice(start), result.IsCanceled, result.IsCompleted);
    }
}
