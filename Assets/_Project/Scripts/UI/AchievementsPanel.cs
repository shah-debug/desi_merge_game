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
    /// Player statistics (from <see cref="IPlayerProgressService"/>) and achievement progress
    /// (from <see cref="IAchievementService"/>). Rows are created from a template to match the catalog.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementsPanel : MonoBehaviour
    {
        [SerializeField] private PanelTransition transition;
        [SerializeField] private Text statsValues;
        [SerializeField] private Text summaryText;
        [SerializeField] private RectTransform list;
        [SerializeField] private AchievementRow rowTemplate;
        [SerializeField] private Text emptyText;
        [SerializeField] private Button closeButton;

        private readonly List<AchievementRow> _rows = new List<AchievementRow>();
        private IAchievementService _achievements;
        private IPlayerProgressService _progress;
        private MergeProgression _content;
        private bool _opening;

        public IReadOnlyList<AchievementRow> Rows => _rows;
        public bool IsOpen => transition != null ? transition.IsVisible : gameObject.activeSelf;
        public string StatsText => statsValues != null ? statsValues.text : string.Empty;

        private void Awake()
        {
            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }

            if (!_opening)
            {
                if (transition != null)
                {
                    transition.SetVisibleImmediate(false);
                }
                else
                {
                    gameObject.SetActive(false);
                }
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

        public void Bind(IAchievementService achievements, IPlayerProgressService progress, MergeProgression content, IAudioService audio)
        {
            _achievements = achievements;
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
            if (transition != null)
            {
                transition.Show();
            }
            else
            {
                gameObject.SetActive(true);
            }

            Refresh();
        }

        public void Close()
        {
            if (transition != null)
            {
                transition.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Achievement rewards are applied automatically when the achievement unlocks.</summary>
        private string WithReward(DesiMerge.Progression.AchievementDefinition achievement)
        {
            var reward = achievement.reward;
            if (!reward.IsSomething)
            {
                return achievement.description;
            }

            var name = reward.type == DesiMerge.Progression.RewardType.PowerUpCharges && !string.IsNullOrEmpty(reward.powerUpId)
                ? char.ToUpperInvariant(reward.powerUpId[0]) + reward.powerUpId.Substring(1)
                : "Coins";
            var state = _achievements.IsUnlocked(achievement.id) ? "received" : "reward";
            return $"{achievement.description}  •  {name} ×{reward.amount} {state}";
        }

        public void Refresh()
        {
            if (_progress != null && statsValues != null)
            {
                statsValues.text =
                    $"{_progress.HighScore}\n{_progress.GamesPlayed}\n{_progress.TotalMerges}\n{HighestItemName()}\n" +
                    $"{_progress.DisplayedStreak()}  (best {_progress.BestStreak})";
            }

            var definitions = _achievements != null ? _achievements.Definitions : null;
            var count = definitions != null ? definitions.Count : 0;
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(count == 0);
            }

            if (summaryText != null)
            {
                summaryText.text = count > 0 ? $"{_achievements.UnlockedCount} / {count} unlocked" : string.Empty;
            }

            if (count == 0 || rowTemplate == null || list == null)
            {
                return;
            }

            while (_rows.Count < count)
            {
                var row = Instantiate(rowTemplate, list);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].gameObject.SetActive(i < count);
                if (i >= count)
                {
                    continue;
                }

                var achievement = definitions[i];
                _rows[i].Show(achievement.title, WithReward(achievement), _achievements.ProgressOf(achievement),
                    _achievements.TargetOf(achievement), _achievements.IsUnlocked(achievement.id));
            }
        }

        private string HighestItemName()
        {
            var index = _content != null ? _content.IndexOf(_progress.HighestItemId) : -1;
            return index >= 0 ? _content.Get(index).DisplayName : "—";
        }

        internal void SetReferencesForTests(Text stats, RectTransform rowList, AchievementRow template)
        {
            statsValues = stats;
            list = rowList;
            rowTemplate = template;
        }
    }
}
