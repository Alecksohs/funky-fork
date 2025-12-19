using System.Linq;
using Content.Shared.CraftingSystem;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.CraftingSystem;

public sealed class CraftingSystem : SharedCraftingSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency]private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CraftingRequestReceivedArgs>(TryCraft);
        SubscribeLocalEvent<CraftingCompletedEvent>(OnCraftingCompleted);
    }

    private void OnCraftingCompleted(CraftingCompletedEvent ev)
    {
        var craftingPlayer = ev.User;
        // TODO: spawn item
    }


    private void TryCraft(CraftingRequestReceivedArgs request, EntitySessionEventArgs args)
    {
        var senderSession = args.SenderSession;
        if (!TryGetRecipe(request.RecipeID, out var recipePrototype))
            return;

        if (senderSession.AttachedEntity == null || recipePrototype == null)
        {
            return;
        }

        if(!DoAuthoritativeChecks(recipePrototype, _hands, _lookup, senderSession.AttachedEntity.Value))
            return;
        // TODO: We now know we can craft, let's craft. This is on the server, so we should actually do it.
        ConsumeRecipeRequirements(senderSession.AttachedEntity.Value, recipePrototype);
        // spawn item, is worldspace object? use single placement ghost unless shift held.
        // TODO: figure out how to differentiate item as handheld or not.


        // TODO: Doafter

        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager, senderSession.AttachedEntity.Value, recipePrototype.DoTime, new CraftingCompletedEvent(recipePrototype.ID,  senderSession.AttachedEntity.Value.Id),  senderSession.AttachedEntity.Value, null, null )
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
                BreakOnDropItem = false,
            });
    }


    public void ConsumeRecipeRequirements(EntityUid uid, CraftingRecipePrototype recipe)
    {
        var recipeRequirementsToFulfill = new Dictionary<EntProtoId, int>(recipe.Requirements);


        foreach (var requirement in recipeRequirementsToFulfill)
        {
            var protoWeAreConsuming = requirement.Key;

            // How many items do we need? We always know we have enough once we're here.
            var amountLeft = recipeRequirementsToFulfill[protoWeAreConsuming];

            // Let's pull from hands first.

            foreach (var hand in _hands.EnumerateHands(uid))
            {
                if (hand.HeldEntity == null)
                    continue;

                if (!TryComp(hand.HeldEntity.Value, out MetaDataComponent? metaData))
                    continue;

                var id = metaData.EntityPrototype?.ID;
                if (id == null)
                    continue;

                ProcessRecipeRequirementConsumption(hand.HeldEntity,
                    recipeRequirementsToFulfill,
                    protoWeAreConsuming,
                    ref amountLeft);
                // We assume removal didn't fail. Possible exploit.
            }

            if (amountLeft <= 0)
            {
                recipeRequirementsToFulfill[protoWeAreConsuming] = 0;
                continue;
            }

            // Now we must check the objects in range. Probably unoptimized. It's WIP code.
            var nearbyEnts = _lookup.GetEntitiesInRange(uid, 1.5f);

            foreach (var entity in nearbyEnts)
            {
                if (!TryComp(entity, out MetaDataComponent? metaData))
                    continue;

                var id = metaData.EntityPrototype?.ID;
                if (id == null || id != protoWeAreConsuming.Id)
                    continue;
                ProcessRecipeRequirementConsumption(entity,
                    recipeRequirementsToFulfill,
                    protoWeAreConsuming,
                    ref amountLeft);
            }
        }
    }

    private void ProcessRecipeRequirementConsumption(EntityUid? entityToCheck,
        Dictionary<EntProtoId, int> recipeRequirementsToFulfill,
        EntProtoId protoWeAreConsuming,
        ref int amountLeft)
    {
        if (entityToCheck == null)
            return;

        var isInStack = TryComp(entityToCheck.Value, out StackComponent? stackComponent);
        var consumedAmount = 0;
        if (isInStack && stackComponent != null)
        {
            var stackAmount = stackComponent.Count;
            var stackAmountConsumable = Math.Min(stackAmount, amountLeft);
            consumedAmount = stackAmountConsumable;
            _stack.SetCount(entityToCheck.Value, stackAmount - stackAmountConsumable);
        }
        else
        {
            // Not in a stack. We have one.
            consumedAmount = 1;
            _entityManager.DeleteEntity(entityToCheck.Value);
        }

        // Fufill Requirements.
        amountLeft -= consumedAmount;
        if (amountLeft <= 0)
        {
            recipeRequirementsToFulfill[protoWeAreConsuming] = 0;
            return;
        }

        recipeRequirementsToFulfill[protoWeAreConsuming] = amountLeft;
    }
}
