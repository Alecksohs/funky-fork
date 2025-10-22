using Robust.Shared.Serialization;

namespace Content.Shared.Construction.Components;

[RegisterComponent, ComponentProtoName("CraftingMaterial")]

public sealed partial class CraftingMaterialComponent : Component, ISerializationHooks
{
    [DataField]
    public CraftingMaterial CraftingMat;

    public void OnMaterialUse(EntityUid uid, CraftingMaterialComponent component)
    {

    }


}


public enum CraftingMaterial
{
    Iron,
    Gold,
    Quartz,
    Uranium,
    Bananium,
    Silver,
    Plasma,
    Diamond,
}
