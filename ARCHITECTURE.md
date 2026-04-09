# Simon Says — Game Architecture

**Reviewed & Approved by 3-Agent Engineering Panel**  
**Namespace:** `YassinTarek.SimonSays.*`  
**Stack:** Unity 6 (6000.3.7f1) · URP v17.3.0 · VContainer · Custom EventBus

---

## 1. Folder Structure

```
Assets/_Game/YassinTarek/
├── YassinTarek.asmdef              (refs: VContainer, DOTween.Modules*)
└── SimonSays/
    ├── Config/
    │   ├── GameConfig.cs           (ScriptableObject)
    │   └── GameConfig.asset
    ├── Core/
    │   ├── EventBus/
    │   │   ├── IEventBus.cs
    │   │   └── EventBus.cs
    │   ├── Events/                 (12 event structs)
    │   └── Domain/
    │       ├── PanelColor.cs
    │       ├── GameState.cs
    │       └── SoundId.cs
    ├── Infrastructure/
    │   ├── ICoroutineRunner.cs
    │   ├── CoroutineRunner.cs      (MonoBehaviour)
    │   ├── IHighScoreRepository.cs
    │   ├── PlayerPrefsHighScoreRepository.cs
    │   └── InMemoryHighScoreRepository.cs
    ├── Services/
    │   ├── GameStateMachine.cs
    │   ├── RoundManager.cs
    │   ├── GameService.cs
    │   ├── ISequenceService.cs / SequenceService.cs
    │   ├── IAudioService.cs / AudioService.cs  (MonoBehaviour)
    │   ├── IInputService.cs / InputService.cs
    │   └── IScoreService.cs / ScoreService.cs
    ├── Views/
    │   ├── PanelRegistry.cs
    │   ├── PanelView.cs            (click input only)
    │   └── PanelAnimator.cs        (animations only)
    ├── UI/
    │   ├── MainMenuController.cs
    │   ├── HudController.cs
    │   └── GameOverController.cs
    └── DI/
        └── SimonSaysLifetimeScope.cs
Tests/
├── YassinTarek.Tests.EditMode.asmdef  (Editor-only)
└── EditMode/
    ├── EventBusTests.cs
    ├── RoundManagerTests.cs
    ├── ScoreServiceTests.cs
    └── GameStateMachineTests.cs
```

*DOTween: if installed, reference `DOTween.Modules` in the asmdef and use the DOTween API in `PanelAnimator`. If absent, use `ICoroutineRunner` + `Color.Lerp` instead. Document the choice at the top of `PanelAnimator.cs`.

---

## 2. Assembly Definitions

| File | Location | References |
|---|---|---|
| `YassinTarek.asmdef` | `Assets/_Game/YassinTarek/` | VContainer, DOTween.Modules |
| `YassinTarek.Tests.EditMode.asmdef` | `Assets/_Game/YassinTarek/Tests/` | YassinTarek, UnityEngine.TestRunner, UnityEditor.TestRunner |

Tests asmdef: `"includePlatforms": ["Editor"]`

---

## 3. Enums

```
PanelColor : Red, Green, Blue, Yellow
GameState  : Idle, PlayingSequence, WaitingForInput, GameOver
SoundId    : RedTone, GreenTone, BlueTone, YellowTone,
             CorrectInput, ErrorBuzz, RoundWon, GameOver
```

`PanelColor` is the universal key across sequences, events, audio maps, and view lookups.

---

## 4. GameConfig (ScriptableObject)

No magic numbers anywhere in gameplay code.

| Field | Type | Default | Purpose |
|---|---|---|---|
| `PointsPerRound` | int | 10 | Score formula multiplier |
| `SequenceStepDuration` | float | 0.6s | How long each panel stays lit |
| `SequenceStepGap` | float | 0.15s | Pause between lit panels |
| `PanelFlashDuration` | float | 0.3s | Player-input flash duration |
| `InitialSequenceLength` | int | 1 | Steps in round 1 |

Create instance: right-click > Create > SimonSays > GameConfig.

---

## 5. Event Catalogue

All events are **plain C# structs** (value types — zero GC pressure on publish).

