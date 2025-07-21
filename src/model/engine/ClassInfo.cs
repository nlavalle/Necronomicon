namespace necronomicon.model.engine;

public class ClassInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Serializer { get; set; } = string.Empty;

    public ClassInfo(int id, string name, string serializer)
    {
        Id = id;
        Name = name;
        Serializer = serializer;
    }
}