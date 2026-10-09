using System.Collections.Generic;
using Game.Core;
using Game.Runtime.UI;
using UnityEngine;

namespace Game.Runtime.Views
{
    /// <summary>Vertical list of reward stacks, one item per reward type.</summary>
    public sealed class RewardListView : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField] RewardItemView itemPrefab;

        readonly Dictionary<RewardDefinition, RewardItemView> items = new();

        public RewardItemView GetOrCreate(RewardDefinition reward)
        {
            if (items.TryGetValue(reward, out var item)) return item;
            item = Instantiate(itemPrefab, content);
            item.name = $"ui_item_reward_{reward.name}";
            items.Add(reward, item);
            return item;
        }

        public void ShowAll(IEnumerable<RewardStack> stacks)
        {
            Clear();
            foreach (var stack in stacks) GetOrCreate(stack.Reward).Bind(stack.Reward.Icon, stack.Amount);
        }

        public void Clear()
        {
            // ponytail: instantiate/destroy; pool if the list ever holds more than a dozen reward types
            foreach (var item in items.Values) Destroy(item.gameObject);
            items.Clear();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            content = UiRefs.Find<RectTransform>(this, "ui_content_rewards");
        }
#endif
    }
}