| Event | Payload | Published By | Subscribed By |
|---|---|---|---|
| `GameStartedEvent` | — | GameService | PanelAnimator×4, HudController, MainMenuController |
| `GameOverEvent` | `int FinalScore` | GameService | GameOverController, HudController |
| `RoundStartedEvent` | `int Round` | GameService | HudController |
| `RoundWonEvent` | `int Round` | GameService | HudController |
| `PanelActivatedEvent` | `PanelColor Color` | SequenceService | PanelAnimator×4 (filter by color) |
| `PlayerInputReceivedEvent` | `PanelColor Color` | PanelView×4 | GameService |
| `PlayerInputCorrectEvent` | `PanelColor Color` | GameService | PanelAnimator×4 (filter by color) |
| `PlayerInputWrongEvent` | — | GameService | PanelAnimator×4 |
| `ScoreChangedEvent` | `int Score, int HighScore` | ScoreService | HudController |
| `InputEnabledEvent` | — | InputService | PanelView×4, PanelAnimator×4 |
| `InputDisabledEvent` | — | InputService | PanelView×4, PanelAnimator×4 |
| `GameStateChangedEvent` | `GameState Prev, GameState Next` | GameStateMachine | Debug/logging |

---

## 6. EventBus

### IEventBus

```csharp
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler)   where T : struct;
    void Unsubscribe<T>(Action<T> handler) where T : struct;
    void Publish<T>(T evt)                 where T : struct;
}
```

### EventBus Implementation

```csharp
// ZERO static state — VContainer owns the single instance
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : struct
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
        {
            list = new List<Delegate>();
            _handlers[typeof(T)] = list;
        }
        list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public void Publish<T>(T evt) where T : struct
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;
        // Snapshot copy tolerates Unsubscribe-during-dispatch
        var snapshot = list.ToArray();
        foreach (var d in snapshot)
            ((Action<T>)d).Invoke(evt);
    }
}
```

Registered: `builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>()`

### Unsubscribe Contract — MANDATORY FOR EVERY SUBSCRIBER

**Lambda expressions passed directly to Subscribe are BANNED.** They cannot be removed by reference.

**MonoBehaviour pattern:**
```csharp
private Action<InputEnabledEvent> _onInputEnabled;

[Inject]
public void Construct(IEventBus eventBus)
{
    _eventBus = eventBus;
    _onInputEnabled = HandleInputEnabled;     // named method — storable reference
    _eventBus.Subscribe(_onInputEnabled);
}

private void OnDestroy() => _eventBus?.Unsubscribe(_onInputEnabled);
```

**Plain C# service pattern (IInitializable + IDisposable):**
```csharp
private Action<PlayerInputReceivedEvent> _onPlayerInput;

public void Initialize()
{
    _onPlayerInput = HandlePlayerInput;
    _eventBus.Subscribe(_onPlayerInput);
}

public void Dispose() => _eventBus.Unsubscribe(_onPlayerInput);
```

---

## 7. Infrastructure

### ICoroutineRunner / CoroutineRunner

```csharp
public interface ICoroutineRunner
{
    Coroutine StartRoutine(IEnumerator routine);
    void StopRoutine(Coroutine coroutine);
}

public sealed class CoroutineRunner : MonoBehaviour, ICoroutineRunner
{
    public Coroutine StartRoutine(IEnumerator routine) => StartCoroutine(routine);
    public void StopRoutine(Coroutine c) { if (c != null) StopCoroutine(c); }
}
```

Pre-placed in scene under `[Infrastructure]`. Registered: `builder.RegisterComponent(_runner).As<ICoroutineRunner>()`.

### IHighScoreRepository

```csharp
public interface IHighScoreRepository
{
    int Load();
    void Save(int value);
}
```

| Implementation | Storage | Use |
|---|---|---|
| `PlayerPrefsHighScoreRepository` | PlayerPrefs key `"SimonSays_HighScore"` | Production |
| `InMemoryHighScoreRepository` | private int field | EditMode tests — no disk side-effects |

---

## 8. Services

### GameStateMachine

Plain C# class. Owns `GameState` transitions. No bus subscriptions.

```
Current: GameState = Idle

TransitionTo(GameState next):
    var prev = Current
    Current = next
    Publish GameStateChangedEvent { Previous = prev, Next = next }

Is(GameState state) → bool
```

### RoundManager

