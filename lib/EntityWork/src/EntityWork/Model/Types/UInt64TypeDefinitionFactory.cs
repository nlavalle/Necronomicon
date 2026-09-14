using System.Runtime.InteropServices;
using BitWork;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class UInt64TypeDefinitionFactory
    : Source2EntityTypeDefinitionFactory
{
    private readonly UInt64FixedTypeDefinition _fixed;
    private readonly UnsignedIntTypeDefinitionFactory<ulong> _variable;

    public UInt64TypeDefinitionFactory(Source2EntityFlags flags = Source2EntityFlags.None)
    {
        _fixed = new(flags);
        _variable = new(flags);
    }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
    {
        if (field is not null && field.HasVarEncoderSym)
        {
            if (builder.Serializer.Symbols[field.VarEncoderSym] == "fixed64")
                return _fixed;
        }

        return _variable.GetTypeDefinition(builder, field);
    }

    internal sealed class UInt64FixedTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        internal readonly static EntityFieldDecoder InvokeDelegate = Invoke;
        private readonly Source2EntityFlags _flags;

        public override string TypeName => "uint64";

        public UInt64FixedTypeDefinition(Source2EntityFlags flags = Source2EntityFlags.None)
        {
            _flags = flags;
        }

        private static int Invoke(ref AlignedLsBitReader reader, in EntityWriter writer)
        {
            const int readBits = sizeof(ulong) * 8;
            bool result;

            if (writer.IsSkipped)
            {
                result = reader.TryAdvance(readBits);
                goto Exit;
            }

            result = reader.TryReadUnsigned<ulong>(readBits, out var read);

            MemoryMarshal.Write(writer.Dereference(), in read);

        Exit:
            if (!result)
                throw new Exception();

            return readBits;
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(InvokeDelegate, null, sym, _flags, TypeCode.UInt64, sizeof(ulong), 0, 0);

            return sizeof(ulong);
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }
    }
}
