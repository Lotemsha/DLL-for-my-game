using CoreClasses.Models;
using System;
using System.Text;

namespace CoreClasses.Models
{
    public class CombatManager
    {
        private Random _random = new Random();
        public bool IsPlayerTurn { get; set; }
        public bool IsCombatActive { get; set; }
        public BattleState LastState { get; private set; }
        private bool _playerUsedStrongLastTurn = false;
        private bool _enemyWillLoseNextTurn = false;
        private bool _PlayerWillLoseNextTurn = false;
        private bool _lastAttackWasCritical = false;

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
            if (_PlayerWillLoseNextTurn)
            {
                _PlayerWillLoseNextTurn = false;
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
                    if (_random.Next(100) < 25)
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
                ResetEnemyModifiers(enemy);
                IsPlayerTurn = true;
                return $"{enemy.Name} is stunned and loses its turn!";
            }
            // האויב מפספס בגלל מהירות
            if (EnemyShouldMiss(enemy, player))
            {
                ResetEnemyModifiers(enemy);
                IsPlayerTurn = true;
                return $"{enemy.Name} lashes out but misses!";
            }
            
            // האויב תוקף
            string attackMessage = EnemyPerformAttack(player, enemy);
           
            // בדיקת מוות של השחקן
            if (player.Health <= 0)
            {
                LastState = BattleState.PlayerLost;
                return $"{enemy.Name} overwhelms {player.Name}. You lose...";
            }
          
            // איפוס מודיפיירים זמניים של האויב
            ResetEnemyModifiers(enemy);
           
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

            player.WinBattle(enemy.RewardXP);

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

        // פונקציות עזר פנימיות (private)

        // Player
        private string HandleDamageResult(PlayerManager player, Enemy enemy, float damage)
        {
            // פספוס רגיל
            if (damage < 0 && damage != -999f)
            {
                IsPlayerTurn = false;
                return $"{player.Name} tries to attack but misses!";
            }

            // פגיעה עצמית (Critical Fail)
            if (damage == -999f)
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
                    LastState = BattleState.PlayerLost;
                    return $"{player.Name} collapses under overwhelming panic... You lose.";
                }

                IsPlayerTurn = false;
                return msg;
            }

            // נזק רגיל לאויב
            enemy.Health -= damage;
            if (enemy.Health < 0) enemy.Health = 0;

            // בדיקת מוות של האויב
            if (enemy.Health <= 0)
            {
                LastState = BattleState.PlayerWon;
                ResetTempModifiers(player);
                return EndCombat(player, enemy);
            }

            // איפוס מודיפיירים
            ResetTempModifiers(player);
            IsPlayerTurn = false;

            if (_lastAttackWasCritical)
                return $"{player.Name} lands a CRITICAL HIT! {damage} damage!";

