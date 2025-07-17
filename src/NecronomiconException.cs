namespace necronomicon;

public class NecronomiconException : Exception
{
    public NecronomiconException(string message, Exception innerException) : base(message, innerException)
    {    
    }

    public NecronomiconException(string message) : base(message)
    {
    }    
}