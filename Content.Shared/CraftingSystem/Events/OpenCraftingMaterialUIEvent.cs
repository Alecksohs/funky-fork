using Content.Shared.Construction.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.CraftingSystem.Events;

[Serializable, NetSerializable]
public sealed class OpenCraftingMaterialUIEvent : EntityEventArgs
{
    public CraftingMaterial Material;

    public OpenCraftingMaterialUIEvent(CraftingMaterial material)
    {
        Material = material;
    }
}
