using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal abstract class TemplateTypeDefinitionFactory
{
    public abstract Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field, string complete, int outerStart, int outerLength, int innerStart, int innerLength);
}
