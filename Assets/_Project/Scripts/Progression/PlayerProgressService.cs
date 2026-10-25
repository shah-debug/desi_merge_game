using System;
using DesiMerge.Persistence;

namespace DesiMerge.Progression
{
    public sealed class PlayerProgressService : IPlayerProgressService
    {
        private readonly JsonRepository<PlayerProgressData> _repository;
        private readonly PlayerProgressData _data;
        private bool _dirty;

        public PlayerProgressService(ISaveStore store)
        {
            _repository = new JsonRepository<PlayerProgressData>(store, SaveKey.PlayerProgress);
            _data = Sanitize(_repository.Load());
        }

        public long HighScore => _data.highScore;
        public long Coins => _data.coins;
        public int GamesPlayed => _data.gamesPlayed;

        public event Action Changed;

        public bool SubmitFinalScore(long score)
        {
            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative.");
            }

            _data.gamesPlayed++;
            var isHighScore = score > _data.highScore;
            if (isHighScore)
            {
                _data.highScore = score;
            }

            MarkChanged();
            return isHighScore;
        }

        public void AddCoins(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            _data.coins = amount > long.MaxValue - _data.coins ? long.MaxValue : _data.coins + amount;
            MarkChanged();
        }

        public bool TrySpendCoins(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            if (_data.coins < amount)
            {
                return false;
            }

            _data.coins -= amount;
            MarkChanged();
            return true;
        }

        public void Flush()
        {
            if (!_dirty)
            {
                return;
            }

            _repository.Save(_data);
            _dirty = false;
        }

        private void MarkChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }

        private static PlayerProgressData Sanitize(PlayerProgressData data)
        {
            // Future schema migrations go here, keyed on data.schemaVersion.
            data.schemaVersion = PlayerProgressData.CurrentSchemaVersion;
            data.highScore = Math.Max(0, data.highScore);
            data.coins = Math.Max(0, data.coins);
            data.gamesPlayed = Math.Max(0, data.gamesPlayed);
            return data;
        }
    }
}
