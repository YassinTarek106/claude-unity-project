# Conversation: Simon Says Game Architecture Planning

**Date:** 2026-04-09

---

**Q:** Design a Simon Says game class structure for a Unity 6 / URP project. Include an Event Bus and VContainer dependency injection. The game has 4 colored panels with unique tones, growing sequence playback, player input validation, round progression, difficulty scaling (speed increases per round), scoring with high score, and full audio feedback.

**A:** Entered plan mode. Launched two parallel agents:
1. **Explore agent** — surveyed `Assets/`, `.cursorrules`, `Packages/manifest.json`, and `SampleScene`. Found VContainer 1.17.0, Unity Input System 1.18.0, URP 17.3.0, NavMesh, Test Framework installed. Scene has Camera, DirectionalLight, GlobalVolume only.
2. **Plan agent** — designed full class architecture.

Produced an initial plan covering:
- **Folder:** `Assets/SimonSays/` with Core, Config, Services, Infrastructure, Views, UI, DI, Tests subfolders
- **Enums:** `PanelColor`, `GameState`, `SoundId`
- **Events (10):** `GameStartedEvent`, `GameOverEvent`, `RoundCompletedEvent`, `SequencePlaybackStartedEvent/CompletedEvent`, `PanelActivatedEvent`, `PlayerInputReceivedEvent/CorrectEvent/WrongEvent`, `ScoreChangedEvent`
- **EventBus:** `Dictionary<Type, List<Delegate>>` pattern, singleton via VContainer
- **ScriptableObjects:** `GameConfig` (timing, speed, score key), `AudioConfig` (SoundId→AudioClip mapping)
- **Services:** `IGameService/GameService` (orchestrator), `ISequenceService/SequenceService` (coroutine playback via `ICoroutineRunner`), `IAudioService/AudioService` (MonoBehaviour, AudioSource pool), `IInputService/InputService` (enable/disable gate), `IScoreService/ScoreService` (PlayerPrefs persistence)
- **Infrastructure:** `ICoroutineRunner/CoroutineRunner` (empty MonoBehaviour to host coroutines for plain C# services)
- **Views:** `PanelView` (click + animations + subscriptions), `PanelRegistry` (4 PanelView refs)
- **UI:** `StartScreenController`, `HudController`, `GameOverController` — all event-driven
- **DI:** `SimonSaysLifetimeScope` registering everything
- **Scene:** panels as Quads + MeshColliders, PhysicsRaycaster on camera, Canvas with 3 screens

---

**Q:** Change the game folder location to `Assets/_Game/YassinTarek/` with an Assembly Definition named `YassinTarek.asmdef`.

**A:** Updated the plan file at `C:\Users\HP\.claude\plans\lazy-wondering-rivest.md`:
- All scripts moved under `Assets/_Game/YassinTarek/SimonSays/`
- Assembly Definition: `YassinTarek.asmdef` at `Assets/_Game/YassinTarek/`
- Tests: `YassinTarek.Tests.EditMode.asmdef` at `Assets/_Game/YassinTarek/Tests/`
- Namespaces: `YassinTarek.SimonSays.*`

---

**Q:** Send 3 professional software engineering subagents to independently review the plan, then have them discuss cross-review findings, vote (majority of 2/3 needed to approve), and finally create a new subagent to write the agreed plan to a new document.

**A:** Ran a 5-phase multi-agent peer review process:

**Phase 1 — Independent Reviews (3 agents in parallel, no communication):**

- **Reviewer 1 (SOLID & DI)** → REJECT. Critical issues: EventBus singleton implies static state (banned), `IInputService.IsEnabled` polled synchronously breaks decoupling, AudioService autonomous bus subscriptions violate single-orchestrator principle, GameService God class, `RegisterComponentInHierarchy<PanelView>()` only registers 1 of 4 panels.
- **Reviewer 2 (Game Systems & Events)** → REJECT. Critical issues: AudioService dual-subscription creates double ErrorBuzz trigger, PanelView handles click+3 subscriptions+animation (SRP violation), `IsEnabled` poll, GameService God class, ScoreService mixes PlayerPrefs persistence with scoring, `RegisterComponentInHierarchy` misuse.
- **Reviewer 3 (Unity Architecture & Testability)** → REJECT. Critical issues: AudioService registration lifecycle undefined, GameService `IInitializable` not stated (subscription never happens), SequenceService `PlaySequence` has no double-invocation guard, DOTween not declared as dependency, EditMode `SequenceServiceTests` broken (coroutines don't run), EventBus `Unsubscribe` contract never specified (lambda delegates can't be removed by reference — systemic memory leak).

**Phase 2 — Cross-Review Discussion (3 agents in parallel, each sees others' reviews):**

All three refined their positions. Key consensus reached:
1. EventBus lambda unsubscribe = #1 priority (all 3 agreed after cross-review)
2. AudioService must be passive (majority 2/3: R1 + R2 over R3)
3. `RegisterComponentInHierarchy` bug confirmed by all 3
4. GameService God class confirmed by all 3
5. ScoreService persistence SRP confirmed by R2 + R3

**Phase 3 — Election Moderator Agent:**

Tallied 12 confirmed issues (C1–C12) and produced the **Revised Architecture** with all fixes applied:

| # | Fix Applied |
|---|---|
| C1 | EventBus: stored delegate fields + `Unsubscribe` by reference in `OnDestroy`/`Dispose`; lambdas banned |
| C2 | EventBus: zero static state, VContainer owns lifetime |
| C3 | PanelRegistry holds all 8 refs; LifetimeScope iterates and registers each individually |
| C4 | AudioService: zero bus subscriptions; all `Play()` calls explicit from GameService/SequenceService |
| C5 | GameService split: extracted `GameStateMachine` + `RoundManager`; GameService = thin orchestrator |
| C6 | GameService explicitly implements `IInitializable`; subscriptions in `Initialize()` |
| C7 | SequenceService stores `Coroutine` ref; `StopRoutine` called before new `StartRoutine` |
| C8 | PanelView split into `PanelView` (click only) + `PanelAnimator` (animations only) |
| C9 | Defense-in-depth input gating: state machine gate in GameService + local cached bool in PanelView via events |
| C10 | `IHighScoreRepository` interface with `PlayerPrefsHighScoreRepository` (prod) + `InMemoryHighScoreRepository` (tests) |
| C11 | DOTween declared in asmdef; coroutine fallback documented |
| C12 | `PointsPerRound` field in `GameConfig` replaces magic `round * 10` |

**Phase 4 — Document Writer:**

Wrote the final architecture to `D:\Work\Repos\claude-unity-project\ARCHITECTURE.md` covering:
- Full revised folder structure under `Assets/_Game/YassinTarek/`
- Complete EventBus implementation with unsubscribe contract
- All 12 event structs (now structs for zero GC)
- `GameStateMachine`, `RoundManager`, `GameService` (thin), all other services
- `PanelView` (click only) + `PanelAnimator` (6 subscriptions, all stored as fields)
- `IHighScoreRepository` abstraction with two implementations
- `SimonSaysLifetimeScope` with foreach loop registering all 4 PanelViews + 4 PanelAnimators
- All data flows, scene hierarchy, implementation order (16 steps), banned patterns table, fix summary

---

**Q:** Extract the entire conversation into a clean Q&A markdown file.

**A:** Created this file at `.claude/conversations/simon-says-architecture-planning.md`.
