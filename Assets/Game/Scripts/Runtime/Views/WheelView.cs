using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Runtime.UI;
using MoreMountains.Feedbacks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wheel.Rules;

namespace Game.Runtime.Views
{
    /// <summary>The revolver wheel: tier art, eight slices, spin button and the spin animation.</summary>
    public sealed class WheelView : MonoBehaviour, IWheelView
    {
        public const float SliceAngle = 360f / WheelDefinition.SliceCount;

        [SerializeField] RectTransform shakePivot;
        [SerializeField] RectTransform pivot;
        [SerializeField] Image baseImage;
        [SerializeField] Image indicatorImage;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] Button spinButton;
        [SerializeField] SliceView[] slices;
        [SerializeField] MMF_Player spinStartFeedback;
        [SerializeField] MMF_Player winFeedback;
        [SerializeField] MMF_Player bombFeedback;

        [Header("Spin")]
        [SerializeField, Min(1)] int fullTurns = 4;
        [SerializeField, Min(0.1f)] float spinDuration = 3.5f;
        [SerializeField] Ease spinEase = Ease.OutQuint;
        [SerializeField, Min(0f)] float bombShakeStrength = 22f;

        [Header("Layout")]
        [Tooltip("Distance from the wheel centre to each chamber centre. ui_spin_*_base.png chambers sit at 143 px of 485 px, which is 212 at the 720-unit wheel.")]
        [SerializeField, Min(0f)] float sliceRadius = 212f;

        public int SliceCount => slices.Length;
        public SliceView GetSlice(int index) => slices[index];
        public Observable<Unit> SpinClicked => spinButton.OnClickAsObservable();

        public void SetTier(Sprite baseSprite, Sprite indicatorSprite, string titleText, string subtitleText)
        {
            baseImage.sprite = baseSprite;
            indicatorImage.sprite = indicatorSprite;
            title.text = titleText;
            subtitle.text = subtitleText;
        }

        public void SetSpinInteractable(bool interactable) => spinButton.interactable = interactable;

        /// <summary>Spins forward a few full turns and settles with the given slice under the indicator.</summary>
        public async UniTask SpinToAsync(int sliceIndex, CancellationToken cancellationToken)
        {
            if (spinStartFeedback != null) spinStartFeedback.PlayFeedbacks();

            var current = pivot.localEulerAngles.z;
            var target = SliceAngle * sliceIndex;
            var remaining = Mathf.Repeat(target - current, 360f);
            var end = current + fullTurns * 360f + remaining;

            pivot.DOKill();
            await pivot.DOLocalRotate(new Vector3(0f, 0f, end), spinDuration, RotateMode.FastBeyond360)
                .SetEase(spinEase)
                .SetLink(gameObject)
                .OnUpdate(KeepSlicesUpright)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
            KeepSlicesUpright();
        }

        public void PlayWin(int sliceIndex)
        {
            slices[sliceIndex].PunchHighlight();
            if (winFeedback != null) winFeedback.PlayFeedbacks();
        }

        public void PlayBomb()
        {
            shakePivot.DOKill(true);
            shakePivot.DOShakeAnchorPos(0.5f, bombShakeStrength, 24).SetLink(gameObject);
            if (bombFeedback != null) bombFeedback.PlayFeedbacks();
        }

        void KeepSlicesUpright()
        {
            var angle = pivot.localEulerAngles.z;
            foreach (var slice in slices) slice.KeepUpright(angle);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            shakePivot = UiRefs.Find<RectTransform>(this, "ui_pivot_wheel_shake");
            pivot = UiRefs.Find<RectTransform>(this, "ui_pivot_wheel");
            baseImage = UiRefs.Find<Image>(this, "ui_image_wheel_base_value");
            indicatorImage = UiRefs.Find<Image>(this, "ui_image_wheel_indicator_value");
            title = UiRefs.Find<TMP_Text>(this, "ui_text_wheel_title_value");
            subtitle = UiRefs.Find<TMP_Text>(this, "ui_text_wheel_subtitle_value");
            spinButton = UiRefs.Find<Button>(this, "ui_button_spin");
            // Slice i must sit at the i-th chamber (clockwise from the top); names carry the index: ui_slice_wheel_0 .. _7.
            slices = pivot != null ? pivot.GetComponentsInChildren<SliceView>(true) : new SliceView[0];
            System.Array.Sort(slices, (a, b) => string.CompareOrdinal(a.name, b.name));
            for (var i = 0; i < slices.Length; i++)
            {
                var angle = (90f - SliceAngle * i) * Mathf.Deg2Rad;
                var rect = (RectTransform)slices[i].transform;
                var target = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sliceRadius;
                // Only write real changes: rewriting float noise would dirty every scene instance with prefab overrides.
                if ((rect.anchoredPosition - target).sqrMagnitude > 0.01f) rect.anchoredPosition = target;
            }
            spinStartFeedback = UiRefs.Find<MMF_Player>(this, "ui_feedback_spin_start");
            winFeedback = UiRefs.Find<MMF_Player>(this, "ui_feedback_win");
            bombFeedback = UiRefs.Find<MMF_Player>(this, "ui_feedback_bomb");
        }
#endif
    }
}
