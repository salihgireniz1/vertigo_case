using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Runtime.Flow;
using Game.Runtime.Presenters;
using Game.Runtime.Views;
using R3;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>Composition root: builds the session, presenters and flow from the config and the scene views.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField, Min(0f), Tooltip("Pause after the wheel stops before the result is applied")]
        float resultHoldSeconds = 0.6f;

        [Header("Views (resolved in OnValidate)")]
        [SerializeField] WheelView wheelView;
        [SerializeField] HudView hudView;
        [SerializeField] RewardListView rewardList;
        [SerializeField] ZoneStripView zoneStrip;
        [SerializeField] BombPopupView bombPopup;
        [SerializeField] LeavePopupView leavePopup;

        DisposableBag lifetime;

        void Awake()
        {
            var session = new GameSession(config.Rules, new SystemRandom());
            var progression = config.Progression;
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            new WheelPresenter(session, progression, wheelView).AddTo(ref lifetime);
            new HudPresenter(session, progression.Rules, hudView).AddTo(ref lifetime);
            new ZoneStripPresenter(session, progression.Rules, zoneStrip, cancellationToken).AddTo(ref lifetime);
            new RewardsPresenter(session.Rewards, rewardList).AddTo(ref lifetime);
            new GameFlow(session, progression, wheelView, hudView, bombPopup, leavePopup, resultHoldSeconds, cancellationToken)
                .AddTo(ref lifetime);
        }

        void OnDestroy() => lifetime.Dispose();

#if UNITY_EDITOR
        void OnValidate()
        {
            wheelView = GetComponentInChildren<WheelView>(true);
            hudView = GetComponentInChildren<HudView>(true);
            zoneStrip = GetComponentInChildren<ZoneStripView>(true);
            bombPopup = GetComponentInChildren<BombPopupView>(true);
            leavePopup = GetComponentInChildren<LeavePopupView>(true);
            rewardList = UI.UiRefs.Find<RewardListView>(this, "ui_list_rewards");
        }
#endif
    }
}
