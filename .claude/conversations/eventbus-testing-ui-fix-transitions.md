# Conversation: EventBus Testing, UI Fix & Game State Transitions

**Date:** 2026-04-09

---

**Q:** Analyze then test the EventBus — make sure it's working. (`Assets/_Game/YassinTarek/SimonSays/Core/EventBus/`)

**A:** Read both `IEventBus.cs` and `EventBus.cs`. The implementation is a clean dictionary-based pub/sub with typed struct events:
- `Subscribe<T>` adds handlers to a `Dictionary<Type, List<Delegate>>`
- `Unsubscribe<T>` removes by delegate equality
- `Publish<T>` snapshots the list via `ToArray()` before iterating (safe for mid-dispatch unsubscribe)

Found existing tests at `Assets/_Game/YassinTarek/Tests/EditMode/EventBusTests.cs` with 7 test cases. Ran them via MCP `tests-run` tool — all 7 passed:
- `Subscribe_ReceivesPublishedEvent`
- `Unsubscribe_StopsReceivingEvents`
- `Publish_WithNoSubscribers_DoesNotThrow`
- `Subscribe_MultipleHandlers_AllReceive`
- `Publish_PassesPayloadCorrectly`
- `Publish_DifferentEventTypes_DoNotCross`
- `Unsubscribe_DuringDispatch_DoesNotThrow`

---

**Q:** Test the `HandlePlayerInput` function in `GameService.cs` (line 70).

**A:** Read `GameService.cs` and all its dependencies (interfaces, domain types, events) to understand the method's behavior. Created `GameServiceHandlePlayerInputTests.cs` with 13 tests using hand-written spy/stub test doubles (no mocking library):

**Tests created:**
1. `CorrectInput_PlaysCorrectInputSound`
2. `CorrectInput_PublishesPlayerInputCorrectEvent`
3. `WrongInput_TransitionsToGameOver`
4. `WrongInput_DisablesInput`
5. `WrongInput_PlaysErrorBuzz`
6. `WrongInput_PublishesPlayerInputWrongEvent`
7. `WrongInput_PublishesGameOverEventWithScore`
8. `CorrectInput_LastStep_PlaysRoundWonSound`
9. `CorrectInput_LastStep_AddsRoundScore`
10. `CorrectInput_LastStep_PublishesRoundWonEvent`
11. `CorrectInput_NotLastStep_DoesNotPublishRoundWon`
12. `IgnoresInput_WhenNotInWaitingForInputState`
13. `Dispose_UnsubscribesFromEvents`

Used reflection to set `RoundManager._sequence` for deterministic test setup. All 13 passed.

**File created:** `Assets/_Game/YassinTarek/Tests/EditMode/GameServiceHandlePlayerInputTests.cs`

---

**Q:** UI doesn't appear — neither HUD nor GameOver Screen.

**A:** Investigated the full UI system: `HudController.cs`, `GameOverController.cs`, `GameplayLifetimeScope.cs`, `GameplayEntryPoint.cs`, and the scene wiring.

**Root cause found — race condition in MonoBehaviour lifecycle vs VContainer:**
1. VContainer calls `[Inject] Construct()` on HudController/GameOverController (subscribes to events)
2. VContainer calls `IStartable.Start()` on `GameplayEntryPoint` -> `GameService.StartGame()` -> publishes `GameStartedEvent`
3. HudController receives event, calls `SetActive(true)` -- HUD is visible
4. Unity calls `MonoBehaviour.Start()` on HudController -> `SetActive(false)` -- **HUD hidden again**

**Fix:** Moved `gameObject.SetActive(false)` from `MonoBehaviour.Start()` into `Construct()` (which runs before events are published) for both controllers. Also moved button listener setup into `Construct()` for `GameOverController`.

**Files modified:**
- `Assets/_Game/YassinTarek/SimonSays/UI/HudController.cs`
- `Assets/_Game/YassinTarek/SimonSays/UI/GameOverController.cs`

---

**Q:** I need a transition state between each round — an amount of time as an indicator for the player. Same thing before showing game over. Same thing after starting the game.

**A:** Added timed transition delays at three points in the game flow using coroutines.

**Changes made:**

1. **`GameState.cs`** — Added `Transitioning` enum value between `Idle` and `PlayingSequence`

2. **`GameConfig.cs`** — Added 3 configurable delay fields:
   - `GameStartDelay` = 1s
   - `RoundTransitionDelay` = 1.2s
   - `GameOverDelay` = 1s

3. **`GameService.cs`** — Added `ICoroutineRunner` and `GameConfig` as constructor dependencies. Three flows now use coroutine delays:
   - `StartGame()` -> `Transitioning` state -> wait `GameStartDelay` -> `StartNextRound()`
   - Correct input (last step) -> `Transitioning` state -> wait `RoundTransitionDelay` -> `StartNextRound()`
   - Wrong input -> `Transitioning` state -> wait `GameOverDelay` -> `GameOver` state + publish `GameOverEvent`

4. **`GameServiceHandlePlayerInputTests.cs`** — Updated for new constructor signature (added `StubCoroutineRunner` and `ScriptableObject.CreateInstance<GameConfig>()`). Updated assertions: wrong input and round completion now transition to `Transitioning` instead of `GameOver`/`PlayingSequence`. Added new test `CorrectInput_LastStep_TransitionsToTransitioning` and `WrongInput_StartsGameOverCoroutine`.

**All 44 tests pass** (14 GameService + 7 EventBus + 23 others).

**Files modified:**
- `Assets/_Game/YassinTarek/SimonSays/Core/Domain/GameState.cs`
- `Assets/_Game/YassinTarek/SimonSays/Config/GameConfig.cs`
- `Assets/_Game/YassinTarek/SimonSays/Services/GameService.cs`
- `Assets/_Game/YassinTarek/Tests/EditMode/GameServiceHandlePlayerInputTests.cs`
