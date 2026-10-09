# Wheel of Fortune

Unity 6000.3.23f1 demo for the Vertigo Games Game Developer case. Critical Strike's "Card game" rules, played on a spinning revolver wheel.

## Rules

- Every zone you spin an eight-slice wheel. Seven slices hold rewards, one holds the bomb.
- Rewards grow every zone: `amount = base * (1 + growth * (zone - 1))`.
- Every 5th zone is a **safe zone** (silver wheel, no bomb). Every 30th is a **super zone** (golden wheel, special rewards, no bomb).
- You can **exit** with everything you collected only while the wheel is idle in a safe or super zone.
- Hitting the bomb wipes the run. You can **give up** or **revive** with gold and continue from the next zone.

All numbers above live in ScriptableObjects under `Assets/Game/Data` and can be changed in the inspector.

## Project layout

```
Assets/Game
  Data/           ScriptableObject instances: Rewards, Wheels (bronze/silver/golden), Config
  Prefabs/UI/     ui_canvas_game and its nested panel, popup, slice, cell and item prefabs
  Scenes/         Game.unity (single scene)
  Scripts/
    Core/         Game.Core  — model + ScriptableObject definitions, no MonoBehaviours
    Runtime/      Game.Runtime — views, presenters, flow, composition root
    Tests/        Game.Tests.EditMode — NUnit tests for the core model
  Sprites/        Provided UI sprites + ui_atlas_game (Sprite Atlas V2); VFX sprites stay loose
```

## Architecture

**Model (`Game.Core`)** is plain C# with no scene dependencies.
`GameSession` is the state machine (`Idle → Spinning → Idle | Bombed`, `Idle → Left`, `Bombed → Idle` on revive) and guards every transition.
`SpinResolver` picks a slice by weight through an `IRandom` seam, `ZoneRules` answers tier/safe/scaling questions, `RewardInventory` stacks rewards per type.
State is exposed as R3 `ReadOnlyReactiveProperty`s.

**Data** is authored in ScriptableObjects: `RewardDefinition` (icon + name), `WheelTierConfig` (sprites, copy, eight `WheelSliceData` entries with reward, base amount, weight, bomb flag), `ZoneProgressionConfig` (intervals, growth, wheel per tier) and `GameConfig` (progression, starting gold, revive cost).

**Presentation (`Game.Runtime`)** follows MVP:

- *Views* are thin MonoBehaviours that hold references and play tweens. Every reference is resolved by child name in `OnValidate`, nothing is wired by hand and no `OnClick` is set in the inspector.
- *Presenters* subscribe to the session and push into views (`WheelPresenter`, `HudPresenter`, `ZoneStripPresenter`, `RewardsPresenter`).
- *`GameFlow`* turns button streams into session calls and sequences the async parts with UniTask: spin → `WheelView.SpinToAsync` → apply result → popup.
- *`GameBootstrap`* is the composition root: it builds the session, presenters and flow from `GameConfig` and the scene views, and disposes them on destroy.

Third-party: R3 (reactive bindings), UniTask (async flow), DOTween (wheel, popups, punches), Feel (glow flashes, scale pops, haptics via Nice Vibrations).

## UI rules from the brief

- Canvas Scaler 1920x1080, match mode **Expand**; `SafeAreaFitter` keeps the HUD inside the device safe area.
- TextMeshPro everywhere. Elements whose content changes end with `_value`; names go general → specific (`ui_image_wheel_base_value`).
- Button and text references come from `OnValidate`; clicks are observed in code (`OnClickAsObservable`).
- `RaycastTarget` and `Maskable` are off except on buttons, the popup dim and items inside `RectMask2D`.
- Frames, panels and buttons use **sliced** sprites with borders set in the importer; icons use `preserveAspect`, nothing is stretched.
- Animated transforms are children (`ui_pivot_wheel`, `ui_pivot_popup`, `ui_pivot_slice_content`), prefab roots stay still.

## Tests

`Window > General > Test Runner > EditMode` runs 46 tests over `ZoneRules`, `SpinResolver` and `GameSession` (transition guards, bomb wipe, leave rules, revive cost, scaling).

## Screenshots

| 20:9 | 16:9 | 4:3 |
|---|---|---|
| ![20:9](docs/screenshots/screenshot_20x9_2400x1080.png) | ![16:9](docs/screenshots/screenshot_16x9_1920x1080.png) | ![4:3](docs/screenshots/screenshot_4x3_1440x1080.png) |

## Build

Android, IL2CPP, ARMv7 + ARM64, landscape only. The APK and a gameplay video are attached to the GitHub release.

## Not included

Save/load, DI container, localization, audio (no audio assets were provided).
