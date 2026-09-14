using BitWork;

namespace EntityWork.Model.Types;

public delegate int EntityFieldDecoder(ref AlignedLsBitReader reader, in EntityWriter writer);
