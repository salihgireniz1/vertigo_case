using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Runtime.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    public sealed class BombPopupView : PopupView, IBombPopupView
    {
        [SerializeField] Button giveUpButton;
        [SerializeField] Button reviveButton;
        [SerializeField] TMP_Text reviveCost;

        public Observable<Unit> GiveUpClicked => giveUpButton.OnClickAsObservable();
        public Observable<Unit> ReviveClicked => reviveButton.OnClickAsObservable();

        public UniTask ShowAsync(int cost, bool canRevive, CancellationToken cancellationToken)
        {
            reviveCost.text = Formatting.Amount(cost);
            reviveButton.interactable = canRevive;
            return ShowCardAsync(cancellationToken);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            giveUpButton = UiRefs.Find<Button>(this, "ui_button_give_up");
            reviveButton = UiRefs.Find<Button>(this, "ui_button_revive");
            reviveCost = UiRefs.Find<TMP_Text>(this, "ui_text_revive_cost_value");
        }
#endif
    }
}
