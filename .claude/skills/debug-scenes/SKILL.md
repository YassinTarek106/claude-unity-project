---
name: debug-scenes
description: |-
  Multi-agent Unity scene debugger. Spawns three sequential agents:
  1. Researcher — reads all C# scripts and scene YAML files to build a full codebase map.
  2. Summarizer — digests the researcher output into a structured reference.
  3. Debugger — opens every scene in the multi-scene hierarchy via MCP tools and cross-checks
     GameObjects, components, serialized references, and DI wiring against the expected architecture.
  Writes all findings to a timestamped markdown bug report in .claude/debug-reports/.
user_invocable: true
---

# Skill: debug-scenes

Invoke this skill with `/debug-scenes` to run a full diagnostic of the Unity multi-scene hierarchy and produce a bug report.

---

## Workflow

Execute the three agents below **in order** (each depends on the previous one's output). Do NOT collapse them into one prompt — keeping them separate preserves context budget and gives each agent a single clear responsibility.

---

### Agent 1 — Researcher

**Goal:** Build a complete map of the codebase without touching the Unity Editor.

**Prompt to pass:**

```
You are a code researcher for a Unity 6 project.

Your job is to read every relevant source file and produce a structured reference document that the next agent can use to validate the live Unity scene hierarchy.

Project root: D:\Work\Repos\claude-unity-project
Key directories to read:
  - Assets/_Game/YassinTarek/SimonSays/DI/         (LifetimeScope files)
  - Assets/_Game/YassinTarek/SimonSays/Bootstrap/   (entry points)
  - Assets/_Game/YassinTarek/SimonSays/Services/    (all service interfaces + implementations)
  - Assets/_Game/YassinTarek/SimonSays/UI/          (all UI controllers)
  - Assets/_Game/YassinTarek/SimonSays/Views/       (view components)
  - Assets/_Game/YassinTarek/SimonSays/Infrastructure/ (ISceneLoaderService, CoroutineRunner, etc.)
  - Assets/_Game/YassinTarek/Scenes/                (read the raw YAML of all .unity files)
  - Assets/Resources/VContainerSettings.asset        (root prefab wiring)

For each LifetimeScope (Root, MainMenu, Gameplay) document:
  - Which scene it belongs to
  - Every [SerializeField] field name + expected type
  - Every container.Register / container.RegisterComponent call
  - IStartable implementations

For each scene YAML file document:
  - Every root GameObject name
  - Every component type listed under each root GameObject

For each service/UI/view class document:
  - Constructor parameters (constructor injection) — names + types
  - [Inject] method parameters — names + types
  - [SerializeField] fields — names + types

Output your findings as a structured markdown document called RESEARCH_RESULT.
Do not make any file edits. Only read.
```

Store the agent's returned markdown as `RESEARCH_RESULT`.

---

### Agent 2 — Summarizer

**Goal:** Compress `RESEARCH_RESULT` into a compact validation checklist.

**Prompt to pass** (include `RESEARCH_RESULT` verbatim in the prompt):

```
You are a summarizer. You have received the following codebase research report from a researcher agent:

<RESEARCH_RESULT>
{INSERT RESEARCH_RESULT HERE}
</RESEARCH_RESULT>

Produce a compact validation checklist in markdown with these sections:

## Expected Scene Hierarchy
List each scene (Bootstrapper / MainMenu / Gameplay), its expected root GameObjects, and the component(s) expected on each.

## Expected VContainer Wiring
For each LifetimeScope, list every [SerializeField] that must be non-null and every type that must be registered.

## Expected Constructor / Inject Dependencies
For each service, UI controller, and view, list the types it expects to receive via constructor injection or [Inject] method.

## Cross-Scene DI Rules
Describe parent/child scope relationships and which scopes are siblings.

Keep each entry to one line. No prose. Output ONLY the checklist — call it VALIDATION_CHECKLIST.
```

Store the agent's returned markdown as `VALIDATION_CHECKLIST`.

---

### Agent 3 — Debugger

**Goal:** Open every scene via MCP tools and validate the live hierarchy against `VALIDATION_CHECKLIST`. Produce the final bug report.

**Prompt to pass** (include both documents):

```
You are a Unity scene debugger. You have a validation checklist to verify against the live Unity Editor.

<VALIDATION_CHECKLIST>
{INSERT VALIDATION_CHECKLIST HERE}
</VALIDATION_CHECKLIST>

Use MCP tools to inspect the live Unity project. Follow this exact sequence:

STEP 1 — Bootstrapper scene
  a. Use scene-open to open: Assets/_Game/YassinTarek/Scenes/Bootstrapper.unity
  b. Use scene-get-data to list all root GameObjects.
  c. For each root GO, use gameobject-component-list-all to list components.
  d. For the RootLifetimeScope component, use gameobject-component-get to read all serialized fields.
     Check every [SerializeField] expected by VALIDATION_CHECKLIST is non-null and the correct type.
  e. Check that VContainerSettings.asset references the correct prefab using assets-get-data on
     Assets/Resources/VContainerSettings.asset.

STEP 2 — MainMenu scene
  a. Use scene-open to open: Assets/_Game/YassinTarek/Scenes/MainMenu.unity
  b. Use scene-get-data to list all root GameObjects.
  c. Verify expected root GOs exist: [MainMenuScope], Canvas (with EventSystem).
  d. Use gameobject-component-get on [MainMenuScope] to read MainMenuLifetimeScope fields.
     Verify MainMenuController reference is set.
  e. Navigate to the Canvas > MainMenuPanel child, verify MainMenuController component exists.
  f. Check there are NO missing (null) MonoBehaviour references on any inspected component.

STEP 3 — Gameplay scene
  a. Use scene-open to open: Assets/_Game/YassinTarek/Scenes/Gameplay.unity
  b. Use scene-get-data to list all root GameObjects.
  c. Verify expected root GOs exist: [GameplayScope], [Infrastructure], [Panels], Canvas, Main Camera.
  d. Use gameobject-component-get on [GameplayScope] / GameplayLifetimeScope:
       - coroutineRunner, audioService, panelRegistry, hudController, gameOverController
       - All 5 must be non-null.
  e. Use gameobject-find to find PanelRegistry. Use gameobject-component-get to read its
     panelViews and panelAnimators arrays — verify all 4 slots are filled.
  f. Use gameobject-find to find [Infrastructure]. List its child GameObjects. Verify
     CoroutineRunner and AudioService components are present.
  g. Use gameobject-find to find [Panels]. Verify 4 child GameObjects exist
     (Panel_Red, Panel_Green, Panel_Blue, Panel_Yellow). For each panel, verify:
       - PanelView component is present
       - PanelAnimator component is present
       - MeshCollider component is present (isTrigger = false)
  h. Use gameobject-find to find the Canvas. Verify EventSystem, HudController, GameOverController
     are reachable. Check serialized fields on HudController (scoreText, roundText, highScoreText)
     and GameOverController (retryButton, mainMenuButton) are non-null.
  i. Use gameobject-find to find Main Camera. Verify PhysicsRaycaster component is present.

STEP 4 — Build settings check
  Use assets-get-data on ProjectSettings/EditorBuildSettings.asset. Verify:
  - Scene index 0 = Bootstrapper
  - Scene index 1 = MainMenu
  - Scene index 2 = Gameplay
  - All three scenes are enabled (not disabled)

STEP 5 — Console check
  Use console-get-logs to check for any error or exception logs related to VContainer,
  scene loading, missing references, or MissingReferenceException.

For every check above, record:
  - PASS (matches expected)
  - FAIL (mismatch or missing) — include the exact field name, expected value, and actual value
  - WARN (unexpected but not necessarily broken)

After all checks, produce the final bug report in the format below.
Do NOT edit any scene or asset. Read-only inspection only.

---

BUG REPORT FORMAT:

# Unity Scene Debug Report
**Date:** {today's date}
**Project:** Simon Says (Unity 6, URP)

## Summary
<number of issues found by severity>

## Critical Issues
<each FAIL that would prevent the game from running — missing required serialized ref, wrong scene index, etc.>

## Warnings
<each WARN — unexpected component, extraneous object, minor misconfiguration>

## Passed Checks
<bulleted list of checks that passed — keep it brief, one line each>

## Recommended Fixes
<for each Critical Issue, one concrete fix action>
```

---

## Output

After Agent 3 completes:

1. Determine a filename: `debug-report-YYYY-MM-DD.md` (use today's date).
2. Create the directory `.claude/debug-reports/` if it does not exist.
3. Write the full bug report markdown to `.claude/debug-reports/debug-report-YYYY-MM-DD.md`.
4. Print a one-paragraph summary to the user stating how many critical issues, warnings, and passed checks were found, and the path to the report file.

---

## Notes

- Use MCP tools for all Unity Editor interactions — never use Bash or file reads to inspect `.unity` YAML directly during the debugger phase (the researcher already did that; the debugger must use live Unity data).
- If a scene fails to open (error from scene-open), record that as a Critical Issue and continue with the next scene.
- If the Unity Editor is not running or the MCP connection is unavailable, abort and tell the user to open the project in Unity first.
- Do not modify any scene, asset, or script during this skill. This is a read-only diagnostic.
