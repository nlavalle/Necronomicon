namespace necronomicon;

public abstract class ReplayElementHandler<TMeta, TData>
    where TMeta : IReplayMetaElement<TData>, allows ref struct
    where TData : allows ref struct
{
    private readonly Action<TData> _delegate;

    public ReplayElementHandler()
    {
        _delegate = OnElementData;
    }

    public virtual void OnElement(TMeta frame)
        => frame.RegisterCallbackForData(_delegate);

    public abstract void OnElementData(TData data);

    public static implicit operator Action<TMeta>(ReplayElementHandler<TMeta, TData> handler)
        => handler.OnElement;

    public static implicit operator Action<TData>(ReplayElementHandler<TMeta, TData> handler)
        => handler._delegate;
}
