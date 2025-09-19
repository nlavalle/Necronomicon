
using System.Diagnostics;
using necronomicon;
using necronomicon.model;
using necronomicon.model.engine;
using necronomicon.processor;
using Steam.Protos.Dota2;

namespace Benchmarks.Necronomicon.ReplayAnalysis;

public delegate Task OnEntityUpdate((Entity, EntityOp)[] updates);

public class ReplayPacketEntities
{
    public Entity?[] Entities = new Entity?[4096]; // https://developer.valvesoftware.com/wiki/Entity_limit
    public List<OnEntityUpdate> Callbacks { get; } = new();
    private Dictionary<int, Class> ClassesById;
    private (Entity, EntityOp)[] entityUpdates;
    private int EntityFullPackets;
    private int ClassIdSize;
    private ReplayStringTables ReplayStringTables;
    public ReplayPacketEntities(
        Dictionary<int, Class> classesById,
        int classIdSize,
        ReplayStringTables replayStringTables
    )
    {
        ClassesById = classesById;
        ClassIdSize = classIdSize;
        ReplayStringTables = replayStringTables;
    }

    public void OnCSVCMsgPacketEntities(
        CSVCMsg_PacketEntities packetEntities
    )
    {
        var entityDataBuffer = packetEntities.EntityData.Span;
        var bitReader = new FastBitReader(entityDataBuffer);

        int index = -1;
        int updates = packetEntities.UpdatedEntries;
        uint cmd;
        int classId;
        int serial;
        EntityOp op = new EntityOp();
        entityUpdates = new (Entity, EntityOp)[updates];
        if (!packetEntities.LegacyIsDelta)
        {
            if (EntityFullPackets > 0)
            {
                return;
            }
            EntityFullPackets++;
        }

        while (updates > 0)
        {
            updates--;
            index += (int)bitReader.ReadUBitVar() + 1;
            // Debug.WriteLine($"Index: {index}");
            op = EntityOp.None;
            Entity? entityChanged = Entities[index];

            cmd = bitReader.Reader.ReadUInt32LSB(2);
            // Debug.WriteLine($"Cmd: {cmd}");

            switch (cmd)
            {
                case 2: // Create
                    classId = (int)bitReader.Reader.ReadUInt32LSB(ClassIdSize);
                    serial = (int)bitReader.Reader.ReadUInt32LSB(17);
                    bitReader.ReadVarUInt32(); // discard return value

                    if (!ClassesById.TryGetValue(classId, out var entityClass))
                    {
                        throw new NecronomiconException($"unable to find new class {classId}");
                    }

                    if (!ReplayStringTables.ClassBaselines.TryGetValue(classId, out var baseline))
                    {
                        throw new NecronomiconException($"unable to find new baseline {classId}");
                    }

                    entityChanged = new Entity(index, serial, entityClass);
                    Entities[index] = entityChanged;

                    FieldReader fieldReader = new FieldReader(entityClass.Serializer, entityChanged.State);
                    var baselineReader = new FastBitReader(baseline);

                    fieldReader.ReadFields2(ref baselineReader);

                    fieldReader.ReadFields2(ref bitReader);

                    op = EntityOp.Created | EntityOp.Entered;
                    break;
                case 0: // Update
                    if (entityChanged == null)
                    {
                        throw new NecronomiconException($"unable to find existing entity {index}");
                    }

                    op = EntityOp.Updated;
                    if (!entityChanged.Active)
                    {
                        entityChanged.Active = true;
                        op |= EntityOp.Entered;
                    }

                    FieldReader updateFieldReader = new FieldReader(entityChanged.EntityClass.Serializer, entityChanged.State);
                    updateFieldReader.ReadFields2(ref bitReader);
                    break;
                case 1: // Leave
                    if (entityChanged == null)
                    {
                        throw new NecronomiconException($"unable to find existing entity {index}");
                    }

                    if (!entityChanged.Active)
                    {
                        throw new NecronomiconException($"entity {entityChanged.EntityClass.ClassId} ({entityChanged.EntityClass.Name}) ordered to leave, already inactive");
                    }

                    op = EntityOp.Left;
                    break;
                case 3: // Delete
                    op = EntityOp.Left | EntityOp.Deleted;
                    // Entities.Remove(index);
                    Entities[index] = null;
                    break;
            }

            if (entityChanged != null)
            {
                entityUpdates[updates] = (entityChanged, op);
            }
        }

        Debug.Assert(entityUpdates.Length == packetEntities.UpdatedEntries, "Got a different amount of entites vs what packet indicated");
        foreach (var handler in Callbacks)
        {
            handler(entityUpdates);
        }
    }
}