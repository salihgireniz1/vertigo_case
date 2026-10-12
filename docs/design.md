# Wheel of Fortune — Design and Architecture

Vertigo Games "Game Developer Demo 2026". Critical Strike's *Card game* rules, played on a spinning
eight-chamber revolver instead of cards. This document explains what was built, why it is layered the
way it is, and how the same code would run behind a game server. The first-day spec (2026-10-09) was
replaced by this document on 2026-10-12; the decisions log below records what changed and why.

Related: [README](../README.md) for setup, layout and build; [case-compliance.md](case-compliance.md)
for the brief's rules and where each one is satisfied.

## 1. Goal

A landscape mobile mini-game. Every zone the player spins an eight-slice wheel. Seven slices hold
rewards that grow with the zone number, one holds the bomb. Every 5th zone is a bomb-free silver
*safe zone*, every 30th a bomb-free golden *super zone* with special rewards. The player may walk away
with the loot only while the wheel is idle in a safe or super zone. Hitting the bomb stops the run:
give up restarts from zone 1 with nothing, revive (bonus feature) pays gold, keeps the loot and replays
the same zone with the bomb chamber disabled.

Deliverables: GitHub repository, APK attached as a GitHub Release, gameplay video, screenshots at
20:9, 16:9 and 4:3.

## 2. Decisions log

| Date | Decision | Why |
|---|---|---|
| 10-09 | Rules live in a pure C# library (`Shared/Wheel.Rules`, netstandard2.1) with no Unity dependency. The Unity client consumes the compiled DLL through a local UPM package; a server would consume the NuGet package from the same build. | The game is gambling: outcomes must be decided by an authority the player cannot patch. One library means one implementation of the rules, tested once, bit-identical on both sides. |
| 10-09 | The client talks to the rules only through `IRunService` (async, cancellable). `LocalRunService` runs the rules in-process for the demo; `RemoteRunService` posts the same intents to HTTP endpoints. | The client is a renderer. Swapping the transport is one line in `GameBootstrap`; nothing else in the client knows where outcomes come from. |
| 10-09 | Revive replays the **same** zone with the bomb chamber disabled (greyed, marked X), like Critical Strike. Not zone + 1. | Matches the reference game; a paid revive that skips a zone would be a hidden reward. |
| 10-09 | Gold won on the wheel is an ordinary reward stack. It is credited to the wallet only when the player leaves. | The brief's rule: the bomb takes *all* rewards collected so far. Gold that went straight to the wallet would be bomb-proof. |
| 10-09 | Reward scaling uses integer math: `amount = base * (100 + growth% * (zone - 1)) / 100`, rounded half up. | A float formula can differ by one unit between platforms; client and server must agree exactly. |
| 10-12 | Hitting the bomb no longer clears the rewards on the spot. The bomb puts the run in `Bombed`; revive keeps everything, give up (restart) wipes it. | A revive that only kept the zone number was worth almost nothing; keeping the rewards is the whole point of paying for it. Rules tests updated. |
| 10-12 | Wheel slice content is laid out with Unity layout components (`VerticalLayoutGroup` + `LayoutElement` + `Image.preserveAspect`), not code-computed rects. Slice rects equal the chamber circles; the ring radius is measured from the base sprites. | Native layout is inspectable and tweakable in the editor; a code fit was tried and removed. |
| 10-12 | An unreachable authority is a first-class state: `RunServiceUnavailableException`, a "CONNECTION LOST" popup with RETRY, the same intent resent with the same sequence number, 10 s request timeout. | The client is a renderer of server answers; it must survive not getting one without unlocking the UI or rolling twice. |

## 3. Layers

