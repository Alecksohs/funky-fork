using Content.Client.CraftingSystem.UI;
using Content.Client.RCD;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.CraftingSystem;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Client.CraftingSystem;

public sealed class CraftingConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!; // Added for prototype access
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    private CraftingSystem? _craftingSystem;


    private string _placementMode = nameof(CraftingPlacementMode);

    private Direction _placementDirection = default;

    // Apparently we need to handle flips here.
    public event EventHandler? FlipCraftingPrototype;
    private bool _useMirrorPrototype = false;

    public override void Initialize()
    {
        base.Initialize();

        // This is required so that if we load after the system is initialized, we can bind to it immediately
        if (_systemManager.TryGetEntitySystem<CraftingSystem>(out var constructionSystem))
            SystemBindingChanged(constructionSystem);


        // bind key
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorFlipObject,
                new PointerInputCmdHandler(HandleFlip, outsidePrediction: true))
            .Register<CraftingConstructionGhostSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<CraftingConstructionGhostSystem>();
        base.Shutdown();
    }

    private void SystemBindingChanged(CraftingSystem? newSystem)
    {
        if (newSystem is null)
        {
            if (_craftingSystem is null)
                return;

            UnbindFromSystem();
        }
        else
        {
            if (_craftingSystem is null)
            {
                BindToSystem(newSystem);
                return;
            }

            UnbindFromSystem();
            BindToSystem(newSystem);
        }
    }

    private void BindToSystem(CraftingSystem system)
    {
        _craftingSystem = system;
        // system.ToggleCraftingWindow += SystemOnToggleMenu;
        // system.FlipConstructionPrototype += SystemFlipConstructionPrototype;
        // system.CraftingAvailabilityChanged += SystemCraftingAvailabilityChanged;
        // system.ConstructionGuideAvailable += SystemGuideAvailable;
    }


    private void UnbindFromSystem()
    {
        var system = _craftingSystem;

        if (system is null)
            throw new InvalidOperationException();
        // system.ToggleCraftingWindow -= SystemOnToggleMenu;
        // system.FlipConstructionPrototype -= SystemFlipConstructionPrototype;
        // system.CraftingAvailabilityChanged -= SystemCraftingAvailabilityChanged;
        // system.ConstructionGuideAvailable -= SystemGuideAvailable;
        _craftingSystem = null;
    }

    private bool HandleFlip(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State == BoundKeyState.Down)
        {
            if (!_placementManager.IsActive || _placementManager.Eraser)
                return false;

            var placerEntity = _placementManager.CurrentPermission?.MobUid;

            if (placerEntity == null)
            {
                return false;
            }

            // if(!TryComp<RCDComponent>(placerEntity, out var rcd) ||
            //    string.IsNullOrEmpty(rcd.CachedPrototype.MirrorPrototype))
            //     return false;

            // _useMirrorPrototype = !rcd.UseMirrorPrototype;
            //
            // var useProto = _useMirrorPrototype ? rcd.CachedPrototype.MirrorPrototype : rcd.CachedPrototype.Prototype;
            // CreatePlacer(placerEntity.Value, rcd, useProto);

            // tell the server

            RaiseNetworkEvent(new CraftingGhostFlipEvent(GetNetEntity(placerEntity.Value), _useMirrorPrototype));
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;

        if (_craftingSystem is not { CraftingEnabled: true })
        {
            return;
        }

        if (!_craftingSystem.isConstructing)
            return;

        var presenter = _craftingSystem.GetPresenter ?? throw new InvalidOperationException();


        // Exit if erasing or the current placer is not an RCD (build mode is active)
        if (_placementManager.Eraser || (placerEntity == null))
            return;

        // Update the direction the RCD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new CraftingGhostRotationEvent(GetNetEntity(placerEntity.Value), _placementDirection));
        }

        // If the placer has not changed build it.
        // _rcdSystem.UpdateCachedPrototype(heldEntity.Value, rcd);
        // var useProto = (_useMirrorPrototype && !string.IsNullOrEmpty(rcd.CachedPrototype.MirrorPrototype)) ? rcd.CachedPrototype.MirrorPrototype : rcd.CachedPrototype.Prototype;

        if (presenter.GetCraftingRecipe == null)
            return;

        presenter.UpdateCachedPrototype(presenter.GetCraftingRecipe);
        var useProto = presenter._cachedRecipe?.OutputId;

        if (useProto != placerProto)
        {
            _placementManager.Clear();
            CreatePlacer(placerEntity.Value, useProto);
        }
    }

    private void CreatePlacer(EntityUid uid, string? prototype)
    {
        // Create a new placer
        var newObjInfo = new PlacementInformation
        {
            MobUid = uid,
            PlacementOption = _placementMode,
            EntityType = prototype,
            Range = (int) Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = false,
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
