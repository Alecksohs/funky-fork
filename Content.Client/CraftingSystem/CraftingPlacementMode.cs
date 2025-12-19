using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.CraftingSystem;

public sealed class CraftingPlacementMode : PlacementMode
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _transformSystem;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private const float SearchBoxSize = 2f;
    private const float PlaceColorBaseAlpha = 0.5f;

    private EntityCoordinates _unalignedMouseCoords = default;

    /// <summary>
    /// This placement mode is majorly copying from the RCD Construction mode.
    /// </summary>
    public CraftingPlacementMode(PlacementManager pMan) : base(pMan)
    {
        IoCManager.InjectDependencies(this);
        _mapSystem = _entityManager.System<SharedMapSystem>();
        _transformSystem = _entityManager.System<SharedTransformSystem>();

        ValidPlaceColor = ValidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
    }

    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        _unalignedMouseCoords = ScreenToCursorGrid(mouseScreen);
        MouseCoords = _unalignedMouseCoords.AlignWithClosestGridTile(SearchBoxSize, _entityManager, _mapManager);

        var gridId = _transformSystem.GetGrid(MouseCoords);

        if (!_entityManager.TryGetComponent<MapGridComponent>(gridId, out var mapGrid))
            return;

        CurrentTile = _mapSystem.GetTileRef(gridId.Value, mapGrid, MouseCoords);

        float tileSize = mapGrid.TileSize;
        GridDistancing = tileSize;

        if (pManager.CurrentPermission!.IsTile)
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2,
                CurrentTile.Y + tileSize / 2));
        }
        else
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
        }
    }

    public override bool IsValidPosition(EntityCoordinates position)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;

        // If the destination is out of interaction range, set the placer alpha to zero
        if (!_entityManager.TryGetComponent<TransformComponent>(player, out var xform))
            return false;

        if (!_transformSystem.InRange(xform.Coordinates, position, SharedInteractionSystem.InteractionRange))
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(0);
            return false;
        }

        // Determine if the user is hovering over a target
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen)
            return false;

        var target = screen.GetClickedEntity(_transformSystem.ToMapCoordinates(_unalignedMouseCoords));

        // Determine if the RCD operation is valid or not
        // if (!_rcdSystem.IsRCDOperationStillValid(heldEntity.Value, rcd, mapGridData.Value, target, player.Value, false))
        //     return false;

        return true;
    }
}
