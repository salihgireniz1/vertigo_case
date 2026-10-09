using Game.Runtime.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    /// <summary>Static HUD: next safe/super zone badges, wallet gold and the exit button.</summary>
    public sealed class HudView : MonoBehaviour, IHudView
    {
        [SerializeField] TMP_Text safeZone;
        [SerializeField] TMP_Text superZone;
        [SerializeField] TMP_Text gold;
        [SerializeField] Button exitButton;

        public Observable<Unit> ExitClicked => exitButton.OnClickAsObservable();

        public void SetNextSafeZone(int zone) => safeZone.text = zone.ToString();
        public void SetNextSuperZone(int zone) => superZone.text = zone.ToString();
        public void SetGold(int amount) => gold.text = Formatting.Amount(amount);
        public void SetExitInteractable(bool interactable) => exitButton.interactable = interactable;

#if UNITY_EDITOR
        void OnValidate()
        {
            safeZone = UiRefs.Find<TMP_Text>(this, "ui_text_safe_zone_value");
            superZone = UiRefs.Find<TMP_Text>(this, "ui_text_super_zone_value");
            gold = UiRefs.Find<TMP_Text>(this, "ui_text_gold_value");
            exitButton = UiRefs.Find<Button>(this, "ui_button_exit");
        }
#endif
    }
}
