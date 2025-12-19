using Content.Client.CraftingSystem.UI;
using Content.Shared.Construction.Components;
using Content.Shared.CraftingSystem;
using Content.Shared.CraftingSystem.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Client.CraftingSystem;

/// <summary>
/// The UI handler of the crafting system
/// </summary>
public sealed class CraftingSystem : SharedCraftingSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;


    private CraftingMenuPresenter? _presenter;

    public CraftingMenuPresenter? GetPresenter => _presenter;

    public bool CraftingEnabled { get; private set; }


    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenCraftingMaterialUIEvent>(OpenUIFiltered);

        _presenter = new CraftingMenuPresenter();
    }

    public void CraftingRequestHandler(CraftingMenuEntry entry)
    {
        if (entry.Recipe?.ID != null)
        {
            var isHandheld = GetIsHandheld(entry.Recipe, out var outputItem);

            // Decide if we should just craft or refer to ghost which initiates craft.
            if (isHandheld)
            {
                RaiseNetworkEvent(new CraftingRequestReceivedArgs(entry.Recipe.ID));
            }
            else
            {
                // Todo: construction ghosts time
            }

        }
    }



    public override void Shutdown()
    {
        base.Shutdown();

        _presenter?.Dispose();
        _presenter = null;
    }

    public void RefreshUI()
    {
        _presenter?.Dispose();
        _presenter = new CraftingMenuPresenter();
        CraftingEnabled = true;
    }

    private void OpenUIFiltered(OpenCraftingMaterialUIEvent ev)
    {
        _presenter?.OpenUIFilteredByMaterial(ev.Material);
    }


}




