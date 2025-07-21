namespace necronomicon.model;

[Flags]
public enum EntityOp
{
    None = 0x00,
    Created = 0x01,
    Updated = 0x02,
    Deleted = 0x03,
    Entered = 0x08,
    Left = 0x10,
    CreatedEntered = Created | Entered,
    UpdatedEntered = Updated | Entered,
    DeletedLeft = Deleted | Left
}


public static class EntityOpExtensions
{
    private static readonly Dictionary<EntityOp, string> _names = new()
    {
        { EntityOp.None,           "None" },
        { EntityOp.Created,        "Created" },
        { EntityOp.Updated,        "Updated" },
        { EntityOp.Deleted,        "Deleted" },
        { EntityOp.Entered,        "Entered" },
        { EntityOp.Left,           "Left" },
        { EntityOp.CreatedEntered, "Created+Entered" },
        { EntityOp.UpdatedEntered, "Updated+Entered" },
        { EntityOp.DeletedLeft,    "Deleted+Left" }
    };

    public static string ToFriendlyString(this EntityOp op)
    {
        return _names.TryGetValue(op, out var name)
            ? name
            : op.ToString(); // fallback to flag string (e.g., "Created, Entered")
    }
}

public class Entity
{
    public int Index { get; set; }
    public int Serial { get; set; }
    public class Class { }
    public bool Active { get; set; }
    public FieldState State { get; set; }
    public Dictionary<string, FieldPath> FpCache { get; set; } = new();
    public HashSet<string> FpNoop { get; set; } = new();
}