Pure plain C# class. Zero Unity dependencies. Fully EditMode-testable.

```
int CurrentRound (read-only)
IReadOnlyList<PanelColor> Sequence (read-only)

StartNewRound()
    → CurrentRound++
    → Append random PanelColor to sequence
    → _expectedStepIndex = 0

GetExpectedColor() → PanelColor
    → _sequence[_expectedStepIndex]

AdvanceStep() → bool
    → _expectedStepIndex++
    → returns true if _expectedStepIndex >= _sequence.Count (round complete)

Reset()
    → CurrentRound = 0, clear sequence, _expectedStepIndex = 0
```

### GameService

Thin orchestrator. Implements `IInitializable`, `IDisposable`.

**Injected:** `IEventBus`, `GameStateMachine`, `RoundManager`, `ISequenceService`, `IAudioService`, `IScoreService`, `IInputService`

```
Initialize():
    _onPlayerInput = HandlePlayerInput
    _eventBus.Subscribe(_onPlayerInput)

StartGame():
    RoundManager.Reset()
    ScoreService.ResetScore()
    StateMachine.TransitionTo(Idle)
    Publish GameStartedEvent
    StartNextRound()

StartNextRound() [private]:
    RoundManager.StartNewRound()
    StateMachine.TransitionTo(PlayingSequence)
    InputService.Disable()
    Publish RoundStartedEvent { Round = RoundManager.CurrentRound }
    SequenceService.PlaySequence(RoundManager.Sequence, OnSequenceComplete)

OnSequenceComplete() [private]:
    StateMachine.TransitionTo(WaitingForInput)
    InputService.Enable()

HandlePlayerInput(PlayerInputReceivedEvent evt) [private]:
    if (!StateMachine.Is(WaitingForInput)) return          // GATE 1

    if (evt.Color != RoundManager.GetExpectedColor()):     // WRONG
        StateMachine.TransitionTo(GameOver)
        InputService.Disable()
        AudioService.Play(SoundId.ErrorBuzz)
        Publish PlayerInputWrongEvent
        Publish GameOverEvent { FinalScore = ScoreService.Score }
        return

    AudioService.Play(SoundId.CorrectInput)                // CORRECT
    Publish PlayerInputCorrectEvent { Color = evt.Color }

    if (RoundManager.AdvanceStep()):                       // ROUND COMPLETE
        AudioService.Play(SoundId.RoundWon)
        ScoreService.AddRoundScore(RoundManager.CurrentRound)
        Publish RoundWonEvent { Round = RoundManager.CurrentRound }
        StartNextRound()

Dispose():
    _eventBus.Unsubscribe(_onPlayerInput)
```

### ISequenceService / SequenceService

Plain C# class. Publishes events. Calls `IAudioService.Play()` for panel tones.

```
Injected: ICoroutineRunner, IAudioService, IEventBus, GameConfig

Fields: Coroutine _activeCoroutine

PlaySequence(IReadOnlyList<PanelColor> sequence, Action onComplete):
    _runner.StopRoutine(_activeCoroutine)    // cancel any in-flight coroutine first
    _activeCoroutine = _runner.StartRoutine(PlaySequenceRoutine(sequence, onComplete))

PlaySequenceRoutine (IEnumerator):
    foreach color in sequence:
        Publish PanelActivatedEvent { Color = color }
        AudioService.Play(PanelColorToSoundId(color))
        yield WaitForSeconds(config.SequenceStepDuration)
        yield WaitForSeconds(config.SequenceStepGap)
    _activeCoroutine = null
    onComplete?.Invoke()

PanelColorToSoundId: Red→RedTone, Green→GreenTone, Blue→BlueTone, Yellow→YellowTone
```

### IAudioService / AudioService

**MonoBehaviour.** Pre-placed in scene under `[AudioManager]`. **Zero bus subscriptions.** Called explicitly by `GameService` and `SequenceService` only.

```
[SerializeField] SoundEntry[] _soundMap    (SoundEntry { SoundId Id; AudioClip Clip; })
List<AudioSource> _pool

Play(SoundId id):
    clip = FindClip(id)
    source = GetIdlePooledSource()         // reuse idle or instantiate new child AudioSource
    source.clip = clip
    source.Play()                          // overlap supported via pool
```

