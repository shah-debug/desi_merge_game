using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Cosmetics;
using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Item skins: every skin with a preview and the player's progress in it. Owned skins can be used at once; the
    /// others are bought with Diamonds (spent once, saved at once). No ads and no real-money purchases.
    /// </summary>
    public sealed class SkinsPanel : MenuPanel
    {
        [SerializeField] private RectTransform list;
        [SerializeField] private SkinRow rowTemplate;
        [SerializeField] private Text statusText;
        [SerializeField] private DiamondCounter diamonds;
        [SerializeField] private int previewLevel = 5;

        private readonly List<SkinRow> _rows = new List<SkinRow>();
        private ICosmeticService _cosmetics;
        private IPlayerProgressService _wallet;
        private MergeProgression _content;
        private IAudioService _audio;

        public IReadOnlyList<SkinRow> Rows => _rows;
        public string StatusText => statusText != null ? statusText.text : string.Empty;

        public void Bind(ICosmeticService cosmetics, IPlayerProgressService wallet, MergeProgression content, IAudioService audio)
        {
            Unbind();
            _cosmetics = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            _wallet = wallet;
            _content = content;
            _audio = audio;
            _cosmetics.Changed += Refresh;
            if (_wallet != null)
            {
                _wallet.Changed += Refresh;
            }

            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }

            diamonds?.Bind(wallet);
            BindAudio(audio);
            Refresh();
        }

        protected override void OnDestroy()
        {
            Unbind();
            base.OnDestroy();
        }

        protected override void OnOpening()
        {
            SetStatus(string.Empty);
            Refresh();
        }

        public void Refresh()
        {
            if (_cosmetics == null || rowTemplate == null || list == null)
            {
                return;
            }

            var skins = new List<CosmeticDefinition>();
            foreach (var definition in _cosmetics.Definitions)
            {
                if (definition.kind == CosmeticKind.ItemSkin)
                {
                    skins.Add(definition);
                }
            }

            while (_rows.Count < skins.Count)
            {
                var row = Instantiate(rowTemplate, list);
                row.UseClicked += OnUse;
                row.BuyClicked += OnBuy;
                _rows.Add(row);
            }

            var selected = _cosmetics.Selected(CosmeticKind.ItemSkin);
            for (var i = 0; i < _rows.Count; i++)
            {
                if (i >= skins.Count)
                {
                    _rows[i].gameObject.SetActive(false);
                    continue;
                }

                var skin = skins[i];
                var owned = _cosmetics.IsOwned(skin.id);
                var state = selected != null && selected.id == skin.id ? SkinRowState.InUse : owned ? SkinRowState.Owned : SkinRowState.ForSale;
                var preview = skin.SpriteFor(previewLevel) ?? (_content != null && previewLevel < _content.Count ? _content.Get(previewLevel).Sprite : null);
                var affordable = _wallet != null && _wallet.Coins >= skin.price;
                _rows[i].Show(skin.id, DesiMerge.Localization.Loc.T(skin.title), Progress(skin, owned), preview, state, skin.price, affordable);
            }
        }

        private string Progress(CosmeticDefinition skin, bool owned)
        {
            var count = _content != null ? _content.Count : 0;
            var found = 0;
            for (var level = 0; level < count; level++)
            {
                found += IsFound(skin, level) ? 1 : 0;
            }

            return owned ? DesiMerge.Localization.Loc.F("{0} / {1} found", found, count) : DesiMerge.Localization.Loc.F("{0} new items to collect", count);
        }

        private bool IsFound(CosmeticDefinition skin, int level) =>
            skin.unlock == CosmeticUnlock.Default ? _wallet != null && _content != null && _wallet.IsDiscovered(_content.Get(level).Id) : _cosmetics.IsDiscovered(skin.id, level);

        private void OnUse(string id)
        {
            if (_cosmetics.TrySelect(id))
            {
                _cosmetics.Flush();
                SetStatus(string.Empty);
            }
        }

        private void OnBuy(string id)
        {
            var definition = Find(id);
            switch (_cosmetics.TryBuy(id, _wallet))
            {
                case CosmeticPurchase.Bought:
                    _cosmetics.TrySelect(id);
                    _cosmetics.Flush();
                    _audio?.PlaySfx(SfxCue.Unlock);
                    SetStatus(DesiMerge.Localization.Loc.F("{0} unlocked!", DesiMerge.Localization.Loc.Raw(definition?.title)));
                    break;
                case CosmeticPurchase.NotEnoughDiamonds:
                    SetStatus(DesiMerge.Localization.Loc.F("Need {0} diamonds. Play games and claim Daily Rewards to earn more.", definition?.price));
                    break;
            }
        }

        private CosmeticDefinition Find(string id)
        {
            foreach (var definition in _cosmetics.Definitions)
            {
                if (definition.id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private void Unbind()
        {
            if (_cosmetics != null)
            {
                _cosmetics.Changed -= Refresh;
            }

            if (_wallet != null)
            {
                _wallet.Changed -= Refresh;
            }

            _cosmetics = null;
        }
    }
}
