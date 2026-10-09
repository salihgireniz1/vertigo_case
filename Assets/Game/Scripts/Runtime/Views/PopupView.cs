using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Runtime.UI;
using UnityEngine;

namespace Game.Runtime.Views
{
    /// <summary>Modal popup shell: the root stays still, the pivot fades and the card scales. Input is off while animating.</summary>
    public abstract class PopupView : MonoBehaviour
    {
        [SerializeField] CanvasGroup pivot;
        [SerializeField] RectTransform card;
        [SerializeField, Min(0.05f)] float showDuration = 0.3f;
        [SerializeField, Min(0.05f)] float hideDuration = 0.2f;

        protected async UniTask ShowCardAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            KillTweens();
            pivot.interactable = false;
            pivot.alpha = 0f;
            card.localScale = Vector3.one * 0.7f;
            await DOTween.Sequence()
                .Join(pivot.DOFade(1f, showDuration))
                .Join(card.DOScale(1f, showDuration).SetEase(Ease.OutBack))
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
            pivot.interactable = true;
        }

        public async UniTask HideAsync(CancellationToken cancellationToken)
        {
            if (!gameObject.activeSelf) return;
            KillTweens();
            pivot.interactable = false;
            await DOTween.Sequence()
                .Join(pivot.DOFade(0f, hideDuration))
                .Join(card.DOScale(0.8f, hideDuration).SetEase(Ease.InBack))
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
            gameObject.SetActive(false);
        }

        void KillTweens() => DOTween.Kill(this);

        void OnDestroy() => KillTweens();

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            pivot = UiRefs.Find<CanvasGroup>(this, "ui_pivot_popup");
            card = UiRefs.Find<RectTransform>(this, "ui_card_popup");
        }
#endif
    }
}
