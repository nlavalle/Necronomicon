using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal abstract class Source2EntityTypeDefinitionFactory
{
    public abstract Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field);
}