            return $"{player.Name} dealt {damage} damage!";
        }

        private bool IsCriticalFail(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            switch (state)
            {
                case AnxietyState.Panic: return _random.NextDouble() < 0.10;
                case AnxietyState.Freeze: return _random.NextDouble() < 0.05; 
                case AnxietyState.High: return _random.NextDouble() < 0.02;
                default: return false;
            }
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
        private bool IsCriticalHit(PlayerManager player)
        {
            float CriticalChance = GetCriticalChance(player);
            return _random.NextDouble() < CriticalChance;
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

        private bool ShouldLoseTurn(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            if (state == AnxietyState.Low)
                return _random.Next(100) < 10;

            if (state == AnxietyState.Freeze)
                return _random.Next(100) < 30;

            return false;
        }

        private bool ShouldMiss(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            if (state == AnxietyState.High)
                return _random.Next(100) < 10;

            if (state == AnxietyState.Panic)
                return _random.Next(100) < 25;

            return false;
        }
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
        private float PerformBasicAttack(PlayerManager player, Enemy enemy)
        {
            _lastAttackWasCritical = false;

            if (IsCriticalFail(player))
            {
                float selfDamage = 5f + player.Level * 1.5f;
                player.Health -= selfDamage;
                return -999f;
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
                damage *= 1.75f;
                _lastAttackWasCritical = true;
            }

            return damage;
        }
        private float PerformStrongAttack(PlayerManager player, Enemy enemy)
        {
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
                damage *= 1.5f;
                _lastAttackWasCritical = true;
            }

                return damage;
        }
        private float GetFinalAccuracy(PlayerManager player)
        {
            float accuracy = 1f;

            accuracy *= player.TempAccuracyModifier;

            accuracy = ModifyAccuracy(player, accuracy);

            accuracy = Math.Clamp(accuracy, 0.1f, 0.95f);

            return accuracy;
        }

        private void ResetTempModifiers(PlayerManager player)
        {
            player.TempAccuracyModifier = 1f;
            player.TempSpeedModifier = 1f;
            player.TempDamageModifier = 1f;
        }
        // Enemies
        private string EnemyPerformAttack(PlayerManager player, Enemy enemy)
        {
            // בדיקת SHIELD — ציוד שמפחית עלייה בחרדה
            float shield = player.Inventory.HasEquipment(StatType.AnxietyShield)
                         ? player.Inventory.GetEquipmentBonus(StatType.AnxietyShield)
                         : 1f;

            float anxiety = player.Anxiety.Value;

            // לפי סוג אויב
            switch (enemy.Type)
            {
                // FEAR — מעלה חרדה ומוריד דיוק
                case EnemyType.Fear:
                    player.Anxiety.Increase(3f * shield);
                    player.TempAccuracyModifier *= 0.8f;
                    break;

                // STRESS — מעלה חרדה ומוריד מהירות
                case EnemyType.Stress:
                    player.Anxiety.Increase(4f * shield);
                    player.TempSpeedModifier *= 0.85f;
                    break;

                // ANXIETY — מעלה חרדה ומוריד דיוק (סיכוי גבוה יותר לפספס)
                case EnemyType.Anxiety:
                    player.Anxiety.Increase(5f * shield);
                    player.TempAccuracyModifier *= 0.9f;
                    break;

                // INTIMIDATION — מעלה חרדה ומוריד נזק של השחקן
                case EnemyType.Intimidation:
                    player.Anxiety.Increase(6f * shield);
                    player.TempDamageModifier *= 0.85f;
                    break;

                // IMPATIENT — מעלה חרדה בצורה חדה ומוריד דיוק משמעותית
                case EnemyType.Impatient:
                    player.Anxiety.Increase(10f * shield);
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
                    if (anxiety < 50f)
                        player.Anxiety.Decrease(7f);   // אם השחקן רגוע — טראומה גורמת קהות
                    else
                        player.Anxiety.Increase(7f);   // אם השחקן לחוץ — טראומה מחמירה

                    player.TempSpeedModifier *= 0.7f;  // טראומה מאטה את השחקן

                    // 20% סיכוי שהשחקן יאבד את התור הבא
                    if (_random.NextDouble() < 0.2)
                        _PlayerWillLoseNextTurn = true;

                    break;
            }

            // חישוב נזק רגשי
            float damage = enemy.BaseDamage * enemy.TempDamageModifier;
            player.Health -= damage;
            if (player.Health < 0) player.Health = 0;

            // טקסט סופי
            return $"{enemy.Name} attacks {player.Name}, dealing {damage} emotional damage!";
        }

        private bool EnemyShouldMiss(Enemy enemy, PlayerManager player)
        {
            float accuracy = enemy.BaseAccuracy * enemy.TempAccuracyModifier;

            accuracy -= (player.TempSpeedModifier - 1f) * 0.2f;

            accuracy = Math.Clamp(accuracy, 0.1f, 0.95f);

            double roll = _random.NextDouble();

            return roll > accuracy;
        }

        private void ResetEnemyModifiers(Enemy enemy)
        {
            enemy.TempAccuracyModifier = 1f;
            enemy.TempSpeedModifier = 1f;
            enemy.TempDamageModifier = 1f;
        }
    }
}

