using necronomicon.processor;
using Steam.Protos.Dota2;

namespace necronomicon.model;

public class SvcPacketEntities
{
    private readonly Necronomicon _parser;
    public SvcPacketEntities(Necronomicon parser)
    {
        _parser = parser;

        _parser.Callbacks.OnSvcPacketEntities.Add(OnCSVCMsgPacketEntities);
    }

    public async Task OnCSVCMsgPacketEntities(CSVCMsg_PacketEntities packetEntities)
    {
        byte[] entityDataBuffer = packetEntities.EntityData.ToArray();
        BitReaderWrapper bitReader = new BitReaderWrapper(entityDataBuffer);

        int index = -1;
        int updates = packetEntities.UpdatedEntries;
        uint cmd;
        int classId;
        int serial;
        EntityOp op = new EntityOp();
        if (!packetEntities.LegacyIsDelta)
        {
            if (_parser.EntityFullPackets > 0)
            {
                return;
            }
            _parser.EntityFullPackets++;
        }

        var tuples = new List<(Entity e, EntityOp op)>(updates);
        while (updates > 0)
        {
            updates--;
            index += (int)bitReader.ReadUBitVar() + 1;
            // Debug.WriteLine($"Index: {index}");
            op = EntityOp.None;
            Entity? entityChanged;
            _parser.Entities.TryGetValue(index, out entityChanged);

            cmd = bitReader.Reader.ReadUInt32LSB(2);
            // Debug.WriteLine($"Cmd: {cmd}");
            // if (index == 142)
            // {
            //     Debug.WriteLine("sawp");
            // }
            switch (cmd)
                {
                    case 2: // Create
                        classId = (int)bitReader.Reader.ReadUInt32LSB((int)_parser.ClassIdSize);
                        serial = (int)bitReader.Reader.ReadUInt32LSB(17);
                        bitReader.ReadVarUInt32(); // discard return value

                        if (!_parser.ClassesById.TryGetValue(classId, out var entityClass))
                        {
                            throw new NecronomiconException($"unable to find new class {classId}");
                        }

                        if (!_parser.ClassBaselines.TryGetValue(classId, out var baseline))
                        {
                            throw new NecronomiconException($"unable to find new baseline {classId}");
                        }

                        entityChanged = new Entity(index, serial, entityClass);
                        _parser.Entities[index] = entityChanged;

                        FieldReader baseLineFieldReader = new FieldReader(new BitReaderWrapper(baseline), entityClass.Serializer, entityChanged.State);
                        baseLineFieldReader.ReadFields();

                        FieldReader createFieldReader = new FieldReader(bitReader, entityClass.Serializer, entityChanged.State);
                        createFieldReader.ReadFields();

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

                        FieldReader updateFieldReader = new FieldReader(bitReader, entityChanged.EntityClass.Serializer, entityChanged.State);
                        updateFieldReader.ReadFields();
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
                        _parser.Entities.Remove(index);
                        break;
                }

            if (entityChanged != null)
            {
                tuples.Add((entityChanged, op));
            }
        }

        // foreach (var handler in entityHandlers)
        // {
        //     foreach (var (entity, operation) in tuples)
        //     {
        //         var err = handler(entity, operation);
        //         if (err != null)
        //         {
        //             return err;
        //         }
        //     }
        // }

        await Task.CompletedTask;
    }
}