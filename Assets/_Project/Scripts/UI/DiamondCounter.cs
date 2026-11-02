using DesiMerge.Progression;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>Shows the player's saved Diamonds (the progress service's balance) and pops when it grows. Event-driven.</summary>
    public sealed class DiamondCounter : MonoBehaviour
    {
        [SerializeField] private Text amountText;
        [SerializeField] private ScalePunch punch;

        private IPlayerProgressService _progress;
        private long _shown = -1;

        public string AmountLabel => amountText != null ? amountText.text : string.Empty;

        public void Bind(IPlayerProgressService progress)
        {
            Unbind();
            _progress = progress;
            if (_progress != null)
            {
                _progress.Changed += Refresh;
            }

            Refresh();
        }

        private void OnDestroy() => Unbind();

        private void Refresh()
        {
            if (_progress == null)
            {
                return;
            }

            var amount = _progress.Coins;
            if (amountText != null)
            {
                amountText.text = amount.ToString();
            }

            if (_shown >= 0 && amount > _shown && punch != null && isActiveAndEnabled)
            {
                punch.Play();
            }

            _shown = amount;
        }

        private void Unbind()
        {
            if (_progress != null)
            {
                _progress.Changed -= Refresh;
            }

            _progress = null;
        }
    }
}
