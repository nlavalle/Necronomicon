using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class DynamicArrayTemplateFactory
    : TemplateTypeDefinitionFactory
{
    private readonly Dictionary<string, Dictionary<Source2EntityTypeDefinitionBase, DynamicArrayTypeDefinition>> _cache;
    private readonly Dictionary<string, Dictionary<Source2EntityTypeDefinitionBase, DynamicArrayTypeDefinition>>.AlternateLookup<ReadOnlySpan<char>> _lookup;


    public DynamicArrayTemplateFactory()
    {
        _cache = new();
        _lookup = _cache.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field, string complete, int outerStart, int outerLength, int innerStart, int innerLength)
    {
        var outerName = complete.AsSpan(outerStart, outerLength);
        if (!_lookup.TryGetValue(outerName, out var typeCache))
        {
            typeCache = new();
            if (!_lookup.TryAdd(outerName, typeCache))
                throw new Exception();
        }

        var inner = builder.CreateParsedType(field, complete, innerStart, innerLength);

        if (!typeCache.TryGetValue(inner, out var type))
        {
            StateGuy state;

            if (inner.IsFallbackType)
            {
                state = new(outerName, complete.AsSpan(innerStart, innerLength));
            }
            else
            {
                state = new(outerName, inner.TypeName);
            }

            var name = string.Create(state.Length, state, (span, state) =>
            {
                state.Outer.CopyTo(span);
                span = span.Slice(state.Outer.Length);
                span[0] = '<';
                span = span.Slice(1);
                state.Inner.CopyTo(span);
                span = span.Slice(state.Inner.Length);
                span[0] = '>';
            });

            type = new(inner, name);
            typeCache.Add(inner, type);
        }

        return type;
    }

    internal readonly ref struct StateGuy
    {
        internal readonly ReadOnlySpan<char> Outer;
        internal readonly ReadOnlySpan<char> Inner;

        internal int Length => Outer.Length + Inner.Length + 2;

        public StateGuy(ReadOnlySpan<char> outer, ReadOnlySpan<char> inner)
        {
            Outer = outer;
            Inner = inner;
        }
    }

}
