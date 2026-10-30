using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Progression;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Shows every item in the current progression as discovered (sprite, colour, name) or locked (dark silhouette,
    /// "???"). Reads <see cref="IPlayerProgressService"/>; slots are created from a template on first open, so the
    /// grid always matches the configured content. Missing sprites fall back to a placeholder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CollectionPanel : MonoBehaviour
    {
        private static readonly Color LockedTint = new Color(0.12f, 0.1f, 0.16f, 0.85f);

        [SerializeField] private PanelTransition transition;
        [SerializeField] private RectTransform grid;
        [SerializeField] private CollectionSlot slotTemplate;
        [SerializeField] private Text counterText;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button closeButton;
        [Tooltip("Used when an item has no sprite.")]
        [SerializeField] private Sprite placeholderSprite;

        private readonly List<CollectionSlot> _slots = new List<CollectionSlot>();
        private IPlayerProgressService _progress;
        private MergeProgression _content;
        private bool _opening;

        public IReadOnlyList<CollectionSlot> Slots => _slots;
        public bool IsOpen => transition != null ? transition.IsVisible : gameObject.activeSelf;
        public string CounterText => counterText != null ? counterText.text : string.Empty;

        private void Awake()
        {
            if (slotTemplate != null)
            {
                slotTemplate.gameObject.SetActive(false);
            }

            if (!_opening)
            {
                SetVisible(false, immediate: true);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Bind(IPlayerProgressService progress, MergeProgression content, IAudioService audio)
        {
            _progress = progress;
            _content = content;
            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }
        }

        public void Open()
        {
            _opening = true;
            SetVisible(true, immediate: false);
            Refresh();
        }

        public void Close() => SetVisible(false, immediate: false);

        public void Refresh()
        {
            var count = _content != null ? _content.Count : 0;
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(count == 0 || _progress == null);
            }

            if (_progress == null || count == 0)
            {
                SetCounter("No items configured");
                return;
            }

            EnsureSlots(count);
            var discovered = 0;
            for (var level = 0; level < _slots.Count; level++)
            {
                var slot = _slots[level];
                var definition = level < count ? _content.Get(level) : null;
                slot.gameObject.SetActive(definition != null);
                if (definition == null)
                {
                    continue;
                }

                var sprite = definition.Sprite != null ? definition.Sprite : placeholderSprite;
                if (_progress.IsDiscovered(definition.Id))
                {
                    discovered++;
                    slot.Show(sprite, definition.SpriteColor, definition.DisplayName, true);
                }
                else
                {
                    slot.Show(sprite, LockedTint, LockedHint(level), false);
                }
            }

            SetCounter($"{discovered} / {count} discovered");
        }

        /// <summary>Names only the already-discovered item that makes this one, so future items stay a surprise.</summary>
        private string LockedHint(int level)
        {
            if (level == 0)
            {
                return "Drop it";
            }

            var previous = _content.Get(level - 1);
            return previous != null && _progress.IsDiscovered(previous.Id) ? $"2× {previous.DisplayName}" : "???";
        }

        private void EnsureSlots(int count)
        {
            if (slotTemplate == null || grid == null)
            {
                return;
            }

            while (_slots.Count < count)
            {
                var slot = Instantiate(slotTemplate, grid);
                slot.gameObject.SetActive(true);
                _slots.Add(slot);
            }
        }

        private void SetCounter(string text)
        {
            if (counterText != null)
            {
                counterText.text = text;
            }
        }

        private void SetVisible(bool visible, bool immediate)
        {
            if (transition == null)
            {
                gameObject.SetActive(visible);
            }
            else if (immediate)
            {
                transition.SetVisibleImmediate(visible);
            }
            else if (visible)
            {
                transition.Show();
            }
            else
            {
                transition.Hide();
            }
        }

        internal void SetReferencesForTests(RectTransform slotGrid, CollectionSlot template, Text counter, Sprite placeholder)
        {
            grid = slotGrid;
            slotTemplate = template;
            counterText = counter;
            placeholderSprite = placeholder;
        }
    }
}
