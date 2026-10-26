# Desi Merge — Coding Standards

## General

- C# targeting Unity 6 (.NET Standard 2.1 API compatibility). Keep code readable over clever.
- All game code lives under `Assets/_Project/` in namespace `DesiMerge.<Area>` matching the folder
  (`DesiMerge.Core`, `DesiMerge.Persistence`, …). Editor code: `DesiMerge.Editor`. Tests: `DesiMerge.Tests.EditMode` / `.PlayMode`.
- One public type per file; file name matches the type. Small closely related enums may share a file (e.g. `AudioCue.cs`).
- Prefer `sealed` classes unless designed for inheritance.

## Naming

| Element | Style | Example |
|---|---|---|
| Types, methods, properties, events, constants | PascalCase | `PlayerProgressService`, `TrySpendCoins`, `SaveKey.Settings` |
| Interfaces | `I` + PascalCase | `IRewardedAdService` |
| Private fields | `_camelCase` | `_repository` |
| Serialized private fields | camelCase with `[SerializeField]` | `targetFrameRate` |
| Serializable data fields (JsonUtility) | public camelCase | `highScore` |
| Static readonly fields | PascalCase | `AllowedTransitions` |
| Locals, parameters | camelCase | `nextState` |
| Methods that may fail without exception | `Try` prefix returning `bool` | `TryTransitionTo` |

## Architecture rules

1. **No global lookups.** Don't use `FindObjectOfType`/`FindFirstObjectByType`/`FindAnyObjectByType`, `GameObject.Find`,
   or static `Instance` singletons for dependencies. Use serialized references within a scene, and receive services
   through `ISceneEntryPoint.Initialize(GameServices)` or constructors.
2. **Logic in plain C#.** MonoBehaviours are thin adapters (input, rendering, Unity callbacks). Rules, state and math go
   in plain classes so they can be EditMode-tested.
3. **Concrete choices in one place.** Only `GameServicesFactory` picks implementations.
4. **No networking or SDKs** in Phase 1–2. Anything that might later talk to a network goes behind an existing interface.
5. **Data vs. config.** Player data → `Persistence` via `ISaveStore`. Designer tuning → ScriptableObjects in
   `Configuration` (read-only at runtime; never write to a ScriptableObject at runtime).
6. **No new packages** without noting the reason in `Architecture.md`.

## Unity specifics

- Don't use services in `Awake`; wait for `Initialize`/`Start`.
- Cache component references; avoid `GetComponent` in `Update`.
- Avoid per-frame allocations in gameplay loops (LINQ, string concatenation, closures, `foreach` over non-struct enumerators).
  LINQ is fine in editor code and one-off setup.
- Use `CompareTag`, not `tag ==`.
- Physics in `FixedUpdate`; input/visuals in `Update`.
- Unsubscribe events in `OnDisable`/`OnDestroy` symmetrical to where you subscribed.
- **Never use `?.` or `??` on `UnityEngine.Object` references** (components, assets, serialized fields). Unity's
  "fake null" objects (destroyed objects, unassigned serialized fields and missing `GetComponent` results in the
  Editor) bypass C# null operators. Use `!= null`, `TryGetComponent`, or an explicit `if`. `?.` is fine on plain C#
  objects (services, delegates).
- Animate visual-only transforms. Never scale or move the transform that owns a `Collider2D`/`Rigidbody2D` for effects.
- Animation components run `Update` only while animating (enable on start, disable when done). Use unscaled time for
  anything that must work on the pause screen.
- UI presents and raises requests; it never decides rules. Gameplay-facing UI goes through an interface
  (`IGameplayView`) and is driven by events/calls, never by polling score or state.
- Keep every `Time.timeScale` change in one owner (`GameplayLoop`) and restore it on every exit path.
- Generated content (scenes, prefabs, placeholder art/audio) is produced by `Editor/Content`; change the generator
  (or `UITheme`) rather than hand-editing generated scenes, or document the manual edit.
- Always commit `.meta` files with their assets; never hand-edit GUIDs.
- Never move or rename assets outside the Unity Editor unless the `.meta` file moves with them.

## Error handling

- Validate public inputs; throw `ArgumentException`/`ArgumentOutOfRangeException` for programmer errors.
- Recoverable runtime conditions (corrupt save, missing ad) must not crash the game: log a warning and fall back.
- Log with a bracketed area prefix: `Debug.LogWarning("[Save] ...")`.

## Formatting

- 4 spaces, Allman braces, always use braces for `if`/`for`/`foreach`.
- `var` when the type is obvious from the right-hand side.
- XML `<summary>` on public types and on members whose behaviour isn't obvious from the name. No comments that restate code.
- Expression-bodied members for trivial one-liners only.

## Tests

- Test class per production class/area: `<Subject>Tests`. Test method names describe behaviour:
  `TrySpendCoins_FailsWhenBalanceTooLow`.
- Arrange / Act / Assert, separated by blank lines.
- Tests never touch real save data: use `InMemorySaveStore` or a unique temp directory.
