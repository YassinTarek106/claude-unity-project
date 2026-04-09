# Conversation: Debug Scenes Skill Creation & Execution

**Date:** 2026-04-09

---

**Q:** Create a new skill for debugging Unity scenes. First create a researcher agent that analyzes the entire codebase. Create another agent to summarize what the researcher agent did, then afterward deploy a debugger agent that checks all scenes in the multi-scene hierarchy starting from the bootstrapper — check for missing references, missing GameObjects, wrongly created and setup scenes. After that summarize the bugs into a single MD file.

**A:** Created the `/debug-scenes` skill and immediately executed it as a three-agent pipeline.

1. **Explored the project** using an Explore agent to understand the full codebase structure:
   - 3 scenes: `Bootstrapper.unity`, `MainMenu.unity`, `Gameplay.unity`
   - VContainer DI hierarchy: RootLifetimeScope → (MainMenuLifetimeScope | GameplayLifetimeScope)
   - 40 C# scripts across Bootstrap, Config, Core, DI, Infrastructure, Services, UI, Views, Tests

2. **Created the skill file** at `.claude/skills/debug-scenes/SKILL.md`:
   - Frontmatter: `name: debug-scenes`, `user_invocable: true`
   - Describes the 3-agent workflow with exact prompts, MCP tool sequences, and output format
   - Registered automatically in the skills list as `/debug-scenes`

3. **Ran Agent 1 — Researcher** (Explore subagent, read-only):
   - Read all LifetimeScope files, Bootstrap entry points, Services, UI controllers, Views, Infrastructure, raw scene YAML files, VContainerSettings.asset, EditorBuildSettings.asset
   - Produced `RESEARCH_RESULT`: full structured reference document mapping every SerializeField, container registration, constructor dependency, and scene hierarchy

4. **Ran Agent 2 — Summarizer** (Explore subagent):
   - Consumed `RESEARCH_RESULT`
   - Produced `VALIDATION_CHECKLIST`: compact table-per-section format covering Expected Scene Hierarchy, VContainer Wiring, Constructor/Inject Dependencies, Cross-Scene DI Rules, and Build Settings

5. **Ran Agent 3 — Debugger** (general-purpose agent with MCP tool access):
   - Used MCP tools: `scene-open`, `scene-get-data`, `gameobject-find`, `gameobject-component-list-all`, `gameobject-component-get`, `assets-get-data`, `console-get-logs`
   - Inspected all 3 scenes live against the checklist
   - Performed 50 tool calls across Bootstrapper, MainMenu, Gameplay, Build Settings, and Console checks

6. **Wrote bug report** to `.claude/debug-reports/debug-report-2026-04-09.md`

**Results:**
- **3 Critical Issues:**
  1. MainMenu scene: Missing EventSystem — StartButton clicks won't register
  2. Gameplay scene: Missing EventSystem — HUD and GameOver buttons non-interactive
  3. Gameplay scene: `PanelRegistry` is a child of `[Panels]` instead of a root-level GameObject
- **1 Warning:** `[Infrastructure]` is an empty parent; CoroutineRunner and AudioService are on child GOs (no functional impact)
- **29 Passed Checks:** All VContainer SerializeFields wired, all panel components correct, build settings valid, no console errors

**Recommended fixes documented:**
- Add EventSystem to MainMenu and Gameplay scenes via Hierarchy → UI → Event System
- Move `[Panels]/PanelRegistry` to root level and verify inspector reference still holds
- Note: Bootstrapper already has an EventSystem; long-term fix is to remove it from Bootstrapper and keep one per UI scene, or make it DontDestroyOnLoad in root scope
