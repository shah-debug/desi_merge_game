using System;
using DesiMerge.Persistence;
using DesiMerge.Progression;
using NUnit.Framework;

namespace DesiMerge.Tests.EditMode
{
    public class PlayerProgressServiceTests
    {
        [Test]
        public void NewPlayer_StartsEmpty()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());

            Assert.AreEqual(0, progress.HighScore);
            Assert.AreEqual(0, progress.Coins);
            Assert.AreEqual(0, progress.GamesPlayed);
        }

        [Test]
        public void SubmitFinalScore_OnlyRaisesHighScore()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());

            Assert.IsTrue(progress.SubmitFinalScore(500));
            Assert.IsFalse(progress.SubmitFinalScore(200));
            Assert.IsFalse(progress.SubmitFinalScore(500));

            Assert.AreEqual(500, progress.HighScore);
            Assert.AreEqual(3, progress.GamesPlayed);
        }

        [Test]
        public void SubmitFinalScore_RejectsNegative()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => progress.SubmitFinalScore(-1));
        }

        [Test]
        public void TrySpendCoins_FailsWhenBalanceTooLow()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());
            progress.AddCoins(10);

            Assert.IsFalse(progress.TrySpendCoins(11));
            Assert.AreEqual(10, progress.Coins);
            Assert.IsTrue(progress.TrySpendCoins(10));
            Assert.AreEqual(0, progress.Coins);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void CoinOperations_RejectNonPositiveAmounts(long amount)
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => progress.AddCoins(amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.TrySpendCoins(amount));
        }

        [Test]
        public void AddCoins_SaturatesInsteadOfOverflowing()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());
            progress.AddCoins(long.MaxValue);

            progress.AddCoins(1);

            Assert.AreEqual(long.MaxValue, progress.Coins);
        }

        [Test]
        public void Changed_IsRaisedOnMutation()
        {
            var progress = new PlayerProgressService(new InMemorySaveStore());
            var count = 0;
            progress.Changed += () => count++;

            progress.AddCoins(5);
            progress.SubmitFinalScore(10);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Flush_PersistsAndSkipsWhenClean()
        {
            var store = new InMemorySaveStore();
            var progress = new PlayerProgressService(store);

            progress.Flush();
            Assert.AreEqual(0, store.WriteCount, "Nothing changed, nothing should be written.");

            progress.AddCoins(42);
            progress.SubmitFinalScore(900);
            progress.Flush();
            progress.Flush();
            Assert.AreEqual(1, store.WriteCount);

            var reloaded = new PlayerProgressService(store);
            Assert.AreEqual(42, reloaded.Coins);
            Assert.AreEqual(900, reloaded.HighScore);
            Assert.AreEqual(1, reloaded.GamesPlayed);
        }

        [Test]
        public void Load_ClampsTamperedNegativeValues()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.PlayerProgress, "{\"schemaVersion\":1,\"highScore\":-10,\"coins\":-3,\"gamesPlayed\":-1}");

            var progress = new PlayerProgressService(store);

            Assert.AreEqual(0, progress.HighScore);
            Assert.AreEqual(0, progress.Coins);
            Assert.AreEqual(0, progress.GamesPlayed);
        }
    }
}
