using Content.Shared.Construction.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.CraftingSystem.Events;

[Serializable, NetSerializable]
public sealed class OpenCraftingMaterialUIEvent : EntityEventArgs
{
    public string? Material;

    public OpenCraftingMaterialUIEvent(string? material)
    {
        Material = material;
    }
}
