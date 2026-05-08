using CoreClasses.Models;
using System;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace CoreClasses.Models
{
    public class CombatManager
    {
        // =====================================================================
        #region Constants & Fields
        // =====================================================================

        // Special damage flag for self‑damage (Critical Fail)
        private const float SELF_DAMAGE_FLAG = -999f;

        // Critical Fail chances
        private const double PANIC_CRIT_FAIL = 0.10;
        private const double FREEZE_CRIT_FAIL = 0.05;
        private const double HIGH_CRIT_FAIL = 0.02;

        // Critical Hit multipliers
        private const float BASIC_CRIT_MULTIPLIER = 1.75f;
        private const float STRONG_CRIT_MULTIPLIER = 1.5f;

        // Accuracy limits
        private const float MIN_ACCURACY = 0.10f;
        private const float MAX_ACCURACY = 0.95f;

        // Strong Attack stun chance
        private const int STRONG_ATTACK_STUN_CHANCE = 25;

        // Self‑damage formula
        private const float SELF_DAMAGE_BASE = 5f;
        private const float SELF_DAMAGE_PER_LEVEL = 1.5f;

        // Lose turn chances
        private const int LOW_LOSE_TURN_PERCENT = 10;
        private const int FREEZE_LOSE_TURN_PERCENT = 30;

        // Miss chances
        private const int HIGH_MISS_PERCENT = 10;
        private const int PANIC_MISS_PERCENT = 25;
        #endregion


        // =====================================================================
        #region Properties
        // =====================================================================
        private Random _random = new Random();
        public bool IsPlayerTurn { get; set; }
        public bool IsCombatActive { get; set; }
        public BattleState LastState { get; private set; }
        private bool _playerUsedStrongLastTurn = false;
        private bool _enemyWillLoseNextTurn = false;
        private bool _playerWillLoseNextTurn = false;
        private bool _lastAttackWasCritical = false;
        #endregion

        // =====================================================================
        #region Public API
        // =====================================================================

        public string StartNewRound(PlayerManager player, Enemy enemy)
        {
            IsCombatActive = true;
            LastState = BattleState.Ongoing;

            // תור רנדומלי בכל תחילת סיבוב
            if (_random.Next(2) == 0)
            {
                IsPlayerTurn = true;
                return $"New Round! {player.Name} moves first.";
            }
            else
            {
                IsPlayerTurn = false;
                return $"New Round! {enemy.Name} is faster this time!";
            }
        }

        public string PlayerTurn(PlayerManager player, Enemy enemy, int attackType = 1)
        {
            // אם הקרב לא פעיל או לא תור השחקן — אין מה לעשות
            if (!IsPlayerTurn || !IsCombatActive) return "Wait for your turn...";

            // השחקן מאבד תור בגלל טראומה
            if (_playerWillLoseNextTurn)
            {
                _playerWillLoseNextTurn = false;
                IsPlayerTurn = false;
                return $"{player.Name} is overwhelmed and loses the turn!";
            }

            // השחקן מאבד תור בגלל חרדה גבוהה (Freeze / Low)
            if (ShouldLoseTurn(player))
            {
                player.Anxiety.Decrease(5f);
                IsPlayerTurn = false;
                return $"{player.Name} freezes from anxiety and loses the turn!";
            }

            // השחקן מפספס בגלל חרדה גבוהה (High / Panic)
            if (ShouldMiss(player))
            {
                player.Anxiety.Increase(5f);
                IsPlayerTurn = false;
                return $"{player.Name} misses the attack due to stress!";
            }

            //  מניעת שימוש כפול ב־Strong Attack
            if (attackType == 2 && _playerUsedStrongLastTurn)
            {
                return $"{player.Name} is still recovering and cannot use a strong attack twice in a row!";
            }

            // ביצוע פעולה לפי סוג ההתקפה
            switch (attackType)
            {
                case 1:
                    _playerUsedStrongLastTurn = false;
                    return HandleDamageResult(player, enemy, PerformBasicAttack(player, enemy));

                case 2:
                    _playerUsedStrongLastTurn = true;
                    player.TempSpeedModifier *= 0.8f;
                    if (_random.Next(100) < STRONG_ATTACK_STUN_CHANCE)
                        _enemyWillLoseNextTurn = true;
                    return HandleDamageResult(player, enemy, PerformStrongAttack(player, enemy));

                case 3:
                    _playerUsedStrongLastTurn = false;
                    return PerformBreathing(player);

                case 4:
                    _playerUsedStrongLastTurn = false;
                    return PerformGrounding(player);

                case 5:
                    _playerUsedStrongLastTurn = false;
                    return PerformSelfTalk(player, enemy);

                default:
                    return $"{player.Name} hesitates and does nothing...";
            }
        }

        public string EnemyTurn(PlayerManager player, Enemy enemy)
        {
            if (!IsCombatActive)
                return "";

            IsPlayerTurn = false;

            // האויב מאבד תור בגלל Strong Attack של השחקן
            if (_enemyWillLoseNextTurn)
            {
                _enemyWillLoseNextTurn = false;
                enemy.ResetTempModifiers();
                IsPlayerTurn = true;
                return $"{enemy.Name} is stunned and loses its turn!";
            }

            // האויב מפספס בגלל מהירות
            if (EnemyShouldMiss(enemy, player))
            {
                enemy.ResetTempModifiers();
                IsPlayerTurn = true;
                return $"{enemy.Name} lashes out but misses!";
            }

            // האויב תוקף
            string attackMessage = EnemyPerformAttack(player, enemy);

            // בדיקת מוות של השחקן
            if (player.Health <= 0)
            {
                player.Defeat();
                LastState = BattleState.PlayerLost;
                IsCombatActive = false;
                return $"{enemy.Name} overwhelms {player.Name}. You lose...";
            }

            // איפוס מודיפיירים זמניים של האויב
            enemy.ResetTempModifiers();

            // החזרת תור לשחקן
            IsPlayerTurn = true;

            return attackMessage;
        }

        public float CalculateDamage(float attackerStrength, float targetDefense)
        {
            float damage = attackerStrength - targetDefense;
            return damage > 0 ? (float)Math.Round(damage, 1) : 1;
        }

        public string EndCombat(PlayerManager player, Enemy enemy)
        {
            IsCombatActive = false;
            StringBuilder summary = new StringBuilder();
            summary.AppendLine($"{enemy.Name} defeated!");
            summary.AppendLine($"Gained {enemy.RewardXP} XP.");

            player.GainXP(enemy.RewardXP);

            if (enemy.Inventory.ItemsList.Count > 0)
            {
                summary.Append("Loot: ");
                foreach (var item in enemy.Inventory.ItemsList)
                {
                    player.Inventory.AddItem(item);
                    summary.Append($"{item.Name} ");
                }
            }
            return summary.ToString();
        }

        #endregion
        // =====================================================================
        #region Player Damage Handling
        // =====================================================================

        private string HandleDamageResult(PlayerManager player, Enemy enemy, float damage)
        {
            // פספוס רגיל
            if (damage < 0 && damage != SELF_DAMAGE_FLAG)
            {
                IsPlayerTurn = false;
                return $"{player.Name} tries to attack but misses!";
            }

            // פגיעה עצמית (Critical Fail)
            if (damage == SELF_DAMAGE_FLAG)
            {
                var state = player.Anxiety.GetState();
                string msg;

                switch (state)
                {
                    case AnxietyState.Panic:
                        msg = $"{player.Name} spirals into panic and injures themselves!";
                        break;

                    case AnxietyState.Freeze:
                        msg = $"{player.Name} freezes, stumbles, and gets hurt!";
                        break;

                    case AnxietyState.High:
                        msg = $"{player.Name}'s hands shake violently — they hurt themselves!";
                        break;

                    default:
                        msg = $"{player.Name} loses control and gets hurt!";
                        break;
                }

                // בדיקת מוות מפגיעה עצמית
                if (player.Health <= 0)
                {
                    player.Defeat();
                    LastState = BattleState.PlayerLost;
                    IsCombatActive = false;
                    return $"{player.Name} collapses under overwhelming panic... You lose.";
                }

                IsPlayerTurn = false;
                return msg;
            }

            // נזק רגיל לאויב
            enemy.TakeDamage(damage);

            // בדיקת מוות של האויב
            if (enemy.Health <= 0)
            {
                LastState = BattleState.PlayerWon;
                player.ResetTempModifiers();
                return EndCombat(player, enemy);
            }

            // איפוס מודיפיירים
            player.ResetTempModifiers();
            IsPlayerTurn = false;

            if (_lastAttackWasCritical)
                return $"{player.Name} lands a CRITICAL HIT! {damage} damage!";

            return $"{player.Name} dealt {damage} damage!";
        }

        #endregion


        // =====================================================================
        #region Player Attacks
        // =====================================================================

        private float PerformBasicAttack(PlayerManager player, Enemy enemy)
        {
            _lastAttackWasCritical = false;

            if (IsCriticalFail(player))
            {
                float selfDamage = SELF_DAMAGE_BASE + player.Level * SELF_DAMAGE_PER_LEVEL;
                player.TakeDamage(selfDamage);
                return SELF_DAMAGE_FLAG;
            }

            float accuracy = GetFinalAccuracy(player);
            if (_random.NextDouble() > accuracy)
                return -1f;

            float basePower = 10f + (player.Level * 2);

            float damage = CalculateDamage(
                basePower * player.TempDamageModifier,
                enemy.EmotionalResistance / 2f
            );

            if (IsCriticalHit(player))
            {
                damage *= BASIC_CRIT_MULTIPLIER;
                _lastAttackWasCritical = true;
            }

            return damage;
        }

        private float PerformStrongAttack(PlayerManager player, Enemy enemy)
        {
            _lastAttackWasCritical = false;

            float accuracy = GetFinalAccuracy(player);
            if (_random.NextDouble() > accuracy)
                return -1f;

            float basePower = 20f + (player.Level * 2);

            float damage = CalculateDamage(
                basePower * player.TempDamageModifier,
                enemy.EmotionalResistance / 2f
            );

            if (IsCriticalHit(player))
            {
                damage *= STRONG_CRIT_MULTIPLIER;
                _lastAttackWasCritical = true;
            }

            return damage;
        }

        #endregion


        // =====================================================================
        #region Player Actions (Breathing, Grounding, SelfTalk)
        // =====================================================================

        private string PerformGrounding(PlayerManager player)
        {
            float calm = 7f;
            player.Anxiety.Decrease(calm);

            player.TempAccuracyModifier *= 1.2f;

            return $"{player.Name} grounds herself, feeling more present (-{calm} anxiety, +accuracy).";
        }

        private string PerformBreathing(PlayerManager player)
        {
            float calm = 12f;
            player.Anxiety.Decrease(calm);

            return $"{player.Name} takes a deep, steady breath... (-{calm} anxiety)";
        }

        private string PerformSelfTalk(PlayerManager player, Enemy enemy)
        {
            float calm = 5f;
            player.Anxiety.Decrease(calm);
            player.TempDamageModifier *= 1.25f;
            enemy.TempDamageModifier *= 0.85f;
            return $"{player.Name} uses positive self-talk, weakening the enemy's impact (-{calm} anxiety, +damage, enemy damage down).";
        }

        #endregion


        // =====================================================================
        #region Player Calculations (Accuracy, Critical, Anxiety)
        // =====================================================================

        private float GetFinalAccuracy(PlayerManager player)
        {
            float accuracy = 1f;

            accuracy *= player.TempAccuracyModifier;

            accuracy = ModifyAccuracy(player, accuracy);

            accuracy = Math.Clamp(accuracy, MIN_ACCURACY, MAX_ACCURACY);

            return accuracy;
        }

        private float ModifyAccuracy(PlayerManager player, float baseAcc)
        {
            float speedFactor = player.CurrentSpeed / player.Speed;
            baseAcc *= speedFactor;

            switch (player.Anxiety.GetState())
            {
                case AnxietyState.Freeze: return baseAcc * 0.6f;
                case AnxietyState.Low: return baseAcc * 0.9f;
                case AnxietyState.Balanced: return baseAcc * 1.1f;
                case AnxietyState.High: return baseAcc * 0.85f;
                case AnxietyState.Panic: return baseAcc * 0.7f;
                default: return baseAcc;
            }
        }

        private bool IsCriticalHit(PlayerManager player)
        {
            float CriticalChance = GetCriticalChance(player);
            return _random.NextDouble() < CriticalChance;
        }

        private float GetCriticalChance(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            switch (state)
            {
                case AnxietyState.Freeze: return 0.02f;
                case AnxietyState.Panic: return 0.03f;
                case AnxietyState.High: return 0.05f;
                case AnxietyState.Low: return 0.10f;
                case AnxietyState.Balanced: return 0.15f;
                default: return 0.10f;
            }
        }

        private bool IsCriticalFail(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            switch (state)
            {
                case AnxietyState.Panic: return _random.NextDouble() < PANIC_CRIT_FAIL;
                case AnxietyState.Freeze: return _random.NextDouble() < FREEZE_CRIT_FAIL;
                case AnxietyState.High: return _random.NextDouble() < HIGH_CRIT_FAIL;
                default: return false;
            }
        }

        private bool ShouldLoseTurn(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            if (state == AnxietyState.Low)
                return _random.Next(100) < LOW_LOSE_TURN_PERCENT;

            if (state == AnxietyState.Freeze)
                return _random.Next(100) < FREEZE_LOSE_TURN_PERCENT;

            return false;
        }

        private bool ShouldMiss(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            if (state == AnxietyState.High)
                return _random.Next(100) < HIGH_MISS_PERCENT;

            if (state == AnxietyState.Panic)
                return _random.Next(100) < PANIC_MISS_PERCENT;

            return false;
        }

        #endregion
        // =====================================================================
        #region Enemy Logic
        // =====================================================================

        private string EnemyPerformAttack(PlayerManager player, Enemy enemy)
        {
            float anxietyDefense = player.Inventory.GetEquipmentBonus(StatType.AnxietyShield);
            float physicalDefense = player.Inventory.GetEquipmentBonus(StatType.Defense);

            float currentAnxiety = player.Anxiety.Value;
            float anxietyChange = 0;
            float finalAnxietyIncrease = 0;

            // לפי סוג אויב
            switch (enemy.Type)
            {
                // FEAR — מעלה חרדה ומוריד דיוק
                case EnemyType.Fear:
                    anxietyChange = 3f;
                    player.TempAccuracyModifier *= 0.8f;
                    break;

                // STRESS — מעלה חרדה ומוריד מהירות
                case EnemyType.Stress:
                    anxietyChange = 4f;
                    player.TempSpeedModifier *= 0.85f;
                    break;

                // ANXIETY — מעלה חרדה ומוריד דיוק (סיכוי גבוה יותר לפספס)
                case EnemyType.Anxiety:
                    anxietyChange = 5f;
                    player.TempAccuracyModifier *= 0.9f;
                    break;

                // INTIMIDATION — מעלה חרדה ומוריד נזק של השחקן
                case EnemyType.Intimidation:
                    anxietyChange = 6f;
                    player.TempDamageModifier *= 0.85f;
                    break;

                // IMPATIENT — מעלה חרדה בצורה חדה ומוריד דיוק משמעותית
                case EnemyType.Impatient:
                    anxietyChange = 10f;
                    player.TempAccuracyModifier *= 0.75f;
                    break;

                // SHAME — מוריד חרדה אבל מחליש את הנזק של השחקן (פגיעה עצמית)
                case EnemyType.Shame:
                    player.Anxiety.Decrease(4f);
                    player.TempDamageModifier *= 0.9f;
                    break;

                // HOPELESSNESS — מוריד חרדה אבל מחליש מאוד את השחקן (נזק ומהירות)
                case EnemyType.Hopelessness:
                    player.Anxiety.Decrease(10f);
                    player.TempDamageModifier *= 0.8f;
                    player.TempSpeedModifier *= 0.8f;
                    break;

                // DISOSIATE — מוריד חרדה אבל מחליש את הנזק (ניתוק רגשי)
                case EnemyType.Disosiate:
                    player.Anxiety.Decrease(10f);
                    player.TempDamageModifier *= 0.85f;
                    break;

                // NUMBNESS — מוריד חרדה אבל מוריד גם דיוק וגם נזק (קהות)
                case EnemyType.Numbness:
                    player.Anxiety.Decrease(5f);
                    player.TempAccuracyModifier *= 0.9f;
                    player.TempDamageModifier *= 0.9f;
                    break;

                // TRAUMA — אפקט מורכב: לפעמים מעלה חרדה, לפעמים מוריד + סיכוי לאבד תור
                case EnemyType.Trauma:
                    if(currentAnxiety < 50f)
                        player.Anxiety.Decrease(7f);   // אם השחקן רגוע — טראומה גורמת קהות
                    else
                        anxietyChange = 7f;   // אם השחקן לחוץ — טראומה מחמירה

                    player.TempSpeedModifier *= 0.7f;  // טראומה מאטה את השחקן

                    // 20% סיכוי שהשחקן יאבד את התור הבא
                    if (_random.NextDouble() < 0.2)
                        _playerWillLoseNextTurn = true;
                    break;
            }
            if (anxietyChange > 0)
            {
                finalAnxietyIncrease = Math.Max(0, anxietyChange - anxietyDefense);
                player.Anxiety.Increase(finalAnxietyIncrease);
            }

            // חישוב נזק רגשי
            float damage = enemy.BaseDamage * enemy.TempDamageModifier;
            float finalDamage = Math.Max(1f, damage - physicalDefense);
            player.TakeDamage(finalDamage);
            float blocked = anxietyChange - finalAnxietyIncrease;

            // טקסט סופי
            string attackDescription = GetEnemyAttackMessage(enemy.Name, enemy.Type, player.Name, finalDamage);
            if (finalAnxietyIncrease < anxietyChange)
            {
                attackDescription += $" (Shield blocked {blocked:F1} Anxiety!)";
            }
            return attackDescription;
        }

        private bool EnemyShouldMiss(Enemy enemy, PlayerManager player)
        {
            float accuracy = enemy.BaseAccuracy * enemy.TempAccuracyModifier;

            accuracy -= (player.TempSpeedModifier - 1f) * 0.2f;

            accuracy = Math.Clamp(accuracy, 0.1f, 0.95f);

            double roll = _random.NextDouble();

            return roll > accuracy;
        }
        private string GetEnemyAttackMessage(string enemyName, EnemyType type, string playerName, float damage)
        {
            return type switch
            {
                EnemyType.Fear => $"{enemyName} grips {playerName}'s chest with fear, dealing {damage} emotional damage.",
                EnemyType.Stress => $"{enemyName} piles pressure onto {playerName}, causing {damage} emotional strain.",
                EnemyType.Anxiety => $"{enemyName} floods {playerName}'s mind with racing thoughts, dealing {damage} emotional damage.",
                EnemyType.Intimidation => $"{enemyName} towers over {playerName}, overwhelming them with {damage} emotional damage.",
                EnemyType.Shame => $"{enemyName} whispers harsh self-judgments, hurting {playerName} for {damage} emotional damage.",
                EnemyType.Hopelessness => $"{enemyName} drains {playerName}'s will, inflicting {damage} emotional damage.",
                EnemyType.Trauma => $"{enemyName} resurfaces painful memories, striking {playerName} for {damage} emotional damage.",
                EnemyType.Impatient => $"{enemyName} rushes {playerName}'s thoughts, hitting for {damage} emotional damage.",
                EnemyType.Disosiate => $"{enemyName} pulls {playerName} into emotional detachment, causing {damage} emotional damage.",
                EnemyType.Numbness => $"{enemyName} dulls {playerName}'s senses, dealing {damage} emotional damage.",
                _ =>
                    $"{enemyName} affects {playerName} emotionally for {damage} damage."
            };
        }
        #endregion
    }
}
