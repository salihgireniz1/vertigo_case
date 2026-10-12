using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    /// <summary>"Connection lost" modal. Stays up until the player taps RETRY, then hides itself.</summary>
    public sealed class ErrorPopupView : PopupView, IErrorPopupView
    {
        [SerializeField] TMP_Text message;
        [SerializeField] Button retryButton;

        public async UniTask ShowUntilRetryAsync(string reason, CancellationToken cancellationToken)
        {
            message.text = reason;
            await ShowCardAsync(cancellationToken);
            await retryButton.OnClickAsync(cancellationToken);
            await HideAsync(cancellationToken);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            message = UiRefs.Find<TMP_Text>(this, "ui_text_error_message_value");
            retryButton = UiRefs.Find<Button>(this, "ui_button_retry");
        }
#endif
    }
}
