using BitWork;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class StringTypeDefinitionFactory
    : Source2EntityTypeDefinitionFactory
{
    private readonly DynamicArrayTypeDefinition _only;

    public StringTypeDefinitionFactory(CharTypeDefinitionFactory charFactory)
    {
        _only = new(charFactory._definition, "string");
    }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
    {
        return _only;
    }
}
