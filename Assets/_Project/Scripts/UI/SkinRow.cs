using System;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    public enum SkinRowState
    {
        InUse = 0,
        Owned = 1,
        ForSale = 2,
    }

    /// <summary>One skin in the Skins screen: preview, name, progress, and In use / Use / Buy.</summary>
    public sealed class SkinRow : MonoBehaviour
    {
        [SerializeField] private Image preview;
        [SerializeField] private Text title;
        [SerializeField] private Text detail;
        [SerializeField] private Button useButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text priceText;
        [SerializeField] private GameObject inUseMark;
        [SerializeField] private CanvasGroup buyGroup;

        public event Action<string> UseClicked;
        public event Action<string> BuyClicked;

        public string Id { get; private set; }
        public SkinRowState State { get; private set; }
        internal Button UseButton => useButton;
        internal Button BuyButton => buyButton;

        private void Awake()
        {
            useButton?.onClick.AddListener(() => UseClicked?.Invoke(Id));
            buyButton?.onClick.AddListener(() => BuyClicked?.Invoke(Id));
        }

        public void Show(string id, string name, string progress, Sprite previewSprite, SkinRowState state, int price, bool affordable)
        {
            Id = id;
            State = state;
            if (title != null)
            {
                title.text = name;
            }

            if (detail != null)
            {
                detail.text = progress;
            }

            if (preview != null)
            {
                preview.sprite = previewSprite;
                preview.enabled = previewSprite != null;
            }

            inUseMark?.SetActive(state == SkinRowState.InUse);
            useButton?.gameObject.SetActive(state == SkinRowState.Owned);
            buyButton?.gameObject.SetActive(state == SkinRowState.ForSale);
            if (priceText != null)
            {
                priceText.text = price.ToString();
            }

            if (buyGroup != null)
            {
                buyGroup.alpha = affordable ? 1f : 0.55f;
            }

            gameObject.SetActive(true);
        }
    }
}
