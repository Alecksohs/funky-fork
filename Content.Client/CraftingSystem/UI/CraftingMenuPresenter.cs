using System.Linq;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.CCVar;
using Content.Shared.CraftingSystem;
using Content.Shared.Materials;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Client.CraftingSystem.UI;

// Construction Menu Presenter is my base. I'll make the same mistakes.
public sealed class CraftingMenuPresenter : IDisposable
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;


    private readonly EntityWhitelistSystem _whitelistSystem;
    private readonly SpriteSystem _spriteSystem;
    private readonly CraftingMenu _craftingMenu;

    private CraftingSystem? _craftingSystem;
    private CraftingRecipePrototype? _selectedRecipe;
    private List<CraftingRecipePrototype> _favoriteRecipes = [];

    private string _selectedCategory = string.Empty;
    private string _favoriteCategoryName = "crafting-category-favorites";
    private string _allCategoryName = "crafting-category-all";

    public CraftingMenuPresenter()
    {
        IoCManager.InjectDependencies(this);
        _craftingMenu = new CraftingMenu(_prototypeManager);
        _craftingMenu.Presenter = this;
        _whitelistSystem = _entManager.System<EntityWhitelistSystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        // _craftingMenu.PopulateRecipes += OnPopulateRecipes;

        if (_systemManager.TryGetEntitySystem<CraftingSystem>(out var craftingSystem))
            SystemBindingChanged(craftingSystem);
    }

    public CraftingSystem? GetCraftingSystem => _craftingSystem;


    private void OnPopulateRecipes(object? sender, (string search, string category, string? materialFilter) e)
    {
        var (search, category, materialFilter) = e;

        var recipes = new List<CraftingRecipePrototype>();

        var isEmptyCategory = category == string.Empty || category == _allCategoryName;

        _selectedCategory = isEmptyCategory ? string.Empty : category;

        foreach (var recipe in _prototypeManager.EnumeratePrototypes<CraftingRecipePrototype>())
        {
            if (recipe.Craftable == false)
                continue;

            if (_playerManager.LocalSession == null || _playerManager.LocalEntity == null)
                continue;

            if (!string.IsNullOrEmpty(search))
            {
                if (!recipe.Name.ToLower().Contains(search.Trim().ToLower()))
                    continue;
            }

            if (!isEmptyCategory)
            {
                if (category == _favoriteCategoryName)
                {
                    if (_favoriteRecipes.Contains(recipe))
                        continue;
                }
                else if (recipe.CraftingCategory != category)
                    continue;
            }

            if (!string.IsNullOrEmpty(materialFilter))
            {
                // Probably swap to tags eventually.
                if (recipe.Requirements.Count != 1)
                    continue;
                var onlyRequirement = recipe.Requirements.Keys.First();
                // Is the only requirement filterable, like does it have a materialcomponent with an ID?
                if (!_prototypeManager.Resolve(onlyRequirement, out var prototype))
                {
                    continue;
                }

                if (prototype.Components.TryGetValue("Material", out var material) &&
                    material.Component is MaterialComponent materialComponent)
                {
                    if (materialComponent.MaterialId != materialFilter)
                        continue;
                }
            }

            recipes.Add(recipe);
            // I am not an algorithms guy. I like doing little artistcode, please reread and redo this since I know it's bad.
            // It's stitched together from varius random online sources and asking a friend lmao
            // Probably unoptimized BUT it is ran on the client for a reason.
            // TLDR, TODO: rewrite this.
        }

        // We've added all recipes, now for the rest. Sorting now.
        if (string.IsNullOrEmpty(search))
        {
            recipes.Sort((a, b) =>
            {
                int commonFirst = b.IsCommonRecipe.CompareTo(a.IsCommonRecipe);
                if (commonFirst != 0)
                    return commonFirst;

                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }
        else
        {
            recipes.Sort((a, b) =>
            {
                var scoreA = LevenshteinDistance(a.Name, search);
                var scoreB = LevenshteinDistance(b.Name, search);

                var cmp = scoreB.CompareTo(scoreA);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        // It's been sorted, now spawn entries.
        _craftingMenu.SpawnEntries(recipes);
    }

    // In other words, string similarity.
    private static float LevenshteinDistance(string source, string target)
    {
        source = source.ToLower();
        target = target.ToLower();

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0f;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var distance = new int[sourceLength + 1, targetLength + 1];

        for (int i = 0; i <= sourceLength; i++)
        {
            distance[i, 0] = i;
        }

        for (int i = 0; i <= targetLength; i++)
        {
            distance[0, i] = i;
        }

        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1,
                        distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        var eDist = distance[sourceLength, targetLength];
        var maxDist = Math.Max(sourceLength, targetLength);
        var similarity = 1.0f - (float) eDist / maxDist;
        return MathF.Max(0.0f, MathF.Min(1.0f, similarity));
    }


    /// <summary>
    /// Does the window have focus? If the window is closed, this will always return false.
    /// </summary>
    private bool IsAtFront => _craftingMenu.IsOpen && _craftingMenu.IsAtFront();

    private bool WindowOpen
    {
        get => _craftingMenu.IsOpen;
        set
        {
            if (value && CraftingAvailable)
            {
                if (_craftingMenu.IsOpen)
                    _craftingMenu.MoveToFront();
                else
                    _craftingMenu.OpenCentered();

                // if (_selected != null)
                //     PopulateInfo(_selected);
            }
            else
                _craftingMenu.Close();
        }
    }

    private bool CraftingAvailable
    {
        get => _uiManager.GetActiveUIWidget<GameTopMenuBar>().CraftingButton.Visible;
        set
        {
            _uiManager.GetActiveUIWidget<GameTopMenuBar>().CraftingButton.Visible = value;
            if (!value)
                _craftingMenu.Close();
        }
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
        if (_uiManager.GetActiveUIWidgetOrNull<GameTopMenuBar>() != null)
        {
            CraftingAvailable = system.CraftingEnabled;
        }
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

    public void OpenUIFilteredByMaterial(string? material)
    {
        WindowOpen = true;
        OnPopulateRecipes(_playerManager.LocalSession, (string.Empty, string.Empty, material));
    }

    public void Dispose()
    {
        _craftingMenu.Dispose();
    }
}

public sealed class RefreshCraftingMenuCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

    public string Command => "ploopy";
    public string Description => "mr krabs i plimopted..";
    public string Help => $"Usage: {Command} / {Command} <preset>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();

            if (!sysMan.TryGetEntitySystem<Content.Client.CraftingSystem.CraftingSystem>(out var craftingSys))
            {
                shell.WriteLine("Client crafting system not found. Are you running this on the client?");
                return;
            }

            // Prefer a public API on the system.
            craftingSys.RefreshUI();
            shell.WriteLine("Crafting UI refreshed.");
        }
        catch (Exception e)
        {
            shell.WriteLine($"Failed to refresh crafting UI: {e.GetType().Name}: {e.Message}");
        }
    }
}
