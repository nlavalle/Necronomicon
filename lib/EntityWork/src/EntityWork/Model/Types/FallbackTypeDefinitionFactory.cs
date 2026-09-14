using BitWork;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class FallbackTypeDefinitionFactory
{
    private readonly FallbackTypeDefinition _fixed;
    private readonly FallbackTypeDefinition _variable;
    private readonly FallbackTypeDefinition _fixedPtr;
    private readonly FallbackTypeDefinition _variablePtr;

    public FallbackTypeDefinitionFactory()
    {
        _fixed = new("__fallback_fixed", UInt64TypeDefinitionFactory.UInt64FixedTypeDefinition.InvokeDelegate, Source2EntityFlags.None);
        _variable = new("__fallback_var", UnsignedIntTypeDefinitionFactory<ulong>.UnsignedIntTypeDefinition.InvokeDelegate64, Source2EntityFlags.None);
        _fixedPtr = new("__pointer_fixed", UInt64TypeDefinitionFactory.UInt64FixedTypeDefinition.InvokeDelegate, Source2EntityFlags.Exclude);
        _variablePtr = new("__pointer_var", UnsignedIntTypeDefinitionFactory<ulong>.UnsignedIntTypeDefinition.InvokeDelegate64, Source2EntityFlags.Exclude);
    }

    public Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field, bool pointer)
    {
        if (field is not null && field.HasVarEncoderSym)
        {
            if (builder.Serializer.Symbols[field.VarEncoderSym] == "fixed64")
            {
                if (pointer)
                    return _fixedPtr;

                return _fixed;
            }
        }

        if (pointer)
            return _variablePtr;
            
        return _variable;
    }

    internal sealed class FallbackTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private readonly string _typeName;
        private readonly EntityFieldDecoder _decoder;
        private readonly Source2EntityFlags _flags;

        public override string TypeName => _typeName;
        public override bool IsFallbackType => true;

        public FallbackTypeDefinition(string typeName, EntityFieldDecoder decoder, Source2EntityFlags flags)
        {
            _typeName = typeName;
            _decoder = decoder;
            _flags = flags;
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(_decoder, null, sym, _flags, TypeCode.UInt64, sizeof(ulong), 0, 0);

            return sizeof(ulong);
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }
    }
}
