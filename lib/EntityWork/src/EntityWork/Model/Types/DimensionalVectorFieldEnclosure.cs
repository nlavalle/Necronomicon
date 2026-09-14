using System.Diagnostics;

namespace EntityWork.Model.Types;

internal sealed class DimensionalVectorFieldEnclosure
{
    private static readonly string[] FieldNames = ["X", "Y", "Z", "W"];

    private readonly Source2EntityTypeDefinitionBase _inner;
    private readonly int _limit;

    public int InnerSizeOf => _inner.SizeOf;
    public int AlignOf => _inner.AlignOf;

    private int _fieldIndex = -1;

    public DimensionalVectorFieldEnclosure(Source2EntityTypeDefinitionBase inner, int limit = 4)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)limit - 1, 3U, nameof(limit));

        _inner = inner;
        _limit = limit;
    }

    internal void Reserve(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
    {
        if (_fieldIndex < 0)
        {
            _inner.Reserve(builder, serDef);

            builder.ReserveFieldCount(_limit);

            _fieldIndex = 0;
        }
    }

    internal int GetOrPlace(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags)
    {
        var fieldIndex = _fieldIndex;
        Debug.Assert(fieldIndex >= 0);

        if (fieldIndex == 0)
        {
            var innerId = _inner.GetOrPlace(builder, Source2EntityInclusionFlags.Include);
            var sizeOf = _inner.SizeOf;

            var fields = builder.AllocateFields(_limit, out fieldIndex);

            for (int i = 0; i < fields.Length; i++)
            {
                var sym = builder.GetOrPlaceSymbol(FieldNames[i]);
                fields[i] = new Source2EntityTypeDatabase.Field(sym, 0, Source2EntityFlags.None, innerId, sizeOf * i);
            }

            _fieldIndex = fieldIndex;
        }

        return fieldIndex;
    }
}

