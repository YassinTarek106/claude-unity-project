# Conversation: Architecture Rules — CLAUDE.md

**Date:** 2026-04-09

---

**Q:** Add the following architecture rules to CLAUDE.md as non-negotiable project rules:

- **VContainer DI** — all services injected via VContainer as interfaces; ban `FindObjectOfType`, singletons, static service access, service locator, and `new Service()` in MonoBehaviours
- **Custom Event System** — build from scratch; ban `UnityEvent`/`UnityAction` for cross-service communication, SO event channels, and third-party libraries (UniRx, MessagePipe, etc.); require typed events, proper subscribe/unsubscribe cleanup, and decoupled publishers/subscribers
- **Audio Service** — centralized via DI; interface-based (e.g. `IAudioService`); sounds mapped by enum; supports overlapping sounds; no raw `AudioSource.Play()` in gameplay code
- **UI System** — reactive and event-driven; UI controllers subscribe to events; no scattered `SetActive` calls; each screen/panel is a separate controller injected via VContainer
- **Clean Code** — no magic numbers, no God classes (max ~150 lines), interface segregation, no `#region`, no commented-out code, consistent naming conventions

**A:**
1. Read existing `CLAUDE.md` to understand current structure
2. Appended a new `## Architecture Rules` section at the bottom of `CLAUDE.md` covering all five rules:
   - VContainer Dependency Injection (with banned patterns listed)
   - Custom Event System (banned libs + required design constraints)
   - Audio Service (interface-based, enum-mapped, overlapping support)
   - UI System (event-driven, per-panel controllers, VContainer-injected)
   - Clean Code (no magic numbers, no God classes, no `#region`, no commented-out code, naming conventions)
3. File modified: `CLAUDE.md`
