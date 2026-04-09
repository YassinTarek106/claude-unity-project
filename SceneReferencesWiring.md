# Scene Architecture — Simon Says Multi-Scene

## Context

The full game logic is implemented (36 scripts, 30 tests passing) but lives in a single `SampleScene` with no scene wiring. This plan introduces a proper multi-scene structure: **Bootstrapper** (persistent root), **MainMenu**, and **Gameplay**. It splits `SimonSaysLifetimeScope` into a VContainer parent-child scope hierarchy and adds scene loading infrastructure.

---

## Scene Structure

| Scene | Build Index | Purpose |
|-------|-------------|---------|
| `Bootstrapper` | 0 | Loads first; DontDestroyOnLoad root scope; additively loads MainMenu |
| `MainMenu` | 1 | Title screen; triggers Gameplay load |
| `Gameplay` | 2 | Full game loop; Retry in-place; Main Menu button unloads and reloads MainMenu |

---

## VContainer Scope Hierarchy

```
RootLifetimeScope  (Bootstrapper — DontDestroyOnLoad)
├── MainMenuLifetimeScope  (MainMenu scene)
└── GameplayLifetimeScope  (Gameplay scene)
```

**Parent resolution: use `VContainerSettings` asset** — not `LifetimeScope.Find<T>()` (not public API).

Setup:
1. `Assets/Resources/VContainerSettings.asset` — Create > VContainer > VContainerSettings
2. Set `Root Lifetime Scope` field to the `RootLifetimeScope` component reference
3. No override code needed on child scopes — VContainer resolves parent automatically at build time

`RootLifetimeScope.Awake()` must call `DontDestroyOnLoad(gameObject)` before `base.Awake()`.

---

## DI Registration Distribution

### RootLifetimeScope — `DI/RootLifetimeScope.cs`
Persists for full game lifetime. Pure C# services only (no MonoBehaviours with runtime pooling hazards).
- `EventBus → IEventBus`
- `GameConfig` (RegisterInstance — ScriptableObject)
- `PlayerPrefsHighScoreRepository → IHighScoreRepository`
- `SceneLoaderService → ISceneLoaderService`
- `BootstrapEntry` (Register as `IStartable` — triggers initial MainMenu load)

> `CoroutineRunner` and `AudioService` are **not** in the root scope — they are MonoBehaviours with runtime-created children (coroutines, AudioSources). Placing them in Root creates stale state across sessions. They live in `GameplayLifetimeScope` and are recreated fresh each session.

### MainMenuLifetimeScope — `DI/MainMenuLifetimeScope.cs`
- `MainMenuController` (RegisterComponent)

### GameplayLifetimeScope — `DI/GameplayLifetimeScope.cs`
Replaces `SimonSaysLifetimeScope`.
- `CoroutineRunner → ICoroutineRunner` (RegisterComponent)
- `AudioService → IAudioService` (RegisterComponent)
- `GameStateMachine`, `RoundManager` (Singleton)
- `GameService` (Singleton, `.AsImplementedInterfaces().AsSelf()`)
- `SequenceService → ISequenceService`, `InputService → IInputService` (Singleton)
- `ScoreService` (Singleton, `.As<IScoreService>().AsImplementedInterfaces()`)
- `PanelRegistry` (RegisterInstance)
- `PanelView × 4`, `PanelAnimator × 4` (RegisterComponent, via PanelRegistry iteration)
- `HudController`, `GameOverController` (RegisterComponent)
- `GameplayEntryPoint` (Register as `IStartable` — calls `StartGame()` after scope builds)

---

## Scene Hierarchies

### Bootstrapper.unity
```
[Root]
  ├── RootLifetimeScope     (DontDestroyOnLoad; SerializedField: _gameConfig)
  └── [no CoroutineRunner or AudioService here]
```

### MainMenu.unity
```
[MainMenuScope]
  └── MainMenuLifetimeScope
[UI]
  └── Canvas (Screen Space Overlay)
        └── EventSystem
        └── MainMenuPanel → MainMenuController  (_startButton)
[Scene]
  └── Main Camera
  └── Directional Light
```

### Gameplay.unity
```
[GameplayScope]
  └── GameplayLifetimeScope  (all SerializedFields: _gameConfig, _coroutineRunner, _audioService,
                               _panelRegistry, _hudController, _gameOverController)
[Infrastructure]
  ├── CoroutineRunner
  └── AudioService
[Panels]
  ├── PanelRegistry                (all 8 refs assigned: 4 × PanelView, 4 × PanelAnimator)
  ├── Panel_Red    (PanelView + PanelAnimator + MeshCollider)
  ├── Panel_Green
  ├── Panel_Blue
  └── Panel_Yellow
[UI]
  └── Canvas (Screen Space Overlay)
        └── EventSystem
        ├── HUD       → HudController      (Score, HighScore, Round labels)
        └── GameOver  → GameOverController  (FinalScore label, Retry button, MainMenu button)
[Scene]
  ├── Main Camera  (+ PhysicsRaycaster)
  ├── Directional Light
  └── Global Volume
```

---

## New Files

