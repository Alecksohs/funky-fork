using Content.Shared.Construction.Components;
using Content.Shared.Hands;
using Robust.Client.UserInterface;

namespace Content.Client.Construction;
/// <summary>
/// The UI handler of the crafting material system
/// </summary>
public sealed class CraftingMaterialSystem : EntitySystem
{

    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CraftingMaterialComponent, RequestActivateInHandEvent>(OnActivateInHand);
    }

    private void OnActivateInHand(Entity<CraftingMaterialComponent> ent, ref RequestActivateInHandEvent args)
    {

    }
}
