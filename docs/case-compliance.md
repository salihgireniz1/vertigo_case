# Case Compliance and Plan

This file maps every requirement in the Vertigo Games "Game Developer Demo 2026" brief to where it is
satisfied in this repository, and records how the week was planned. The intent is that a reviewer can
verify each line without asking. Architecture and reasoning are in [design.md](design.md).

## 1. Gameplay rules from the brief

| Brief | Status | Where |
|---|---|---|
| Spin a wheel instead of picking cards | Done | `WheelView`, `GameFlow.SpinAsync` |
| Slice content of each wheel editable from the editor | Done | `Assets/Game/Data/Wheels/wheel_{bronze,silver,golden}.asset` (`WheelTierConfig`: reward, base amount, weight, bomb per slice). `OnValidate` rejects an invalid wheel. |
| Every zone: multiple rewards and the bomb | Done | `WheelDefinition.Validate()` enforces exactly one bomb on bronze. |
| The bomb takes all rewards collected so far | Done | Bomb puts the run in `GameState.Bombed`; give up (`TryRestart`) clears the inventory. Revive (bonus) keeps it, see the decisions log in design.md. |
| Rewards get better every zone | Done | `ZoneRules.ScaleAmount`, `growthPercentPerZone` in `zone_progression.asset`. |
| Every 5th zone: risk-free silver spin, no bomb | Done | `ZoneRules.GetTier`, `safeZoneInterval = 5`, silver wheel validated bomb-free. |
| Every 30th zone: risk-free golden spin, special rewards, no bomb | Done | `superZoneInterval = 30`, golden wheel with its own reward set. |
| Leave only while the wheel is not spinning and the zone is safe or super | Done | `GameSession.CanLeave` (state Idle and safe zone) plus `RunStore.CanLeave` (and not busy) drive the EXIT button. |
| On a bomb all rewards are lost and the game can be restarted | Done | Bomb popup: GIVE UP calls `RestartAsync`. |
| Continue system with currency (bonus) | Done | REVIVE pays `reviveCost` gold, keeps the rewards, replays the zone with the bomb chamber disabled and marked X. |
| Walk away with everything collected before spinning | Done | Leave popup lists the loot; gold stacks are credited to the wallet on leave. |

## 2. Deliverables

| Brief | Status | Where |
|---|---|---|
| Video of the game working | Done | `gameplay.mp4` attached to the latest GitHub Release (scripted run: zones 1-10, exit, play again, bomb, revive, bomb, give up). |
| Screenshots at 20:9, 16:9, 4:3 | Done | `docs/screenshots/screenshot_20x9_2400x1080.png`, `screenshot_16x9_1920x1080.png`, `screenshot_4x3_1440x1080.png`. |
| GitHub project with APK as a Release | Done | `WheelOfFortune.apk` on the latest release of `salihgireniz1/vertigo_case`. |
| Unity 6000.3.23f1 | Done | `ProjectSettings/ProjectVersion.txt`. |

## 3. Review criteria

| Criterion | How it is met |
|---|---|
| Proper working APK as a GitHub release | Android IL2CPP build from the tagged commit; release notes list what changed. |
| Reusable, maintainable, scalable, easily testable codebase | Rules in a Unity-free library with 51 NUnit tests; client flow tested with fake views and a fake authority (9 EditMode tests); views are thin MonoBehaviours behind interfaces; presenters are plain classes. |
| SOLID / OOP | Single responsibility per class (session, resolver, zone rules, store, flow, presenters, views). Open for extension through `IRunService` (local / remote), `IRandom`, view interfaces. Dependencies point inward; composition in one root (`GameBootstrap`). |
| Visually acceptable, lean, reusable UI hierarchy for 20:9, 16:9, 4:3 | One canvas prefab, `ui_panel_safe_area` under it, panels anchored to corners / centre, lists built from `ui_cell_zone` and `ui_item_reward` prefabs, wheel slices from `ui_slice_wheel`. Screenshots at all three ratios in `docs/screenshots`. |
| Proper use of ScriptableObjects | Rewards, wheels, progression and game config are assets under `Assets/Game/Data`; code converts them to immutable rule definitions once at startup. |
| Proper use of DOTween | Wheel spin, slice punch, bomb shake, popup show / hide, zone strip slide, reward item pop. Tweens are linked to their GameObject (`SetLink`) or killed on hide; async ones are awaited with UniTask. |
| Proper use of Sprite Atlas | `Assets/Game/Sprites/ui_atlas_game.spriteatlasv2` packs the `Assets/Game/Sprites/UI` folder (Sprite Atlas V2, included in build). |
| Well-designed prefab and folder structure | `Assets/Game/{Data,Prefabs/UI,Scenes,Scripts/{Data,Runtime,Tests},Sprites}`; one prefab per panel / popup / list item / slice, nested under `ui_canvas_game`. Third-party packages stay under their own roots. |
| Polished final output matching the reference mechanic | Revolver base per tier, indicator, counter-rotated upright icons, win punch, bomb shake with haptics, animated popups, sliding zone strip, disabled bomb chamber after revive. |

