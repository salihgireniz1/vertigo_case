using UnityEngine;

namespace Game.Runtime.UI
{
    /// <summary>Keeps this RectTransform inside the device safe area (notches, rounded corners).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect applied;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied) Apply(); // ponytail: per-frame rect compare instead of an orientation event
        }

        void Apply()
        {
            applied = Screen.safeArea;
            var min = applied.position;
            var max = applied.position + applied.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
