using UnityEngine;

namespace Game.Runtime.UI
{
    /// <summary>Resolves view references by child name from OnValidate, so nothing is wired by hand in the inspector.</summary>
    public static class UiRefs
    {
        public static T Find<T>(Component owner, string childName) where T : Component
        {
            foreach (var candidate in owner.GetComponentsInChildren<T>(true))
                if (candidate.name == childName) return candidate;

            Debug.LogWarning($"{owner.name}: no child '{childName}' with a {typeof(T).Name}", owner);
            return null;
        }
    }

    public static class Formatting
    {
        public static string Amount(int value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
