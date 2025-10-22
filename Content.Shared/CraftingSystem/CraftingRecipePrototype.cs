using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.CraftingSystem;

[Prototype("craftingRecipe")]
public sealed class CraftingRecipePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = "Some Type of Item";
    [DataField] public string Description { get; private set; } = "Something.";
    [DataField] public Dictionary<EntProtoId, int> Requirements { get; private set; } =  new Dictionary<EntProtoId, int>();
    [DataField] public List<EntProtoId> RequiredTools { get; private set; } = new List<EntProtoId>();
    // Time to craft the item.
    [DataField] public float DoTime { get; private set; } = 3.0f;
    // If false, hide from the UI. I don't know what you're coding but if you needed this, here. Cook, king.
    [DataField] public bool Craftable { get; private set; } = true;
    // If true, show in the UI sorted first.
    [DataField] public bool IsCommonRecipe  { get; private set; } = false;
    // UI Categorization, make new ones if you must, don't overdo it.
    [DataField] public CraftingCategory CraftingCategories { get; private set; } = CraftingCategory.Miscellaneous;

    // LESS USED

    // Structures needed to craft our item. Only fill out one. If both aren't empty, you'll crash. It's intentional.
    [DataField] public List<EntProtoId> RequiredMachineProtoIDs { get; private set; } = new List<EntProtoId>();
    [DataField] public List<TagPrototype> RequiredMachineTags { get; private set; } = new List<TagPrototype>();

    // Allows us to craft more than one at once if true.
    [DataField] public bool MassCraftable { get; private set; } = false;
    //After we craft, how many items per stack do we spawn? If item isn't stackable, then we don't care.
    [DataField] public int ResultStackAmount { get; private set; } = 0;

    // TODO: Implement Reagant Holder detection and requirements. Not required for 1.0, but will be useful.
    // TODO:


}
