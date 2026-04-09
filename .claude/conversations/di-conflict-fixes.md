# Conversation: DI Conflict Fixes and Scene Scope Architecture

**Date:** 2026-04-09

---

**Q:** Search for any conflicting registry (e.g. PanelView) in the DI folder and fix them. Should the parent be of both the Gameplay and MainMenu be the Root? Or is there another structure?

**A:** Read all three LifetimeScope files, PanelRegistry, PanelView, PanelAnimator, and all injectable services (GameService, InputService, RoundManager, GameStateMachine, SequenceService, HudController, GameOverController, ScoreService, BootstrapEntry) to build a complete picture of the dependency graph.

Found two real issues:

1. **Duplicate `IScoreService` registration** in `GameplayLifetimeScope.cs:30`:
   ```csharp
   // Before (wrong — IScoreService registered twice):
   builder.Register<ScoreService>(Lifetime.Singleton).As<IScoreService>().AsImplementedInterfaces();
   // After:
   builder.Register<ScoreService>(Lifetime.Singleton).AsImplementedInterfaces();
   ```
   `AsImplementedInterfaces()` already covers `IScoreService` and `IInitializable`; the explicit `.As<IScoreService>()` caused a duplicate.

2. **`BootstrapEntry` calls `LoadMainMenu()` which tries to unload "Gameplay"** — at startup no Gameplay scene exists, causing `UnloadSceneAsync` to throw. Fixed `SceneLoaderService` to guard the unload with `GetSceneByName().IsValid()`:
   ```csharp
   await SceneManager.LoadSceneAsync(load, LoadSceneMode.Additive);
   var sceneToUnload = SceneManager.GetSceneByName(unload);
   if (sceneToUnload.IsValid())
       await SceneManager.UnloadSceneAsync(sceneToUnload);
   ```

**On scope hierarchy:** Confirmed the correct structure is:
```
RootLifetimeScope (DontDestroyOnLoad, entire game lifetime)
├── MainMenuLifetimeScope  (sibling child, while MainMenu scene is active)
└── GameplayLifetimeScope  (sibling child, while Gameplay scene is active)
```
Both children are siblings under Root — neither should be parent of the other. VContainerSettings pointing to `RootLifetimeScopePrefab` wires this automatically. Both need `IEventBus`, `ISceneLoaderService`, `GameConfig`, `IHighScoreRepository` from Root.

**Files modified:**
- `Assets/_Game/YassinTarek/SimonSays/DI/GameplayLifetimeScope.cs`
- `Assets/_Game/YassinTarek/SimonSays/Infrastructure/SceneLoaderService.cs`

---

**Q:** VContainerException at runtime: `Conflict implementation type: Registration PanelView ContractTypes=[YassinTarek.SimonSays.Views.PanelView]` — fix this issue.

**A:** VContainer forbids multiple `RegisterComponent` calls with the same concrete type. The previous implementation registered each of the 4 `PanelView` instances and 4 `PanelAnimator` instances individually in foreach loops, causing type key collisions.

**Fix:** Remove the foreach loops from `GameplayLifetimeScope` and instead have `PanelRegistry` receive `IEventBus` via injection and distribute it to each panel manually.

1. **`PanelRegistry.cs`** — added `[Inject] Construct(IEventBus)` that chains injection down to each view and animator:
   ```csharp
   [Inject]
   public void Construct(IEventBus eventBus)
   {
       foreach (var view in _panelViews)
           view.Construct(eventBus);
       foreach (var animator in _panelAnimators)
           animator.Construct(eventBus);
   }
   ```

2. **`GameplayLifetimeScope.cs`** — replaced `RegisterInstance` + two foreach loops with a single `RegisterComponent`:
   ```csharp
   // Before:
   builder.RegisterInstance(_panelRegistry);
   foreach (var view in _panelRegistry.PanelViews)
       builder.RegisterComponent(view);
   foreach (var animator in _panelRegistry.PanelAnimators)
       builder.RegisterComponent(animator);

   // After:
   builder.RegisterComponent(_panelRegistry);
   ```
   `RegisterComponent` (unlike `RegisterInstance`) triggers injection, which fires `PanelRegistry.Construct` and chains `IEventBus` down to all panels. No individual panel registrations needed.

**Files modified:**
- `Assets/_Game/YassinTarek/SimonSays/Views/PanelRegistry.cs`
- `Assets/_Game/YassinTarek/SimonSays/DI/GameplayLifetimeScope.cs`
