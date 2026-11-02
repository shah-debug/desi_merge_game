using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>One finished game in the local score history.</summary>
    public sealed class ScoreRecordRow : MonoBehaviour
    {
        [SerializeField] private Text indexText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text dateText;
        [SerializeField] private GameObject newBestBadge;

        public string ScoreLabel => scoreText != null ? scoreText.text : string.Empty;
        public bool ShowsNewBest => newBestBadge != null && newBestBadge.activeSelf;

        public void Show(int number, ScoreRecord record, int today)
        {
            if (indexText != null)
            {
                indexText.text = number.ToString();
            }

            if (scoreText != null)
            {
                scoreText.text = record.score.ToString();
            }

            if (dateText != null)
            {
                var days = today - record.day;
                dateText.text = days <= 0 ? DesiMerge.Localization.Loc.T("Today") : days == 1 ? DesiMerge.Localization.Loc.T("Yesterday") : DesiMerge.Localization.Loc.F("{0} days ago", days);
            }

            if (newBestBadge != null)
            {
                newBestBadge.SetActive(record.newBest);
            }

            gameObject.SetActive(true);
        }
    }
}
