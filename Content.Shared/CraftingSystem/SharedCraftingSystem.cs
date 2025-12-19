using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CraftingSystem;

public abstract class SharedCraftingSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _protoMan = default!;



    private static readonly ISawmill _sawmill = Logger.GetSawmill("Crafting System");


    // ID:, Ref. Doing this for easier searching, if there's easier let me know.
    private Dictionary<string, CraftingRecipePrototype> _recipes = new();


    public override void Initialize()
    {
        base.Initialize();
        RebuildRecipes();
    }

    private void RebuildRecipes()
    {
        _recipes.Clear();
        foreach (var proto in _protoMan.EnumeratePrototypes<CraftingRecipePrototype>())
        {
            if (!PassesSanityChecks(proto))
            {
                continue;
            }
            _recipes[proto.ID] = proto;
        }
    }


    private bool PassesSanityChecks(CraftingRecipePrototype proto)
    {
        if (proto.RequiredMachineProtoIDs.Count != 0 && proto.RequiredMachineTags.Count != 0)
        {
            _sawmill.Error(
                $"Crafting recipe '{proto.ID}' defines both RequiredMachineProtoIDs and RequiredMachineTags. Only define one. This is a code quality problem, not an inherent error. Fix it though.");
            return false;
        }

        return true;

    }

    public bool TryGetRecipe(string id, out CraftingRecipePrototype? recipe)
    {
        return _recipes.TryGetValue(id, out recipe);
    }

    public static bool CanCraft(CraftingRecipePrototype recipe)
    {
        return recipe.Craftable;
    }

    public Dictionary<EntProtoId, int> GetNearbyMaterialCounts(EntityUid ent, EntityLookupSystem lookup, SharedHandsSystem handsSystem, float range)
    {
        var materialCounts = new Dictionary<EntProtoId, int>();
        var nearbyEntities =  lookup.GetEntitiesInRange(ent, range);

        foreach (var entity in nearbyEntities)
        {
            AppendMatches(entity, materialCounts);
        }

        foreach (var hand in handsSystem.EnumerateHands(ent))
        {
            if(hand.HeldEntity != null)
                AppendMatches(hand.HeldEntity.Value, materialCounts);
        }

        return materialCounts;
    }

    private void AppendMatches(EntityUid entity, Dictionary<EntProtoId, int> materialCounts)
    {
        if (!TryComp(entity, out MetaDataComponent? metaData))
            return;

        var id = metaData.EntityPrototype?.ID;
        if (id == null)
            return;

        var amount = TryComp(entity, out StackComponent? stackComponent) ? stackComponent.Count : 1;
        materialCounts[id] = materialCounts.GetValueOrDefault(id) + amount;
    }

    public bool CheckCraftingRequirements(CraftingRecipePrototype recipe, Dictionary<EntProtoId, int> materialCounts)
    {
        foreach (var requirement in recipe.Requirements)
        {
            if (!materialCounts.TryGetValue(requirement.Key, out var materialCount) || materialCount <= requirement.Value)
            {
                return false;
            }
        }
        return true;
    }

    public bool DoAuthoritativeChecks(CraftingRecipePrototype recipe,
        SharedHandsSystem handsSystem,
        EntityLookupSystem entLookup,
        EntityUid entity)
    {
        if(!CanCraft(recipe))
            return false;

        // TODO: Turn this into a CVAR please.
        const float range = 1.5f;

        var nearbyCounts = GetNearbyMaterialCounts(entity, entLookup, handsSystem, range);
        var craftingReqsMet = CheckCraftingRequirements(recipe, nearbyCounts);

        if(!craftingReqsMet)
            return false;

        return true;
    }

    public IEnumerable<CraftingRecipePrototype> EnumerateRecipes() => _recipes.Values;

    public List<CraftingRecipePrototype> GetRecipesByCategory(string category)
    {
        var results = new List<CraftingRecipePrototype>();

        foreach (var recipe in _recipes)
        {
            if (string.Equals(recipe.Value.CraftingCategory.Id, category, StringComparison.CurrentCultureIgnoreCase))
            {
                results.Add(recipe.Value);
            }
        }
        return results;
    }

    public bool IsRecipeMadeViaMaterial(CraftingRecipePrototype recipe, List<EntProtoId> materialName)
    {
        if (materialName.Any(material => recipe.Requirements.ContainsKey(material)))
        {
            return true;
        }

        return false;
    }

    public bool GetIsHandheld(CraftingRecipePrototype? recipePrototype, out EntityPrototype? outputItem)
    {
        if (recipePrototype == null)
        {
            outputItem = null;
            return false;
        }
        bool isHandheld;
        if (!_protoMan.TryIndex(recipePrototype.OutputId, out outputItem))
        {
            return false;
        }
        if (!outputItem.Components.TryGetValue("Item", out var itemComp))
            isHandheld = false;
        else
        {
            ItemComponent? comp = itemComp.Component as ItemComponent;
            isHandheld = comp != null;
        }

        return isHandheld;
    }


}


[Serializable, NetSerializable]
public sealed class CraftingRequestReceivedArgs : EntityEventArgs
{
    public string RecipeID { get; }

    public CraftingRequestReceivedArgs(string id)
    {
        RecipeID = id;
    }
}
[Serializable, NetSerializable]
public sealed partial class CraftingCompletedEvent : DoAfterEvent
{
    public string RecipeId { get; }
    public int CrafterUID { get; }

    public CraftingCompletedEvent(string id, int crafter)
    {
        RecipeId = id;
        CrafterUID = crafter;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
