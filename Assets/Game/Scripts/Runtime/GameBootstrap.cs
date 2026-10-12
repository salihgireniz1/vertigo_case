using System;
using Cysharp.Threading.Tasks;
using Game.Data;
using Game.Runtime.Flow;
using Game.Runtime.Presenters;
using Game.Runtime.Services;
using Game.Runtime.State;
using Game.Runtime.UI;
using Game.Runtime.Views;
using R3;
using UnityEngine;
using Wheel.Rules;

namespace Game.Runtime
{
    /// <summary>Composition root: builds the service, store, presenters and flow from the config and the scene views.</summary>
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
        [SerializeField] ErrorPopupView errorPopup;

        IRunService service;
        RunStore store;
        DisposableBag lifetime;

        void Awake()
        {
            Validate();

            var progression = config.Progression;
            var catalog = RewardCatalog.From(progression);
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            // Demo: the rules run in-process. Production: new RemoteRunService(url, token) and nothing else changes.
            service = new LocalRunService(config, new SystemRandom());
            store = new RunStore();
            store.AddTo(ref lifetime);

            new WheelPresenter(store, progression, wheelView).AddTo(ref lifetime);
            new HudPresenter(store, progression.Rules, hudView).AddTo(ref lifetime);
            new ZoneStripPresenter(store, progression.Rules, zoneStrip, cancellationToken).AddTo(ref lifetime);
            new RewardsPresenter(store, catalog, rewardList).AddTo(ref lifetime);

            var flow = new GameFlow(service, store, catalog, wheelView, hudView, bombPopup, leavePopup, errorPopup, resultHoldSeconds, cancellationToken);
            flow.AddTo(ref lifetime);
            flow.StartAsync().Forget();
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
            Require(errorPopup, nameof(errorPopup));

            var problem = config.Progression.Validate();
            if (problem != null) throw new InvalidOperationException($"{name}: {problem}");
            if (wheelView.SliceCount != WheelDefinition.SliceCount)
                throw new InvalidOperationException($"{name}: the wheel view has {wheelView.SliceCount} slices, the rules expect {WheelDefinition.SliceCount}");
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
            errorPopup = GetComponentInChildren<ErrorPopupView>(true);
            rewardList = UiRefs.Find<RewardListView>(this, "ui_list_rewards");
        }
#endif
    }
}
