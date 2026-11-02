using System;
using System.Collections.Generic;
using DesiMerge.Audio;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>Score milestones (player ranks): reached ranks can be claimed once; the next rank shows its target.</summary>
    public sealed class RanksPanel : MenuPanel
    {
        [SerializeField] private ProgressCard[] cards = Array.Empty<ProgressCard>();
        [SerializeField] private Text bestText;
        [SerializeField] private Text statusText;

        private IMilestoneService _milestones;
        private IPlayerProgressService _progress;
        private IPowerUpService _powerUps;
        private RewardPresenter _presenter;

        public IReadOnlyList<ProgressCard> Cards => cards;

        public void Bind(IMilestoneService milestones, IPlayerProgressService progress, IPowerUpService powerUps, RewardPresenter presenter, IAudioService audio)
        {
            Unbind();
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
            _progress = progress;
            _powerUps = powerUps;
            _presenter = presenter;
            _milestones.Changed += Refresh;
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.ClaimClicked += OnClaim;
                }
            }

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
            if (_milestones == null)
            {
                return;
            }

            var best = _progress?.HighScore ?? 0;
            if (bestText != null)
            {
                bestText.text = DesiMerge.Localization.Loc.F("Best score  {0}", best);
            }

            var definitions = _milestones.Definitions;
            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                {
                    continue;
                }

                if (i >= definitions.Count)
                {
                    cards[i].gameObject.SetActive(false);
                    continue;
                }

                var milestone = definitions[i];
                var reached = _milestones.IsReached(milestone.id);
                var claimed = _milestones.IsClaimed(milestone.id) || (reached && !milestone.reward.IsSomething);
                var description = reached ? DesiMerge.Localization.Loc.T(milestone.description) : DesiMerge.Localization.Loc.F("Reach a best score of {0}", milestone.score);
                cards[i].Show(milestone.id, DesiMerge.Localization.Loc.T(milestone.title), description, Math.Min(best, milestone.score), Math.Max(1, milestone.score),
                    _presenter != null ? _presenter.Describe(milestone.reward) : string.Empty, _presenter != null ? _presenter.IconFor(milestone.reward) : null,
                    reached && !claimed, claimed, animate: IsOpen);
            }
        }

        private void OnClaim(string id)
        {
            var milestone = FindDefinition(id);
            if (milestone == null)
            {
                return;
            }

            var result = _milestones.TryClaim(id);
            if (result == ClaimResult.Claimed)
            {
                SetStatus(string.Empty);
                _presenter?.Show(DesiMerge.Localization.Loc.T("NEW RANK"), DesiMerge.Localization.Loc.T(milestone.title), new[] { milestone.reward });
            }
            else if (result == ClaimResult.StorageFull)
            {
                var definition = _powerUps?.Find(milestone.reward.powerUpId);
                SetStatus(definition != null ? DesiMerge.Localization.Loc.F("{0} storage is full. Use one in a game, then claim.", DesiMerge.Localization.Loc.T(definition.title)) : DesiMerge.Localization.Loc.T("Storage is full. Use a power-up first."));
            }
            else if (result == ClaimResult.Failed)
            {
                SetStatus(DesiMerge.Localization.Loc.T("The reward couldn't be saved. Please try again."));
            }
        }

        private MilestoneDefinition FindDefinition(string id)
        {
            foreach (var definition in _milestones.Definitions)
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
            if (_milestones != null)
            {
                _milestones.Changed -= Refresh;
            }

            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.ClaimClicked -= OnClaim;
                }
            }

            _milestones = null;
        }
    }
}
