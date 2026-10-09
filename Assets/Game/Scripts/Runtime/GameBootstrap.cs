using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Runtime.Flow;
using Game.Runtime.Presenters;
using Game.Runtime.UI;
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

        GameSession session;
        DisposableBag lifetime;

        void Awake()
        {
            Validate();

            session = new GameSession(config.Rules, new SystemRandom());
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

        /// <summary>Fail at startup with a readable message instead of mid-spin with a NullReferenceException.</summary>
        void Validate()
        {
            Require(config, nameof(config));
            Require(config.Progression, "config.Progression");
            Require(wheelView, nameof(wheelView));
            Require(hudView, nameof(hudView));
            Require(rewardList, nameof(rewardList));
            Require(zoneStrip, nameof(zoneStrip));
            Require(bombPopup, nameof(bombPopup));
            Require(leavePopup, nameof(leavePopup));

            var problem = config.Progression.Validate();
            if (problem != null) throw new InvalidOperationException($"{name}: {problem}");

            foreach (var tier in (WheelTier[])Enum.GetValues(typeof(WheelTier)))
            {
                var wheel = config.Progression.GetWheel(tier);
                if (wheel.Slices.Count != wheelView.SliceCount)
                    throw new InvalidOperationException($"{name}: {wheel.name} has {wheel.Slices.Count} slices but the wheel view has {wheelView.SliceCount}");
            }
        }

        void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null) throw new InvalidOperationException($"{name}: {field} is not assigned");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            wheelView = GetComponentInChildren<WheelView>(true);
            hudView = GetComponentInChildren<HudView>(true);
            zoneStrip = GetComponentInChildren<ZoneStripView>(true);
            bombPopup = GetComponentInChildren<BombPopupView>(true);
            leavePopup = GetComponentInChildren<LeavePopupView>(true);
            rewardList = UiRefs.Find<RewardListView>(this, "ui_list_rewards");
        }
#endif
    }
}
