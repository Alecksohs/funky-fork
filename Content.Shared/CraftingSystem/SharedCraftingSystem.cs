using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

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
            return true;

        _sawmill.Error(
            $"Crafting recipe '{proto.ID}' defines both RequiredMachineProtoIDs and RequiredMachineTags. Only define one. This is a code quality problem, not an inherent error. Fix it though.");
        return false;

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
        // TODO: Turn this into a CVAR please.
        const float range = 1.5f;

        var nearbyCounts = GetNearbyMaterialCounts(entity, entLookup, handsSystem, range);
        var craftingReqsMet = CheckCraftingRequirements(recipe, nearbyCounts);

        return CanCraft(recipe) && craftingReqsMet;
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

}
