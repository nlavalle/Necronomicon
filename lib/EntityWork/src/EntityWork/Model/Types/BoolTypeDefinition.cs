using System.Runtime.InteropServices;
using BitWork;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class BoolTypeDefinitionFactory
    : Source2EntityTypeDefinitionFactory
{
    private readonly BoolTypeDefinition _definition = new();

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
        => _definition;

    internal sealed class BoolTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private readonly static EntityFieldDecoder _delly = Invoke;

        public override string TypeName => "bool";

        private static int Invoke(ref AlignedLsBitReader reader, in EntityWriter writer)
        {
            bool result;

            if (writer.IsSkipped)
            {
                result = reader.TryAdvance(1);
                goto Exit;
            }

            result = reader.TryReadBool(out var read);

            MemoryMarshal.Write(writer.Dereference(), in read);

        Exit:
            if (!result)
                throw new Exception();

            return 1;
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, Source2EntityFlags.None, TypeCode.Boolean, sizeof(bool), 0, 0);

            return sizeof(bool);
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }
    }
}