## 4. UI technical details

| Rule | Status | Where / how to verify |
|---|---|---|
| Canvas mode Expand | Done | `ui_canvas_game.prefab`: `CanvasScaler` Scale With Screen Size, reference 1920x1080, Screen Match Mode **Expand** (`m_ScreenMatchMode: 1`). |
| Use TextMeshPro | Done | Every text is `TextMeshProUGUI`; `grep -l "UnityEngine.UI.Text" Assets/Game/Prefabs/UI/*.prefab` returns nothing. |
| Changeable elements end with `_value` | Done | `ui_text_gold_value`, `ui_text_wheel_title_value`, `ui_image_wheel_base_value`, `ui_image_slice_icon_value`, `ui_text_slice_amount_value`, `ui_text_zone_number_value`, `ui_text_reward_amount_value`, and the rest. Static labels (`ui_text_exit_label`, popup titles) deliberately do not. |
| Names from general to specific | Done | `ui_<type>_<group>_<detail>[_value]`, e.g. `ui_image_wheel_indicator_value`, `ui_button_spin`, `ui_panel_zone_strip`, `ui_cell_zone`. |
| No RaycastTarget / Maskable on unnecessary images | Done | Raycast only on the five buttons and the two popup dim images; Maskable only on scroll-list items (`ui_cell_zone`, `ui_item_reward`). Verify: `grep -c "m_RaycastTarget: 1" Assets/Game/Prefabs/UI/*.prefab`. |
| UI animators not on the root transform | Done | No Animator components at all. Tweens target child pivots: `ui_pivot_wheel`, `ui_pivot_wheel_shake`, `ui_pivot_slice_content`, popup pivots and cards. Panel roots are never animated. |
| Anchors and pivots correct for every aspect | Done | HUD corners anchored to corners, zone strip to top-centre, wheel panel to centre, lists stretch. Screenshots at 20:9 / 16:9 / 4:3 show no overlap or clipping. |
| Button references set from OnValidate | Done | `WheelView`, `HudView`, `BombPopupView`, `LeavePopupView`, `SliceView`, `GameBootstrap` resolve every reference in `OnValidate` through `UiRefs.Find<T>(root, name)`. |
| Sliced sprites in Image components | Done | Frames, panels, badges and buttons use `Image.Type.Sliced` with the provided 9-slice sprites; icons and the wheel art use Simple with `preserveAspect`. |
| No OnClick / event references from the editor | Done | No persistent `onClick` calls in any prefab; buttons are bound in code through R3 `OnClickAsObservable` in `GameFlow`. Verify: `grep -A3 "m_OnClick" Assets/Game/Prefabs/UI/*.prefab` shows empty `m_Calls`. |
| Do not stretch images | Done | Every `Image` with sprite art is either Sliced or Simple with `preserveAspect`; the wheel base, indicator, icons and popup art keep their ratio at every aspect. |

## 5. Plan and timeline

| Day | Work |
|---|---|
| 2026-10-09 | Brief analysed, Critical Strike card game studied. Design spec. Rules extracted into `Shared/Wheel.Rules` with tests. Unity project on 6000.3.23f1, third-party packages (DOTween Pro, UniTask, R3, Feel). Data assets, UI prefabs, views, presenters, flow. First APK, video, screenshots, release v1.0.0. CI for the shared library. |
| 2026-10-10 | CI maintenance (Node 24 actions). |
| 2026-10-12 | Wheel slices centred on the chamber art (ring radius measured from the sprites), slice content moved to layout components so every reward icon fits. Revive rule corrected: the bomb no longer wipes rewards before the player decides. Scratch files and personal editor tools removed from the repository. Docs rewritten. Release v1.2.0 with a new APK and video. |

## 6. How to verify locally

```bash
dotnet test Shared/Wheel.Rules.Tests
```

That runs the 51 rules tests. In Unity: open `Assets/Game/Scenes/Game.unity`, then Window > General >
Test Runner > EditMode > Run All for the 9 `GameFlow` tests. Play mode: spin, hit a bomb, revive, exit
in zone 5.

Android: Build Profiles > Android > Build, or install the attached APK.
