using Content.Shared.Construction.Components;
using Content.Shared.CraftingSystem.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Materials;
using Robust.Shared.Player;

namespace Content.Server.CraftingSystem;
// not to be confused with craftingSystem, this is specifically to handle component interactions of CraftingMaterialComponent.cs


public sealed class CraftingMaterialsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MaterialComponent, UseInHandEvent>(OnUsedInhand);
    }

    private void OnUsedInhand(EntityUid uid, MaterialComponent component, UseInHandEvent args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        RaiseNetworkEvent(new OpenCraftingMaterialUIEvent(component.MaterialId), actor.PlayerSession);
    }
}
