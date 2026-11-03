using System;
using DesiMerge.Configuration;
using DesiMerge.PowerUps;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    public enum PowerUpResult
    {
        /// <summary>The effect happened and one charge was used.</summary>
        Used = 0,
        /// <summary>Hammer is waiting for the player to tap an item (no charge used yet).</summary>
        TargetingStarted = 1,
        /// <summary>The player cancelled Hammer before choosing an item (no charge used).</summary>
        TargetingCancelled = 2,
        /// <summary>Not during play (paused, game over, leaving, another power-up selecting).</summary>
        NotAllowedNow = 3,
        NoCharges = 4,
        /// <summary>The board has nothing the power-up can act on.</summary>
        NothingToTarget = 5,
        /// <summary>The tap wasn't on an item; Hammer keeps waiting.</summary>
        MissedTarget = 6,
        /// <summary>Shuffle can't fit the items below the danger line; nothing moved.</summary>
        NoRoom = 7,
        UnknownPowerUp = 8,
    }

    /// <summary>
    /// Validates and applies power-up effects on the board. Plain C#, owned by <see cref="GameplayLoop"/>, which
    /// decides whether play currently allows power-ups. A charge is used only after the effect has succeeded, so a
    /// refused or cancelled power-up never costs anything. Removing items never adds score or records merges.
    /// </summary>
    public sealed class PowerUpController
    {
        private readonly GameplayConfig _config;
        private readonly MergeBoard _board;
        private readonly IPowerUpService _service;
        private readonly System.Random _random;

        private readonly DesiMerge.Progression.IPlayerProgressService _wallet;

        public PowerUpController(GameplayConfig config, MergeBoard board, IPowerUpService service, System.Random random,
            DesiMerge.Progression.IPlayerProgressService wallet = null)
        {
            _wallet = wallet;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>ID of the power-up waiting for a target, or null.</summary>
        public string TargetingId { get; private set; }

        public bool IsTargeting => TargetingId != null;

        public IPowerUpService Service => _service;

        /// <param name="allowedNow">Whether play currently allows power-ups (decided by the loop).</param>
        public PowerUpResult Activate(string id, bool allowedNow)
        {
            if (IsTargeting)
            {
                // Pressing the selected power-up again cancels it; any other activation is refused until then.
                if (TargetingId == id)
                {
                    CancelTargeting();
                    return PowerUpResult.TargetingCancelled;
                }

                return PowerUpResult.NotAllowedNow;
            }

            if (!allowedNow)
            {
                return PowerUpResult.NotAllowedNow;
            }

            var definition = _service.Find(id);
            if (definition == null)
            {
                return PowerUpResult.UnknownPowerUp;
            }

            if (_service.Available(id) <= 0 && !CanBuy(definition))
            {
                return PowerUpResult.NoCharges;
            }

            switch (definition.kind)
            {
                case PowerUpKind.Hammer:
                    if (_board.ActiveCount == 0)
                    {
                        return PowerUpResult.NothingToTarget;
                    }

                    TargetingId = id;
                    return PowerUpResult.TargetingStarted;

                case PowerUpKind.Shuffle:
                    if (_board.ActiveCount < 2)
                    {
                        return PowerUpResult.NothingToTarget;
                    }

                    if (!_board.TryShuffle(_random))
                    {
                        return PowerUpResult.NoRoom;
                    }

                    return Consume(id);

                default:
                    return PowerUpResult.UnknownPowerUp;
            }
        }

        /// <summary>Hammer: removes the item under a board-local point. A miss keeps waiting for another tap.</summary>
        public PowerUpResult SelectTarget(Vector2 boardLocalPoint, bool allowedNow)
        {
            if (!IsTargeting || !allowedNow)
            {
                return PowerUpResult.NotAllowedNow;
            }

            var item = _board.FindItemAt(boardLocalPoint, _config.HammerTapTolerance);
            if (item == null || !_board.TryRemove(item))
            {
                return PowerUpResult.MissedTarget;
            }

            var id = TargetingId;
            TargetingId = null;
            return Consume(id);
        }

        /// <summary>Leaves Hammer selection without using a charge. Returns false if nothing was selecting.</summary>
        public bool CancelTargeting()
        {
            if (!IsTargeting)
            {
                return false;
            }

            TargetingId = null;
            return true;
        }

        /// <summary>Diamond price of one use of a power-up (0 = not for sale).</summary>
        public int PriceOf(string id) => _service.Find(id)?.diamondPrice ?? 0;

        /// <summary>True when the power-up has no uses left but the player can buy one with diamonds.</summary>
        public bool CanBuy(PowerUpDefinition definition) =>
            _wallet != null && definition != null && definition.diamondPrice > 0 && _wallet.Coins >= definition.diamondPrice;

        private PowerUpResult Consume(string id)
        {
            if (_service.Available(id) <= 0 && CanBuy(_service.Find(id)))
            {
                // Bought: diamonds are spent only now, after the effect has worked, and saved at once so a crash
                // can't refund them. Reported as a use so missions treat it like any other power-up.
                if (_wallet.TrySpendCoins(_service.Find(id).diamondPrice))
                {
                    _wallet.Flush();
                }

                _service.RecordPurchasedUse(id);
                return PowerUpResult.Used;
            }

            if (!_service.TryConsume(id))
            {
                // Availability was checked before the effect; this would mean the charges changed meanwhile.
                Debug.LogError($"[PowerUps] '{id}' was applied but no charge could be used.");
            }

            return PowerUpResult.Used;
        }
    }
}
