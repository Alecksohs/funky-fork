using Robust.Shared.Prototypes;

namespace Content.Shared.CraftingSystem;

[Prototype("craftingCategory")]
public sealed class CraftingCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
