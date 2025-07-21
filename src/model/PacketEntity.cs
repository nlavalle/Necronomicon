using System.Diagnostics;
using BitsKit.IO;
using necronomicon.processor;
using Steam.Protos.Dota2;

namespace necronomicon.model;

// Corresponds to CSVCMsg_PacketEntities
public class PacketEntity
{
    private readonly CSVCMsg_PacketEntities _packetEntities;
    public PacketEntity(CSVCMsg_PacketEntities packetEntities)
    {
        _packetEntities = packetEntities;
    }

    public void Parse()
    {
        byte[] entityDataBuffer = _packetEntities.EntityData.ToArray();
        BitReaderWrapper bitReader = new BitReaderWrapper(entityDataBuffer);

        int index = -1;
        int updates = _packetEntities.UpdatedEntries;
        uint cmd;
        int classId;
        int serial;
        Entity e = new Entity();
        EntityOp op = new EntityOp();
        if (!_packetEntities.LegacyIsDelta)
        {
            // Do I need this?
            Debug.Assert(1 == 0);
        }

        var tuples = new List<(Entity e, EntityOp op)>(updates);
        while (updates > 0)
        {
            updates--;
            index += (int)bitReader.ReadUBitVar() + 1;
            op = EntityOp.None;

            cmd = bitReader.Reader.ReadUInt32LSB(2);
            if ((cmd & 0x01) == 0)
            {
                if ((cmd & 0x02) != 0)
                {
                    classId = (int)bitReader.Reader.ReadUInt32LSB(32);
                    serial = bitReader.Reader.ReadInt32LSB(17);
                    bitReader.ReadVarUInt32(); // discard return value

                    // if (!classesById.TryGetValue(classId, out var @class))
                    // {
                    //     throw new NecronomiconException($"unable to find new class {classId}");
                    // }

                    // if (!classBaselines.TryGetValue(classId, out var baseline))
                    // {
                    //     throw new NecronomiconException($"unable to find new baseline {classId}");
                    // }

                    e.Index = index;
                    e.Serial = serial;
                    // e = NewEntity(index, serial, @class);
                    // entities[index] = e;

                    // readFields(new ReaderWrapper(new BitReader(baseline)), @class.Serializer, e.State);
                    // readFields(r, @class.Serializer, e.State);

                    op = EntityOp.Created | EntityOp.Entered;
                }
                else
                {
                    // if (!entities.TryGetValue(index, out e))
                    // {
                    //     Panic($"unable to find existing entity {index}");
                    //     return null;
                    // }

                    op = EntityOp.Updated;
                    if (!e.Active)
                    {
                        e.Active = true;
                        op |= EntityOp.Entered;
                    }

                    // readFields(r, e.Class.Serializer, e.State);
                }
            }
            else
            {
                // if (!entities.TryGetValue(index, out e))
                // {
                //     Panic($"unable to find existing entity {index}");
                //     return null;
                // }

                // if (!e.Active)
                // {
                //     Panic($"entity {e.Class.ClassId} ({e.Class.Name}) ordered to leave, already inactive");
                //     return null;
                // }

                op = EntityOp.Left;

                if ((cmd & 0x02) != 0)
                {
                    op |= EntityOp.Deleted;
                    // entities[index] = null;
                }
            }

            tuples.Add((e, op));
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
    }
}