Registered: `builder.RegisterComponent(_audioService).As<IAudioService>()`

### IInputService / InputService

Plain C# class. Publishes enable/disable events. No bus subscriptions. **No `IsEnabled` bool on the interface.**

```csharp
public interface IInputService
{
    void Enable();
    void Disable();
}
```

```
Enable():   if (!_isEnabled) → _isEnabled = true,  Publish InputEnabledEvent
Disable():  if (_isEnabled)  → _isEnabled = false, Publish InputDisabledEvent
```

### IScoreService / ScoreService

Plain C# class. Implements `IInitializable`.

**Injected:** `IEventBus`, `IHighScoreRepository`, `GameConfig`

```
Initialize(): _highScore = _repository.Load()

AddRoundScore(int round):
    _score += round * _config.PointsPerRound     // no magic number
    if (_score > _highScore):
        _highScore = _score
        _repository.Save(_highScore)
    Publish ScoreChangedEvent { Score = _score, HighScore = _highScore }

ResetScore(): _score = 0
Score { get } → _score
```

---

## 9. View Layer

### PanelRegistry

MonoBehaviour. Serialized refs to all 4 `PanelView` and 4 `PanelAnimator` instances. `SimonSaysLifetimeScope` iterates both arrays and calls `builder.RegisterComponent(component)` for each.

```csharp
public sealed class PanelRegistry : MonoBehaviour
{
    [SerializeField] private PanelView[] _panelViews;
    [SerializeField] private PanelAnimator[] _panelAnimators;

    public PanelView[] PanelViews => _panelViews;
    public PanelAnimator[] PanelAnimators => _panelAnimators;
}
```

### PanelView (click input only)

```
[SerializeField] PanelColor _color
[Inject] IEventBus _eventBus
bool _isInputEnabled = false

Private delegate fields:
    _onInputEnabled  = _ => _isInputEnabled = true
    _onInputDisabled = _ => _isInputEnabled = false

[Inject] Construct(IEventBus eventBus):
    Subscribe _onInputEnabled, _onInputDisabled

IPointerClickHandler.OnPointerClick():
    if (!_isInputEnabled) return                           // GATE 2
    Publish PlayerInputReceivedEvent { Color = _color }

OnDestroy():
    _eventBus?.Unsubscribe(_onInputEnabled)
    _eventBus?.Unsubscribe(_onInputDisabled)
```

Requires a `PhysicsRaycaster` on the camera for 3D click detection.

### PanelAnimator (animations only)

```
[SerializeField] PanelColor _color
[Inject] IEventBus _eventBus

Private delegate fields (6 total):
    _onPanelActivated, _onCorrect, _onWrong
    _onGameStarted, _onInputEnabled, _onInputDisabled

[Inject] Construct(IEventBus eventBus):
    Assign all delegates to named handler methods
    Subscribe all 6 delegates

Handlers:
    OnPanelActivated(PanelActivatedEvent evt):   if Color matches → PlayFlashAnimation()
    OnCorrect(PlayerInputCorrectEvent evt):       if Color matches → PlayCorrectAnimation()
    OnWrong(PlayerInputWrongEvent):               PlayWrongAnimation() (all 4 respond)
    OnGameStarted:                               ResetVisualState()
    OnInputEnabled / OnInputDisabled:             optional visual dimming

PlayFlashAnimation():
    // DOTween: tween emission on MaterialPropertyBlock
    // Fallback: IEnumerator lerp via ICoroutineRunner

OnDestroy():
    Unsubscribe all 6 stored delegates
```

---

## 10. UI Controllers

Each MonoBehaviour manages ONLY its own `gameObject.SetActive()`. No gameplay code calls `SetActive` on any controller.

### MainMenuController
- Visible on scene load
- Hides on `GameStartedEvent`
- Start button → `_gameService.StartGame()`
- Stores delegate, unsubscribes in `OnDestroy`

### HudController
- Shows on `GameStartedEvent`, hides on `GameOverEvent`
- Updates on `ScoreChangedEvent`, `RoundStartedEvent`, `RoundWonEvent`
- Stores all delegates, unsubscribes in `OnDestroy`

