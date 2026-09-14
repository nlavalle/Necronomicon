using System.Diagnostics;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class VectorTypeDefinitionFactoryFactory
{
    private readonly DimensionalVectorFieldEnclosure _inner;

    public VectorTypeDefinitionFactoryFactory(FloatTypeDefinitionFactory floatFactory)
    {
        var inner = new DimensionalVectorFieldEnclosure(floatFactory.ReadOnlyDefinition, 4);
        _inner = inner;
    }

    public VectorTypeDefinitionFactory CreateFactory(int count)
    {
        return new(count, _inner);
    }

    internal sealed class VectorTypeDefinitionFactory
        : Source2EntityTypeDefinitionFactory
    {
        private readonly int _count;
        private readonly DimensionalVectorFieldEnclosure _inner;

        private readonly VectorTypeDefinition _fixed;
        private readonly Dictionary<EntityFieldDecoder, VectorTypeDefinition> _cache;

        public VectorTypeDefinitionFactory(int count, DimensionalVectorFieldEnclosure inner)
        {
            _count = count;
            _inner = inner;
            _fixed = new(FloatFieldDecoderFactory.CreateFixed(count), inner, count, "Vector" + count);
            _cache = new();
        }

        public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
        {
            Debug.Assert(field is not null);

            if ((field.BitCount & 31) == 0)
            {
                return _fixed;
            }

            var decoder = FloatFieldDecoderFactory.Singleton.GetFieldDecoder(field, _count);

            if (!_cache.TryGetValue(decoder, out var type))
            {
                type = new(decoder, _inner, _count, builder.Serializer.Symbols[field.VarTypeSym]);
                _cache.Add(decoder, type);
            }

            return type;
        }
    }
    
    public sealed class VectorTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private readonly EntityFieldDecoder _delly;
        private readonly DimensionalVectorFieldEnclosure _inner;
        private readonly int _count;
        private readonly string _name;

        public override string TypeName => _name;
        public override bool IsFallbackType => true;
        public override int AlignOf => _inner.AlignOf;

        public VectorTypeDefinition(EntityFieldDecoder decoder, DimensionalVectorFieldEnclosure inner, int count, string name)
        {
            _delly = decoder;
            _inner = inner;
            _count = count;
            _name = name;
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            _inner.Reserve(builder, serDef);

            builder.ReserveType();
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sizeOf = sizeof(float) * _count;

            var index = _inner.GetOrPlace(builder, Source2EntityInclusionFlags.None);

            var sym = builder.GetOrPlaceSymbol(_name);
            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, Source2EntityFlags.None, TypeCode.Object, sizeOf, index, _count);

            return sizeOf;
        }
    }
}

