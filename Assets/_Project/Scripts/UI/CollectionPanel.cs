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
        [SerializeField] private Text skinTitle;
        [SerializeField] private Button previousSkinButton;
        [SerializeField] private Button nextSkinButton;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button closeButton;
        [Tooltip("Used when an item has no sprite.")]
        [SerializeField] private Sprite placeholderSprite;

        private readonly List<CollectionSlot> _slots = new List<CollectionSlot>();
        private IPlayerProgressService _progress;
        private MergeProgression _content;
        private DesiMerge.Cosmetics.ICosmeticService _cosmetics;
        private readonly List<DesiMerge.Cosmetics.CosmeticDefinition> _skins = new List<DesiMerge.Cosmetics.CosmeticDefinition>();
        private int _page;

        /// <summary>The skin page shown (null = the default items only, when there is no skin catalog).</summary>
        public DesiMerge.Cosmetics.CosmeticDefinition Page => _page >= 0 && _page < _skins.Count ? _skins[_page] : null;
        public int PageCount => Mathf.Max(1, _skins.Count);
        public string SkinTitle => skinTitle != null ? skinTitle.text : string.Empty;

        /// <summary>Adds one page per item skin (each skin's 10 items are collected separately).</summary>
        public void BindSkins(DesiMerge.Cosmetics.ICosmeticService cosmetics)
        {
            _cosmetics = cosmetics;
            _skins.Clear();
            if (cosmetics != null)
            {
                foreach (var definition in cosmetics.Definitions)
                {
                    if (definition.kind == DesiMerge.Cosmetics.CosmeticKind.ItemSkin)
                    {
                        _skins.Add(definition);
                    }
                }
            }

            previousSkinButton?.onClick.RemoveListener(PreviousPage);
            nextSkinButton?.onClick.RemoveListener(NextPage);
            previousSkinButton?.onClick.AddListener(PreviousPage);
            nextSkinButton?.onClick.AddListener(NextPage);
            _page = 0;
            Refresh();
        }

        public void ShowPage(int page)
        {
            _page = (page % PageCount + PageCount) % PageCount;
            Refresh();
        }

        private void PreviousPage() => ShowPage(_page - 1);

        private void NextPage() => ShowPage(_page + 1);
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

                var skin = Page;
                var skinSprite = skin?.SpriteFor(level);
                var sprite = skinSprite != null ? skinSprite : definition.Sprite != null ? definition.Sprite : placeholderSprite;
                var name = DesiMerge.Localization.Loc.T(skin?.NameFor(level) ?? definition.DisplayName);
                if (IsFound(level))
                {
                    discovered++;
                    slot.Show(sprite, skinSprite != null ? Color.white : definition.SpriteColor, name, true);
                }
                else
                {
                    slot.Show(sprite, LockedTint, LockedHint(level), false);
                }
            }

            var page = Page;
            if (skinTitle != null)
            {
                skinTitle.text = page != null ? DesiMerge.Localization.Loc.T(page.title).ToUpperInvariant() : string.Empty;
            }

            previousSkinButton?.gameObject.SetActive(_skins.Count > 1);
            nextSkinButton?.gameObject.SetActive(_skins.Count > 1);
            var locked = page != null && _cosmetics != null && !_cosmetics.IsOwned(page.id);
            SetCounter(locked ? DesiMerge.Localization.Loc.F("Unlock {0} in Skins ({1} diamonds)", DesiMerge.Localization.Loc.Raw(page.title), page.price) : DesiMerge.Localization.Loc.F("{0} / {1} discovered", discovered, count) + (_skins.Count > 1 ? "   •   " + DesiMerge.Localization.Loc.F("{0} / {1} in all", TotalFound(), count * _skins.Count) : string.Empty));
        }

        /// <summary>Names only the already-discovered item that makes this one, so future items stay a surprise.</summary>
        private string LockedHint(int level)
        {
            if (level == 0)
            {
                return DesiMerge.Localization.Loc.T("Drop it");
            }

            var previous = _content.Get(level - 1);
            var name = Page?.NameFor(level - 1) ?? previous?.DisplayName;
            return previous != null && IsFound(level - 1) ? $"2× {DesiMerge.Localization.Loc.Raw(name)}" : "???";
        }

        /// <summary>Default items use the saved discoveries; skin items count once made or dropped with that skin.</summary>
        private bool IsFound(int level)
        {
            var page = Page;
            if (page == null || page.unlock == DesiMerge.Cosmetics.CosmeticUnlock.Default)
            {
                return _progress.IsDiscovered(_content.Get(level).Id);
            }

            return _cosmetics != null && _cosmetics.IsDiscovered(page.id, level);
        }

        private int TotalFound()
        {
            var total = 0;
            var current = _page;
            for (var page = 0; page < _skins.Count; page++)
            {
                _page = page;
                for (var level = 0; level < _content.Count; level++)
                {
                    total += IsFound(level) ? 1 : 0;
                }
            }

            _page = current;
            return total;
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