### GameOverController
- Shows on `GameOverEvent` (populates final score text)
- Hides on `GameStartedEvent`
- Retry button → `_gameService.StartGame()`
- Stores delegates, unsubscribes in `OnDestroy`

---

## 11. VContainer — SimonSaysLifetimeScope

```csharp
public sealed class SimonSaysLifetimeScope : LifetimeScope
{
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private CoroutineRunner _coroutineRunner;
    [SerializeField] private AudioService _audioService;      // pre-placed MonoBehaviour
    [SerializeField] private PanelRegistry _panelRegistry;
    [SerializeField] private HudController _hudController;
    [SerializeField] private MainMenuController _mainMenuController;
    [SerializeField] private GameOverController _gameOverController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_gameConfig);

        builder.RegisterComponent(_coroutineRunner).As<ICoroutineRunner>();
        builder.Register<PlayerPrefsHighScoreRepository>(Lifetime.Singleton)
               .As<IHighScoreRepository>();

        // EventBus: plain C# instance, zero static state
        builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();

        // Plain C# services
        builder.Register<GameStateMachine>(Lifetime.Singleton);
        builder.Register<RoundManager>(Lifetime.Singleton);
        builder.Register<GameService>(Lifetime.Singleton)
               .AsImplementedInterfaces().AsSelf();
        builder.Register<SequenceService>(Lifetime.Singleton).As<ISequenceService>();
        builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
        builder.Register<ScoreService>(Lifetime.Singleton)
               .As<IScoreService>().AsImplementedInterfaces();

        // MonoBehaviour services
        builder.RegisterComponent(_audioService).As<IAudioService>();

        // Register all 4 PanelViews and 4 PanelAnimators individually
        builder.RegisterInstance(_panelRegistry);
        foreach (var view in _panelRegistry.PanelViews)
            builder.RegisterComponent(view);
        foreach (var animator in _panelRegistry.PanelAnimators)
            builder.RegisterComponent(animator);

        // UI
        builder.RegisterComponent(_hudController);
        builder.RegisterComponent(_mainMenuController);
        builder.RegisterComponent(_gameOverController);
    }
}
```

`.AsImplementedInterfaces()` causes VContainer to invoke `Initialize()` on all `IInitializable` post-injection, and `Dispose()` on all `IDisposable` at scope teardown.

---

## 12. Scene Hierarchy

```
[SimonSays Root]
  └── SimonSaysLifetimeScope          (all Inspector refs assigned)

[Infrastructure]
  └── CoroutineRunner

[AudioManager]
  └── AudioService                    (child AudioSources added at runtime)

[Panels]
  ├── PanelRegistry                   (all 8 component refs assigned in Inspector)
  ├── Panel_Red
  │     ├── PanelView     (_color = Red)
  │     └── PanelAnimator (_color = Red)
  ├── Panel_Green
  ├── Panel_Blue
  └── Panel_Yellow

[UI]
  └── Canvas (Screen Space Overlay)
        ├── MainMenu      (MainMenuController + Start Button)
        ├── HUD           (HudController + Score/HighScore/Round labels)
        └── GameOver      (GameOverController + FinalScore label + Retry Button)

[Scene]
  ├── Main Camera (+ PhysicsRaycaster)
  ├── Directional Light
  └── Global Volume
```

---

## 13. Key Data Flows

### Game Start
```
Player → Start → MainMenuController → GameService.StartGame()
  → Reset + GameStartedEvent
  → StartNextRound()
    → InputService.Disable() → InputDisabledEvent → PanelView._isInputEnabled = false
    → RoundStartedEvent → HudController updates
    → SequenceService.PlaySequence(sequence, OnSequenceComplete)
      → per step: PanelActivatedEvent (PanelAnimator flashes) + AudioService.Play(panel tone)
    → OnSequenceComplete() → InputService.Enable() → InputEnabledEvent
```

### Player Input (Correct)
```
Click → PanelView.OnPointerClick()
  → GATE 2: _isInputEnabled
  → PlayerInputReceivedEvent → GameService.HandlePlayerInput()
    → GATE 1: StateMachine.Is(WaitingForInput)
    → color match → AudioService.Play(CorrectInput) + PlayerInputCorrectEvent
    → if round complete → ScoreService.AddRoundScore() + RoundWonEvent + StartNextRound()
```

