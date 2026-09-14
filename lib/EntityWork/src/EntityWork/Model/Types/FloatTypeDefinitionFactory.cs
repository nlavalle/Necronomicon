using System.Diagnostics;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed partial class FloatTypeDefinitionFactory
    : Source2EntityTypeDefinitionFactory
{
    private readonly FloatTypeDefinition _fixed = new(FloatFieldDecoderFactory.Fixed);
    private readonly FloatTypeDefinition _readOnly = new(null);

    private readonly Dictionary<int, Source2EntityTypeDefinitionBase> _encoders = new();
    private readonly Dictionary<FloatFieldDecoderFactory.CacheKey, FloatTypeDefinition> _types = new();

    private readonly UnsignedIntTypeDefinitionFactory<uint> _uint32Factory;

    internal FloatTypeDefinition ReadOnlyDefinition => _readOnly;

    public FloatTypeDefinitionFactory(UnsignedIntTypeDefinitionFactory<uint> uint32Factory)
    {
        _uint32Factory = uint32Factory;
    }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
    {
        if (field is not null)
        {
            if (field.HasVarEncoderSym)
            {
                var encoderId = field.VarEncoderSym;
                if (_encoders.TryGetValue(encoderId, out var decoder))
                    return decoder;

                var encoder = builder.Serializer.Symbols[encoderId];
                switch (encoder)
                {
                    case "coord":
                        decoder = new FloatTypeDefinition(FloatFieldDecoderFactory.Coord);
                        _encoders.Add(encoderId, decoder);
                        return decoder;
                    case "simtime":
                        decoder = _uint32Factory.GetTypeDefinition(builder, field);
                        _encoders.Add(encoderId, decoder);
                        return decoder;
                }
            }

            var bitCount = field.BitCount;
            if (((uint)bitCount - 1) < 31)
            {
                Debug.Assert(bitCount > 0 && bitCount < 32);

                uint lowInt = BitConverter.SingleToUInt32Bits(field.LowValue),
                    highInt = BitConverter.SingleToUInt32Bits(field.HighValue);
                var flags = field.EncodeFlags;

                var lookup = new FloatFieldDecoderFactory.CacheKey(lowInt, highInt, flags, bitCount);
                if (!_types.TryGetValue(lookup, out var value))
                {
                    var encoder = FloatFieldDecoderFactory.Singleton.GetFieldDecoder(field);
                    value = new(encoder);
                    _types.Add(lookup, value);
                }

                return value;
            }

            return _fixed;
        }

        return _readOnly;
    }

    public sealed class FloatTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private readonly EntityFieldDecoder? _delly;

        public override string TypeName => "float32";

        internal FloatTypeDefinition(EntityFieldDecoder? decoder)
        {
            _delly = decoder;
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, Source2EntityFlags.None, TypeCode.Single, sizeof(float), 0, 0);

            return sizeof(float);
        }
    }
}