```
                 Shared/Wheel.Rules  (netstandard2.1, no Unity)          <- rules, DTOs, tests
                           |  dotnet build -c Release copies the DLL
                           v
            Shared/Wheel.Rules.Unity  (local UPM package: DLL + pdb)
                           |
   Assets/Game/Scripts/Data  (Game.Data)     ScriptableObjects -> rule definitions
                           |
   Assets/Game/Scripts/Runtime  (Game.Runtime)
      Services   IRunService, LocalRunService, RemoteRunService
      State      RunStore (last snapshot + busy flag, R3 reactive properties)
      Flow       GameFlow (intents -> service -> animation -> snapshot)
      Presenters Wheel / Hud / ZoneStrip / Rewards (store -> views)
      Views      MonoBehaviours: WheelView, SliceView, popups, lists (tweens only)
      GameBootstrap  composition root
                           |
   Tests   Shared/Wheel.Rules.Tests (NUnit, dotnet)   Assets/Game/Scripts/Tests/EditMode (GameFlow with fakes)
```

Dependency direction is strictly downward. `Wheel.Rules` knows nothing about Unity, `Game.Data` knows
nothing about views, views know nothing about the rules. `Game.Runtime` references `Game.Data`, R3,
UniTask, DOTween, Feel, TextMeshPro and uGUI; nothing references `Game.Runtime` except the tests.

### 3.1 Shared/Wheel.Rules

| Type | Role |
|---|---|
| `GameSession` | One player's run: zone, state, gold, inventory, `bombDisabledZone`. Owns and guards every transition: `Spin`, `TryLeave`, `TryRevive`, `TryRestart`. `Snapshot()` serialises the whole run. |
| `GameState` | `Idle -> Idle` (reward) or `Bombed` (bomb); `Idle -> Left` (safe zone only); `Bombed -> Idle` (revive, same zone); `Bombed` or `Left -> Idle` (restart). |
| `SpinResolver` | Weighted random pick over the slices, optionally excluding bombs. Float-drift guard on the last positive weight. |
| `ZoneRules` | Tier for a zone, safe check, integer amount scaling, next safe / super zone. |
| `WheelDefinition`, `SliceDefinition` | Wheel content, with `Validate()`: exactly 8 slices, bronze has exactly one bomb, silver and golden have none, every reward slice is hittable. |
| `RewardInventory` | Reward stacks in pickup order. |
| `GameRules`, `ProgressionRules` | Immutable rule numbers (intervals, growth, starting gold, revive cost, wallet reward id). |
| `IRandom` | The roll source. The server owns the real one; tests script it. |
| `RunSnapshot`, `SpinOutcome`, `RewardStackDto` | Wire types. Public fields, `[Serializable]`, no Unity types, so `JsonUtility` and `System.Text.Json` both handle them without attributes. |

Everything the client needs to render is in the snapshot, including what it is allowed to do
(`CanSpin`, `CanLeave`, `CanRevive`). The client never derives permissions itself.

### 3.2 Game.Data (ScriptableObjects)

| Asset | Content |
|---|---|
| `RewardDefinition` x30 | id, display name, icon sprite. |
| `WheelTierConfig` x3 (bronze, silver, golden) | base sprite, indicator sprite, eight `WheelSliceData` (reward, base amount, weight, is bomb). Editable in the inspector, as the brief requires. |
| `ZoneProgressionConfig` | safe / super intervals, growth percent, the three wheels. `Validate()` runs the shared `WheelDefinition.Validate()` and `OnValidate` logs problems while editing. |
| `GameConfig` | progression, starting gold, revive cost, wallet reward. `Rules` converts to the shared `GameRules`. |

`RewardCatalog` maps reward ids back to sprites for the views. The conversion to shared definitions
happens once, in the `LocalRunService` constructor.

### 3.3 Game.Runtime

* **Services.** `IRunService` exposes five intents, all `UniTask` and all taking a `CancellationToken`:
  `StartAsync`, `SpinAsync`, `LeaveAsync`, `ReviveAsync`, `RestartAsync`. `LocalRunService` wraps a
  `GameSession`; `RemoteRunService` posts JSON to `run/<intent>` with a bearer token and a per-request
  sequence number.
* **State.** `RunStore` holds the last `RunSnapshot` and a `Busy` flag as R3 reactive properties and
  derives `CanSpin` / `CanLeave` observables for the buttons.