### Game Over
```
Wrong click → GameService.HandlePlayerInput()
  → Mismatch → InputService.Disable()
  → AudioService.Play(ErrorBuzz)            [single call — no double-trigger]
  → PlayerInputWrongEvent → PanelAnimator×4 wrong animation
  → GameOverEvent → GameOverController shows, HudController hides
```

---

## 14. Implementation Order

| # | Deliverable | Tests |
|---|---|---|
| 1 | `GameConfig.cs` + `.asset` | — |
| 2 | Enums (`PanelColor`, `GameState`, `SoundId`) | — |
| 3 | All 12 event structs | — |
| 4 | `IEventBus`, `EventBus` | EditMode: subscribe/unsubscribe/publish |
| 5 | `IHighScoreRepository` + both impls | EditMode: load/save with InMemory |
| 6 | `ICoroutineRunner`, `CoroutineRunner` | Manual |
| 7 | `RoundManager` | EditMode: StartNewRound, AdvanceStep, Reset |
| 8 | `GameStateMachine` | EditMode: state transitions |
| 9 | `IInputService`, `InputService` | EditMode: enable/disable events |
| 10 | `IScoreService`, `ScoreService` | EditMode: score formula, high score |
| 11 | `IAudioService`, `AudioService` | Manual |
| 12 | `ISequenceService`, `SequenceService` | PlayMode: timing, double-run guard |
| 13 | `GameService` | PlayMode: full game loop |
| 14 | `PanelRegistry`, `PanelView`, `PanelAnimator` | Manual visual |
| 15 | UI Controllers | Manual UI |
| 16 | `SimonSaysLifetimeScope` + scene wiring | Smoke test: 3 rounds → game over → retry |

---

## 15. Banned Patterns

| Category | Banned |
|---|---|
| DI | `FindObjectOfType`, static singleton, Service Locator, `new Service()` in MonoBehaviours |
| Events | `UnityEvent`/`UnityAction` for cross-service, SO event channels, third-party event libs |
| EventBus | Any static field on `EventBus` — lambdas directly to `Subscribe` without field storage |
| Audio | `AudioSource.Play()` in gameplay code — `AudioService` subscribing to bus events |
| UI | `SetActive` from gameplay code (controllers manage own visibility only) |
| Code | `#region`, commented-out code, magic numbers, God classes (>150 lines) |
| Input interface | `IsEnabled` bool on `IInputService` |
| Persistence | Direct `PlayerPrefs` in `ScoreService` — use `IHighScoreRepository` |

---

## 16. Peer Review Fix Summary

| # | Issue Found | Resolution Applied |
|---|---|---|
| C1 | EventBus unsubscribe unspecified — lambda memory leak | Stored delegate fields; `Unsubscribe` by reference in `OnDestroy`/`Dispose` |
| C2 | EventBus static state | `EventBus` has only instance fields; VContainer owns lifetime |
| C3 | `RegisterComponentInHierarchy` registers 1 of 4 PanelViews | `PanelRegistry` holds all refs; LifetimeScope iterates and registers each |
| C4 | AudioService dual-subscription double-triggers ErrorBuzz | AudioService has zero bus subscriptions; all `Play()` calls are explicit |
| C5 | GameService God class (5 deps, 5 behaviors) | Extracted `GameStateMachine` and `RoundManager`; GameService is thin orchestrator |
| C6 | `IInitializable` not stated for GameService | Explicitly implements `IInitializable`; subscriptions happen in `Initialize()` |
| C7 | SequenceService double-invocation — two coroutines race | Stores `Coroutine` ref; `StopRoutine` called before any new `StartRoutine` |
| C8 | PanelView SRP violation (click + 3 subs + animation) | Split into `PanelView` (click only) and `PanelAnimator` (animations only) |
| C9 | Input gating via synchronous `IsEnabled` poll | Defense-in-depth: state machine gate in GameService + local bool in PanelView via events |
| C10 | ScoreService mixes scoring with PlayerPrefs persistence | `IHighScoreRepository` with prod/test implementations |
| C11 | DOTween undeclared as dependency | Added to asmdef; coroutine fallback documented |
| C12 | Magic number `round * 10` in ScoreService | `PointsPerRound` field in `GameConfig` |
