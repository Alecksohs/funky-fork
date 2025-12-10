using Content.Client.CraftingSystem.UI;
using Content.Shared.Construction.Components;
using Content.Shared.CraftingSystem.Events;
using Content.Shared.Hands;
using Robust.Client.UserInterface;

namespace Content.Client.CraftingSystem;
/// <summary>
/// The UI handler of the crafting system
/// </summary>
public sealed class CraftingSystem : EntitySystem
{

    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    private CraftingMenuPresenter? _presenter;

    public CraftingMenuPresenter? GetPresenter => _presenter;

    public bool CraftingEnabled { get; private set; }

    public override void Initialize()
    {
        SubscribeNetworkEvent<OpenCraftingMaterialUIEvent>(OpenUIFiltered);

        _presenter = new CraftingMenuPresenter();
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
