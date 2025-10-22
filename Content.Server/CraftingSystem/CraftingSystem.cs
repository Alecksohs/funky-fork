using Content.Shared.CraftingSystem;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.CraftingSystem;

public sealed class CraftingSystem : SharedCraftingSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
}
