using Content.Shared.Construction.Components;
using Content.Shared.CraftingSystem.Events;
using Content.Shared.Interaction.Events;
using Robust.Shared.Player;

namespace Content.Server.CraftingSystem;
// not to be confused with craftingSystem, this is specifically to handle component interactions of CraftingMaterialComponent.cs


public sealed class CraftingMaterialsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CraftingMaterialComponent, UseInHandEvent>(OnUsedInhand);
    }

    private void OnUsedInhand(EntityUid uid, CraftingMaterialComponent component, UseInHandEvent args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        RaiseNetworkEvent(new OpenCraftingMaterialUIEvent(component.CraftingMat), actor.PlayerSession);
    }
}
