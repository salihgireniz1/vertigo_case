# Wheel of Fortune

Unity 6000.3.23f1 demo for the Vertigo Games Game Developer case. Critical Strike's "Card game" rules, played on a spinning revolver wheel, built the way an online feature has to be built: the rules live in a server-ready library and the client only renders what the authority decides.

## Rules

- Every zone you spin an eight-slice wheel. Seven slices hold rewards, one holds the bomb.
- Rewards grow every zone: `amount = base * (100 + growth% * (zone - 1)) / 100` (integer math, so client and server agree bit for bit).
- Every 5th zone is a **safe zone** (silver wheel, no bomb). Every 30th is a **super zone** (golden wheel, special rewards, no bomb).
- You can **exit** with everything you collected only while the wheel is idle in a safe or super zone. Gold collected on the wheel is credited to your wallet.
- Hitting the bomb wipes the run. **Give up** restarts, **revive** pays gold and replays the same zone with the bomb chamber disabled.

All numbers live in ScriptableObjects under `Assets/Game/Data` and can be changed in the inspector.

## Repository layout

```
Shared/
  Wheel.Rules/          pure C# netstandard2.1 class library: the rules (no Unity dependency)
  Wheel.Rules.Tests/    NUnit tests, `dotnet test`
  Wheel.Rules.Unity/    UPM package carrying the compiled Wheel.Rules.dll (+ pdb)
Assets/Game/
  Data/                 ScriptableObject instances: Rewards, Wheels, Config
  Prefabs/UI/           ui_canvas_game and its nested panel, popup, slice, cell and item prefabs
  Scenes/Game.unity
  Scripts/
    Data/               Game.Data: ScriptableObject definitions, convert to Wheel.Rules definitions
    Runtime/            Game.Runtime: services, store, views, presenters, flow, composition root
    Tests/EditMode/     Game.Tests.EditMode: GameFlow driven through fakes
  Sprites/              provided UI sprites + ui_atlas_game (Sprite Atlas V2)
.github/workflows/      dotnet test + pack of the shared library
```

## Architecture

```
          Shared/Wheel.Rules (netstandard2.1)             Unity client
 ┌──────────────────────────────────────────┐   ┌────────────────────────────────────────┐
 │ GameSession  SpinResolver  ZoneRules     │   │ Game.Data   SO configs → definitions   │
 │ WheelDefinition  RunSnapshot  SpinOutcome│◄──│ Game.Runtime                           │
 │ IRandom (server owns the real one)       │   │   IRunService ─ Local | Remote         │
 └──────────────────────────────────────────┘   │   RunStore (R3) ← snapshots            │
                 ▲                              │   Presenters → Views (MVP)              │
          game server (same DLL via NuGet)      │   GameFlow: intents + animation         │
                                                └────────────────────────────────────────┘
```

**Server authority.** The client never computes a spin. It sends an intent (`spin`, `leave`, `revive`, `restart`) through `IRunService` and gets back a `SpinOutcome` / `RunSnapshot` from the shared DTOs. The wheel animates to the slice it was told, then the snapshot is applied to `RunStore` and every view re-renders from it. `CanSpin`, `CanLeave`, `CanRevive` arrive in the snapshot; the client shows what the authority allows.

- `LocalRunService` runs `Wheel.Rules.GameSession` in-process. That is what the demo ships.
- `RemoteRunService` is the HTTP transport for a backend that runs the same `GameSession` (a Unity Cloud Code C# module or any .NET service referencing the NuGet). Not wired in the demo, no backend is deployed. Each request carries a sequence number so a retry is idempotent.

**Shared library.** `Shared/Wheel.Rules` is built with `dotnet build -c Release`; a post-build step copies the DLL into `Shared/Wheel.Rules.Unity`, which the project consumes as a local UPM package (`Packages/manifest.json`: `file:../Shared/Wheel.Rules.Unity`). The server consumes `dotnet pack` output. CI runs the 51 rules tests and packs both on every change under `Shared/`. RNG is behind `IRandom`, so tests script outcomes and the server can later publish a commit-reveal seed for provably fair spins.

**Presentation (`Game.Runtime`)** is MVP:

- *Views* are thin MonoBehaviours behind interfaces (`IWheelView`, `IBombPopupView`, ...). Every reference is resolved by child name in `OnValidate`; no `OnClick` is wired in the inspector, clicks are observed in code.
- *Presenters* subscribe to `RunStore` and push into views.
- *`GameFlow`* turns clicks into service calls and sequences the async parts with UniTask. One intent in flight at a time.
- *`GameBootstrap`* is the composition root. Swapping `LocalRunService` for `RemoteRunService` is the only line that changes for production.

Third-party: R3 (reactive store and bindings), UniTask (async flow), DOTween (wheel, popups, punches), Feel (glow flashes, scale pops, haptics via Nice Vibrations).

## UI rules from the brief

- Canvas Scaler 1920x1080, match mode **Expand**; `SafeAreaFitter` keeps the HUD inside the device safe area.
- TextMeshPro everywhere. Elements whose content changes end with `_value`; names go general → specific (`ui_image_wheel_base_value`).
- Button and text references come from `OnValidate`; clicks are observed in code (`OnClickAsObservable`).
- `RaycastTarget` and `Maskable` are off except on buttons, the popup dim and items inside `RectMask2D`.
- Frames, panels and buttons use **sliced** sprites with borders set in the importer; icons use `preserveAspect`, nothing is stretched.
- Animated transforms are children (`ui_pivot_wheel`, `ui_pivot_popup`, `ui_pivot_slice_content`), prefab roots stay still.

## Tests

- `dotnet test Shared/Wheel.Rules.Tests`: 51 tests over `ZoneRules`, `SpinResolver` and `GameSession` (transition guards, bomb wipe, revive replay without bomb, wallet credit on leave, scaling, validation).
- Unity Test Runner, EditMode: 9 `GameFlow` tests with a fake authority and fake views (busy gate, bomb popup, revive, double tap, exit). Editor only (Window > General > Test Runner); CI has no Unity licence, so it runs only the shared library tests.

## Screenshots

| 20:9 | 16:9 | 4:3 |
|---|---|---|
| ![20:9](docs/screenshots/screenshot_20x9_2400x1080.png) | ![16:9](docs/screenshots/screenshot_16x9_1920x1080.png) | ![4:3](docs/screenshots/screenshot_4x3_1440x1080.png) |

## Build

Android, IL2CPP, ARMv7 + ARM64, landscape only. The APK and a gameplay video are attached to the GitHub release.

## Not included

A deployed backend, save/load, DI container, localization, audio (no audio assets were provided).
