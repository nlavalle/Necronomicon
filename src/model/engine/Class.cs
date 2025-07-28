namespace necronomicon.model.engine;

public class Class
{
    public int ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Serializer Serializer { get; set; }

    public Class(int id, string name, Serializer serializer)
    {
        ClassId = id;
        Name = name;
        Serializer = serializer;
    }
}