* **Flow.** `GameFlow` subscribes to the button observables, gates every intent (`TryBegin`: not busy
  and the last snapshot allows it), awaits the service, runs the animation around the answer, applies
  the snapshot, releases the gate in `finally`.
* **Presenters.** Plain C# classes that subscribe to the store and push values into views. They are the
  only place that knows both the snapshot and the view contracts (`IWheelView`, `IHudView`, ...).
* **Views.** MonoBehaviours with serialized references resolved in `OnValidate` by child name
  (`UiRefs.Find`). They hold DOTween / Feel animations and nothing else.
* **Bootstrap.** `GameBootstrap.Awake` validates config and references, builds the service, store,
  presenters and flow, and ties their lifetime to the GameObject.

## 4. Runtime flow of one spin

```
 SPIN button --R3--> GameFlow.SpinAsync
                        | TryBegin(store.Current.CanSpin)         <- busy gate + authority's permission
                        v
                   IRunService.SpinAsync(ct)  ---------------->  authority (local GameSession or server)
                        |                                          rolls, applies, returns SpinOutcome
                        v
              WheelView.SpinToAsync(outcome.SliceIndex)         <- wheel animates to the decided slice
                        | PlayWin / PlayBomb, HoldAsync (0.6 s)
                        v
                 store.Apply(outcome.Snapshot)                   <- every presenter re-renders
                        | bomb? bombPopup.ShowAsync(cost, canRevive)
                        v
                 finally store.SetBusy(false)
```

The order matters: the outcome is known *before* the wheel starts turning, so the animation is a
presentation of a decision, never the decision itself. The 3.5 s spin plus 0.6 s hold is the only
time the authority's answer is withheld from the store, and the busy gate blocks every other intent
meanwhile. Cancellation flows from the `GameBootstrap` destroy token through the flow into every await,
so leaving the scene mid-spin cancels the request and the tweens.

## 5. Why a shared rules library

1. **Authority.** The brief calls the game gambling. In a real product the server rolls, the client
   renders. `GameSession` is that authority, and the Unity build only ever sees its snapshots.
2. **One implementation.** The same `GameSession` runs in `LocalRunService` for the demo and would run
   inside the server process. There is no "client prediction" copy that can drift.
3. **Exact agreement.** Integer-only scaling, deterministic `SpinResolver`, `IRandom` injected. Two
   machines with the same seed produce the same run; the test suite proves the edges.
4. **Fast tests.** 51 NUnit tests run in well under a second with plain `dotnet test`, in CI on every
   change under `Shared/`, without a Unity licence.
5. **Two consumers from one build.** `dotnet build -c Release` copies the DLL into the UPM package;
   `dotnet pack` produces the NuGet for the server. CI uploads both.

## 6. Server scenario

Nothing server-side is deployed for the demo. This is how the existing pieces fit a server:

| Endpoint | Request | Response | Server work |
|---|---|---|---|
| `POST run/start` | bearer token, sequence | `RunSnapshot` | Load or create the player's `GameSession`; return `Snapshot()`. Also the resume path after a reconnect. |
| `POST run/spin` | bearer token, sequence | `SpinOutcome` | `session.Spin(wheelFor(session.CurrentTier))` with the server's `IRandom`; persist; return the outcome. |
| `POST run/leave` | same | `RunSnapshot` | `TryLeave()`; on `false` answer 409 with the current snapshot. Credit the wallet in the player's account. |
| `POST run/revive` | same | `RunSnapshot` | `TryRevive()`; 409 when not allowed or not enough gold. |
| `POST run/restart` | same | `RunSnapshot` | `TryRestart()`. |

Minimal ASP.NET host, same library:

```csharp
app.MapPost("run/spin", (PlayerId id, IntentRequest req, ISessionStore store, IRandom rng, WheelCatalog wheels) =>
{
    var session = store.Load(id);                            // GameSession, persisted per player
    if (store.TryGetReply(id, req.Sequence, out var cached))  // idempotent retry: same sequence, same answer
        return Results.Ok(cached);
    if (!session.CanSpin) return Results.Conflict(session.Snapshot());
    var outcome = session.Spin(wheels.For(session.CurrentTier));
    store.Save(id, session, req.Sequence, outcome);
    return Results.Ok(outcome);
});
```

