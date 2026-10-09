using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Runtime.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    public sealed class LeavePopupView : PopupView, ILeavePopupView
    {
        [SerializeField] TMP_Text title;
        [SerializeField] RewardListView rewardList;
        [SerializeField] Button playAgainButton;

        public Observable<Unit> PlayAgainClicked => playAgainButton.OnClickAsObservable();

        public UniTask ShowAsync(int zone, IReadOnlyList<RewardEntry> rewards, CancellationToken cancellationToken)
        {
            title.text = $"ZONE {zone} - YOU LEFT WITH";
            rewardList.ShowAll(rewards);
            return ShowCardAsync(cancellationToken);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            title = UiRefs.Find<TMP_Text>(this, "ui_text_leave_title_value");
            rewardList = UiRefs.Find<RewardListView>(this, "ui_list_rewards_summary");
            playAgainButton = UiRefs.Find<Button>(this, "ui_button_play_again");
        }
#endif
    }
}
