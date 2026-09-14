namespace EntityWork.Model.Types;

public enum Source2EntityInclusionFlags : byte
{
    // forThis = (fromParent & ~(thisFlags >> 2)) | (thisFlags & 3)
    // forChild = forThis & ~(thisFlags >> 4)

    None = 0,

    IncludeField = 1 << 0,
    IncludeSet = 1 << 1,
    Include = IncludeField | IncludeSet,

    NoInheritField = 1 << 2,
    NoInheritSet = 1 << 3,
    NoInherit = NoInheritField | NoInheritSet,

    NoPropagateField = 1 << 4,
    NoPropagateSet = 1 << 5,
    NoPropagate = NoPropagateField | NoPropagateSet,
}
