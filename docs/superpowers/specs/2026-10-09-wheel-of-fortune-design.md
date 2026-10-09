# Wheel of Fortune — Design Spec

> Initial design, written before implementation. The shipped architecture moved the model into the shared
> `Shared/Wheel.Rules` library behind `IRunService` (server authority) and changed revive to replay the same zone
> with the bomb disabled; see the README for the current state.

Vertigo Games Game Developer demo. Critical Strike "Card game" rules, spinning wheel instead of cards.

## Goal

Playable landscape mobile mini-game. Every zone the player spins an 8-slice wheel. Normal zones (bronze) have one bomb slice; hitting it wipes every collected reward. Every 5th zone is a safe silver spin and every 30th zone is a super golden spin, both bomb-free. The player may leave with the loot only while the wheel is idle in a safe or super zone. Revive with gold after a bomb is a bonus feature.

Deliverables: GitHub repo with APK as a Release, gameplay video, screenshots at 20:9, 16:9, 4:3.

## Constraints (from the brief)

- Unity 6000.3.23f1, provided UI sprites only, Sprite Atlas, DOTween, ScriptableObjects, TextMeshPro.
- Canvas Scaler match mode Expand. Correct anchors/pivots for every aspect.
- Naming general-to-specific (`ui_image_spin_silver`); elements whose content changes at runtime end with `_value`.
- Button references resolved in `OnValidate`, never wired through the inspector `OnClick`.
- No RaycastTarget / Maskable on non-interactive images. Sliced sprites for frames/buttons. No stretched images.
- Animators live on child transforms, never on a prefab root.

## Architecture

MVP with R3 reactive bindings, UniTask for the async spin flow, DOTween for tweens, Feel (MMF_Player) for juice/haptics.

Three assemblies under `Assets/Game/Scripts`:

| Assembly | Contents | References |
|---|---|---|
| `Game.Core` | ScriptableObject definitions, pure model (`GameSession`, `SpinResolver`, `ZoneRules`, `RewardInventory`), no MonoBehaviours | R3 |
| `Game.Runtime` | Views, presenters, `GameFlow`, `GameBootstrap` composition root | Core, R3, R3.Unity, UniTask, UniTask.DOTween, DOTween.Modules, MoreMountains.Feedbacks, Unity.TextMeshPro, UnityEngine.UI |
| `Game.Tests.EditMode` | NUnit tests for the Core model | Core, R3 |

### Data (ScriptableObjects)

- `RewardDefinition`: id, displayName, icon.
- `WheelTierConfig`: tier (Bronze/Silver/Golden), base sprite, indicator sprite, eight `WheelSliceData` entries (reward, baseAmount, weight, isBomb). `OnValidate` warns on wrong slice count or illegal bombs.
- `ZoneProgressionConfig`: safeZoneInterval (5), superZoneInterval (30), growthPerZone, tier config per tier. Amount scaling is linear: `base * (1 + growth * (zone - 1))`.
- `GameConfig`: progression, startingGold, reviveCost.

### Core model

- `GameState`: Idle, Spinning, Bombed, Left. `GameSession` owns the transitions and guards them.
- `GameSession` exposes `ReactiveProperty` Zone, State, Gold and a `RewardInventory`. API: `StartSpin(IRandom)` returns a `SpinResult`, `ApplySpinResult`, `Leave`, `Revive`, `Restart`.
- `SpinResolver.Pick(slices, random)`: weighted random slice index.
- `ZoneRules`: tier for a zone, safe check, amount scaling.
- `IRandom` abstracts randomness so tests are deterministic.

Revive deducts gold, keeps rewards, advances one zone, returns to Idle.

### Presentation

- `GameBootstrap` (scene root) builds `GameSession`, presenters and `GameFlow` from `GameConfig`.
- `GameFlow` runs the async loop: spin requested → `StartSpin` → `WheelView.SpinToAsync(index)` → `ApplySpinResult` → bomb popup or next zone.
- Presenters (`WheelPresenter`, `HudPresenter`, `RewardsPresenter`, `PopupPresenter`) subscribe to session streams and push to views. Views only hold references and play tweens.
- Views: `WheelView` + `SliceView`, `ZoneStripView` + `ZoneCellView`, `RewardListView` + `RewardItemView`, `HudView`, `BombPopupView`, `LeavePopupView`.
- Layout mirrors the reference screenshot: zone strip top-center, loot panel + exit button left, wheel center, safe/super zone counters top-right.

### Juice

DOTween: wheel spin with ease-out overshoot, slice highlight punch, reward fly-to-panel. MMF_Player: bomb shake + haptic, win burst with `star_glow_alpha` / `star_flash_alpha` particles.

## Testing

EditMode tests cover `ZoneRules`, `SpinResolver` (weight distribution with a seeded random), and `GameSession` transitions (leave only in safe zones while idle, bomb wipes inventory, revive cost). Views are verified manually at the three aspect ratios.

## Out of scope

Save/load, DI container, localization, audio (no audio assets provided), IAP.
