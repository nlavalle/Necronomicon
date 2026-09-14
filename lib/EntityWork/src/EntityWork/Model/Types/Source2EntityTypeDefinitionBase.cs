using System.Diagnostics;

namespace EntityWork.Model.Types;

internal abstract class Source2EntityTypeDefinitionBase
    : IEquatable<Source2EntityTypeDefinitionBase>
{
    private int _position = -1;
    private int _sizeOf = -1;

    public abstract string TypeName { get; }
    public int SizeOf => _sizeOf;
    public virtual int AlignOf => _sizeOf != 0 ? _sizeOf : 1;
    public virtual bool IsHeapType => false;
    public virtual bool IsFallbackType => false;

    internal virtual EntityFieldDecoder? GetOuterDecoder(Source2EntityFlags flags) => null;

    internal int GetOrPlace(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags)
    {
        var position = _position;
        if (position < 0)
        {
            throw new InvalidOperationException("Space for type was not reserved in reservation phase.");
        }

        int sizeOf = _sizeOf;
        if (sizeOf < 0)
        {
            Span<Source2EntityTypeDatabase.Type> span;

            if (position == 0)
            {
                span = builder.AllocateType(out position);
                _position = position;
            }
            else
            {
                span = builder.GetType(position);
            }

            sizeOf = PlaceInternal(builder, parentFlags, ref span[0], position);
            if (sizeOf < 0)
            {
                throw new Exception();
            }

            var alignOf = AlignOf - 1;
            if (alignOf > 0)
            {
                sizeOf = (sizeOf + alignOf) & ~alignOf;
            }

            _sizeOf = sizeOf;
        }

        return position;
    }

    internal abstract int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId);

    internal void Reserve(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef, int position = 0)
    {
        Debug.Assert(position >= 0);

        var previous = _position;
        if (previous < 0)
        {
            ReserveInternal(builder, serDef);
            _position = position;
        }
        else
        {
            if (previous == 0)
            {
                _position = position;
            }
            else if (previous != position)
            {
                throw new InvalidOperationException("Attempt to change position of type is not supported.");
            }
        }
    }

    protected abstract void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef);

    public virtual bool Equals(Source2EntityTypeDefinitionBase? other)
    {
        return this == other;
    }
}
