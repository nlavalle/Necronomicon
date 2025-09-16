using System.Buffers;

namespace necronomicon.processor;

public static class ProcessHelpers
{
    public static Span<byte> ResolveArray(scoped ref byte[]? array, int dataSize)
    {
        var rental = array;
        if (rental is null)
        {
            rental = ArrayPool<byte>.Shared.Rent(dataSize);
            array = rental;
        }
        else
        {
            if (rental.Length < dataSize)
            {
                ArrayPool<byte>.Shared.Return(rental);
                rental = ArrayPool<byte>.Shared.Rent(dataSize);
                array = rental;
            }
        }

        return rental.AsSpan(0, dataSize);
    }
    
}