| File | Description |
|------|-------------|
| `DI/RootLifetimeScope.cs` | Root scope; DontDestroyOnLoad; registers EventBus, GameConfig, repo, SceneLoader |
| `DI/MainMenuLifetimeScope.cs` | Menu scope; registers MainMenuController |
| `DI/GameplayLifetimeScope.cs` | Gameplay scope; replaces SimonSaysLifetimeScope |
| `Infrastructure/ISceneLoaderService.cs` | Interface: `LoadGameplay()`, `LoadMainMenu()` |
| `Infrastructure/SceneLoaderService.cs` | Async additive load + unload with double-load guard |
| `Bootstrap/BootstrapEntry.cs` | `IStartable` — calls `LoadMainMenu()` on boot |
| `Bootstrap/GameplayEntryPoint.cs` | `IStartable` — calls `_gameService.StartGame()` on scene load |

---

## Modified Files

### `UI/MainMenuController.cs`
- **Remove** `GameService` injection
- **Add** `ISceneLoaderService` injection
- **Remove** `GameStartedEvent` subscription (MainMenu will unload when Gameplay loads)
- `OnStartClicked()` → `_sceneLoader.LoadGameplay()`
- `OnDestroy()` — button listener only; no bus cleanup needed

### `UI/GameOverController.cs`
- **Keep** `GameService` injection (Retry stays in-scene)
- **Add** `ISceneLoaderService` injection
- **Add** `[SerializeField] Button _mainMenuButton`
- `OnMainMenuClicked()` → `_sceneLoader.LoadMainMenu()`
- `OnDestroy()` — add removal of main menu button listener

### **Deleted:** `DI/SimonSaysLifetimeScope.cs`

---

## SceneLoaderService Implementation

```csharp
public sealed class SceneLoaderService : ISceneLoaderService
{
    private bool _isLoading;

    public void LoadGameplay() => LoadAdditiveThenUnload("Gameplay", "MainMenu");
    public void LoadMainMenu() => LoadAdditiveThenUnload("MainMenu", "Gameplay");

    private async void LoadAdditiveThenUnload(string load, string unload)
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            await SceneManager.LoadSceneAsync(load, LoadSceneMode.Additive);
            await SceneManager.UnloadSceneAsync(unload);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneLoaderService] Failed to load {load}: {e}");
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

No `MonoBehaviour` dependency. Uses Unity 6's awaitable `AsyncOperation` natively. Double-load guard via `_isLoading` flag.

---

## Load / Unload Sequence

**Boot:**
```
Bootstrapper (index 0) → RootLifetimeScope.Awake() → DontDestroyOnLoad → container builds
→ BootstrapEntry.Start() → SceneLoaderService.LoadMainMenu()
→ MainMenu loads additively → MainMenuLifetimeScope auto-parents to Root (VContainerSettings)
→ IEventBus, ISceneLoaderService resolved from Root parent scope
```

**Start Game:**
```
MainMenuController.OnStartClicked() → ISceneLoaderService.LoadGameplay()
→ Gameplay loads additively → MainMenu unloads
→ GameplayLifetimeScope builds (auto-parented to Root)
→ GameplayEntryPoint.Start() → IGameService.StartGame()
```

**Retry (in-place, no scene change):**
```
GameOverController.OnRetryClicked() → IGameService.StartGame()
```

**Return to Main Menu:**
```
GameOverController.OnMainMenuClicked() → ISceneLoaderService.LoadMainMenu()
→ MainMenu loads additively → Gameplay unloads
→ GameplayLifetimeScope.Dispose() → GameService.Dispose() called (unsubscribes from EventBus)
→ MainMenuLifetimeScope builds fresh
```

---

## Implementation Order

1. Create `ISceneLoaderService.cs` and `SceneLoaderService.cs`
2. Create `BootstrapEntry.cs` and `GameplayEntryPoint.cs`
3. Create `RootLifetimeScope.cs`, `MainMenuLifetimeScope.cs`, `GameplayLifetimeScope.cs`
4. Modify `MainMenuController.cs` and `GameOverController.cs`
5. Delete `SimonSaysLifetimeScope.cs`
6. Create `VContainerSettings.asset` via MCP
7. Create and wire Bootstrapper, MainMenu, Gameplay scenes via MCP

---

## Verification Checklist

1. Play → Bootstrapper scene → `RootLifetimeScope` in Hierarchy under `DontDestroyOnLoad`
2. MainMenu loads additively — `MainMenuLifetimeScope` resolves `IEventBus` from Root (no null ref)
3. Click Start → Gameplay loads additively → MainMenu unloads — confirm in Hierarchy
4. Sequence plays immediately (`GameplayEntryPoint.Start()` fired)
5. Wrong input → Game Over panel — Retry → sequence restarts without scene reload
6. Main Menu button → Gameplay unloads → `GameService.Dispose()` logged → MainMenu fresh
7. High score persists across sessions (`PlayerPrefs` survives all scene changes)
8. Double-click Start → game loads only once (`_isLoading` guard)
9. Zero errors and zero null refs in Console throughout all flows