What the design already provides for this:

* **DTOs** are plain and attribute-free; `JsonUtility` on the client and `System.Text.Json` on the
  server read the same shape. Enums are integers in both.
* **Permissions** travel in the snapshot. The client disables buttons from `CanSpin` / `CanLeave` /
  `CanRevive`; the server re-checks them anyway and answers 409 on a stale intent.
* **Idempotency key.** `RemoteRunService` sends a sequence number with every intent. The number is
  allocated per intent, not per request: while an intent is unanswered, a retry resends it with the
  same number, so the server answers with the stored outcome instead of rolling again.
* **Outage handling.** Requests time out after 10 s. A connection error, timeout or 5xx becomes
  `RunServiceUnavailableException`; `GameFlow.SendAsync` catches only that, shows the
  `ui_popup_error` modal ("CONNECTION LOST") and resends the same intent when the player taps RETRY.
  The busy gate stays closed meanwhile, so no other intent can slip in between. A 4xx is treated as a
  refused intent and is not retried.
* **Resume.** `run/start` returns the current snapshot, so a client that reconnects mid-run renders
  the run exactly where the server has it.
* **Fairness extension.** Because `IRandom` is injected, the server can publish a commit-reveal seed
  per run (hash before, seed after) and the client can verify every roll offline with the same library.

## 7. Known gaps and next steps

Honest list, in priority order, of what a production rollout would add:

1. **`RemoteRunService` has not met a real server.** It compiles, mirrors `LocalRunService` and its
   failure path is exercised through a simulated outage (`AgentScripts/FlakyService.cs`), but no
   backend exists to run it against; a sample host (section 6) plus an integration test would close
   that.
2. **Wheel content versioning.** The client renders slice icons from its own `WheelTierConfig`. A
   content hash in `RunSnapshot` would let the client detect a server-side wheel change.
3. **A refused intent (4xx) has no UI.** The server's 409 would carry the current snapshot; the client
   should apply it and re-render instead of surfacing an exception. Cannot happen with the local
   authority, so it is not built.
4. **EditMode tests run only in the editor.** The 10 `GameFlow` tests need Unity; CI covers the rules
   only. A licensed Unity runner would run both.
5. **No audio.** No audio assets were provided; out of scope for the demo.

## 8. Presentation and polish

* **Wheel.** `WheelView` tweens `ui_pivot_wheel` with `DOLocalRotate` (4 full turns + the target
  slice, `OutQuint`, 3.5 s). Each `SliceView` counter-rotates its content pivot every frame so icons
  stay upright. Win: `DOPunchScale` on the slice content. Bomb: `DOShakeAnchorPos` on
  `ui_pivot_wheel_shake`, a child pivot, never the panel root. Feel `MMF_Player`s add scale / image
  feedback and haptics (`MMF_NVPreset`) on spin start, win and bomb.
* **Slices.** Slice rect = chamber circle (114 units, ring radius 212, both measured from the base
  sprites). Content is a `VerticalLayoutGroup`: a 75x58 icon box held by a `LayoutElement` with
  `Image.preserveAspect`, then the amount label. Tall, wide and square reward sprites all fit.
* **Popups.** `PopupView` fades the pivot and scales the card (`OutBack` in, `InBack` out), awaited
  with UniTask so the flow continues only when the popup is on screen.
* **Zone strip.** A `HorizontalLayoutGroup` of `ui_cell_zone` prefabs slid with `DOAnchorPosX` so the
  current zone stays centred; safe zones are green.
* **Lists.** The reward panel is a `ScrollRect` with a `VerticalLayoutGroup` of `ui_item_reward`.
* **Tiers.** `WheelPresenter` swaps base and indicator sprites and titles per tier from the snapshot.

## 9. Out of scope

Save/load across sessions, a DI container, localisation, audio, IAP, a deployed backend.
