namespace necronomicon;

public interface IReplayMetaElement<TData>
    where TData : allows ref struct
{
    public void RegisterCallbackForData(Action<TData> callback);
}
