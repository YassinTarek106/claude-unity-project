# Unity Scene Debug Report
**Date:** 2026-04-09
**Project:** Simon Says (Unity 6, URP)

---

## Summary
- Critical Issues: **3**
- Warnings: **1**
- Passed Checks: **29**

---

## Critical Issues

### 1. FAIL — MainMenu: Missing EventSystem
- **Location:** MainMenu scene (all root GOs and Canvas children)
- **Expected:** EventSystem GameObject present as child or sibling of Canvas
- **Actual:** No EventSystem exists anywhere in the MainMenu scene. UI clicks (StartButton) will not be processed.

### 2. FAIL — Gameplay: Missing EventSystem
- **Location:** Gameplay scene (all root GOs and Canvas children)
- **Expected:** EventSystem GameObject present as child or sibling of Canvas
- **Actual:** No EventSystem exists anywhere in the Gameplay scene. UI clicks (HUD buttons, GameOver panel buttons) will not be processed.

### 3. FAIL — Gameplay: PanelRegistry placed under [Panels] instead of as a separate root GameObject
- **Location:** `[Panels]` hierarchy
- **Expected:** `[Panels]` has exactly 4 children (Panel_Red, Panel_Green, Panel_Blue, Panel_Yellow); `PanelRegistry` is a separate root-level GameObject
- **Actual:** `PanelRegistry` is a child of `[Panels]` (path: `[Panels]/PanelRegistry`), giving `[Panels]` 5 children

---

## Warnings

### 1. WARN — Gameplay: [Infrastructure] does not directly host CoroutineRunner and AudioService
- **Expected:** `[Infrastructure]` root GameObject carries both components directly
- **Actual:** `[Infrastructure]` is an empty parent; `CoroutineRunner` and `AudioService` are on child GameObjects `[Infrastructure]/CoroutineRunner` and `[Infrastructure]/AudioService`
- **Impact:** None — GameplayLifetimeScope holds direct component references by instanceID. No functional bug; structural interpretation ambiguity only.

---

## Passed Checks

- Bootstrapper: `[RootLifetimeScope]` root GameObject exists
- Bootstrapper: `RootLifetimeScope` component present on `[RootLifetimeScope]`
- Bootstrapper: `_gameConfig` field is non-null (instanceID -61592)
- Bootstrapper: `EventSystem` root GameObject present in scene
- VContainerSettings.asset: `RootLifetimeScope` prefab reference is non-null (instanceID 66672)
- MainMenu: `[MainMenuScope]` root GameObject exists
- MainMenu: `MainMenuLifetimeScope` component present on `[MainMenuScope]`
- MainMenu: `_mainMenuController` field is non-null (instanceID 73018)
- MainMenu: `parentReference` correctly set to `RootLifetimeScope`
- MainMenu: `Canvas/MainMenuPanel` has `MainMenuController` component
- MainMenu: `Canvas/MainMenuPanel/StartButton` has `Button` component
- Gameplay: `[GameplayScope]` root GameObject exists with `GameplayLifetimeScope` component
- Gameplay: `_coroutineRunner` is non-null (instanceID 73162)
- Gameplay: `_audioService` is non-null (instanceID 73230)
- Gameplay: `_panelRegistry` is non-null (instanceID 73210)
- Gameplay: `_hudController` is non-null (instanceID 73204)
- Gameplay: `_gameOverController` is non-null (instanceID 73118)
- Gameplay: `parentReference` correctly set to `RootLifetimeScope`
- Gameplay: `[Infrastructure]` container exists with CoroutineRunner and AudioService child GOs
- Gameplay: Panel_Red has PanelView, PanelAnimator, MeshCollider
- Gameplay: Panel_Green has PanelView, PanelAnimator, MeshCollider
- Gameplay: Panel_Blue has PanelView, PanelAnimator, MeshCollider
- Gameplay: Panel_Yellow has PanelView, PanelAnimator, MeshCollider
- Gameplay: All 4 MeshColliders are non-trigger (`m_IsTrigger: 0`)
- Gameplay: `PanelRegistry._panelViews` has 4 non-null entries
- Gameplay: `PanelRegistry._panelAnimators` has 4 non-null entries
- Gameplay: `Canvas/HUD` has HudController; `_scoreText`, `_highScoreText`, `_roundText` all non-null
- Gameplay: `Canvas/GameOver` has GameOverController; `_finalScoreText`, `_retryButton`, `_mainMenuButton` all non-null
- Gameplay: Main Camera has `PhysicsRaycaster` component
- Build Settings: 3 scenes in correct order (Bootstrapper=0, MainMenu=1, Gameplay=2), all enabled
- Console: No MissingReferenceException, VContainer errors, or scene loading errors

---

## Recommended Fixes

### Fix 1 — Add EventSystem to MainMenu scene
Open `Assets/_Game/YassinTarek/Scenes/MainMenu.unity` in the Unity Editor.  
In the Hierarchy: right-click → **UI → Event System**.  
Place the resulting `EventSystem` GameObject at the **root level** (sibling of Canvas).  
Save the scene.

### Fix 2 — Add EventSystem to Gameplay scene
Open `Assets/_Game/YassinTarek/Scenes/Gameplay.unity` in the Unity Editor.  
In the Hierarchy: right-click → **UI → Event System**.  
Place the resulting `EventSystem` GameObject at the **root level** (sibling of Canvas).  
Save the scene.

> **Note on EventSystem duplication:** Since the Bootstrapper scene already has an EventSystem and scenes are loaded additively, there may be duplicate EventSystems at runtime. Unity will log a warning but only one will be active. The correct long-term fix is to remove the EventSystem from Bootstrapper (it has no UI) and ensure exactly one EventSystem exists per scene that has UI — or use a single persistent EventSystem in the root scope with DontDestroyOnLoad.

### Fix 3 — Move PanelRegistry to root level in Gameplay scene
In the Gameplay scene Hierarchy, drag `[Panels]/PanelRegistry` up to the root level (no parent).  
Verify the `GameplayLifetimeScope._panelRegistry` inspector field still shows the correct reference.  
Save the scene.
