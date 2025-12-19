using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.CraftingSystem;


[Serializable, NetSerializable]
public sealed class CraftingGhostFlipEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly bool UseMirrorPrototype;
    public CraftingGhostFlipEvent(NetEntity netEntity, bool useMirrorPrototype)
    {
        NetEntity = netEntity;
        UseMirrorPrototype = useMirrorPrototype;
    }
}
[Serializable, NetSerializable]
public sealed class CraftingGhostRotationEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly Direction Direction;

    public CraftingGhostRotationEvent(NetEntity netEntity, Direction direction)
    {
        NetEntity = netEntity;
        Direction = direction;
    }
}

[Serializable, NetSerializable]
public sealed class CraftingRequestReceivedArgs : EntityEventArgs
{
    public string RecipeID { get; }

    public CraftingRequestReceivedArgs(string id)
    {
        RecipeID = id;
    }
}
[Serializable, NetSerializable]
public sealed partial class CraftingCompletedEvent : DoAfterEvent
{
    public string RecipeId { get; }
    public int CrafterUID { get; }

    public CraftingCompletedEvent(string id, int crafter)
    {
        RecipeId = id;
        CrafterUID = crafter;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
