using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class CombatManager
    {
        // =====================================================================
        #region Constants & Fields
        // =====================================================================

        // Critical Fail chances
        private const double PANIC_CRIT_FAIL = 0.10;
        private const double FREEZE_CRIT_FAIL = 0.05;
        private const double HIGH_CRIT_FAIL = 0.02;

        // Critical Hit multipliers
        private const float BASIC_CRIT_MULTIPLIER = 1.5f;
        private const float STRONG_CRIT_MULTIPLIER = 1.75f;

        // Accuracy limits
        private const float MIN_ACCURACY = 0.15f;
        private const float MAX_ACCURACY = 0.95f;

        // Strong Attack stun chance
        private const int STRONG_ATTACK_STUN_CHANCE = 25;

        // Self‑damage formula
        private const float SELF_DAMAGE_BASE = 5f;
        private const float SELF_DAMAGE_PER_LEVEL = 1.5f;

        // Lose turn chances
        private const int LOW_LOSE_TURN_PERCENT = 3;
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
        public Enemy CurrentEnemy { get; private set; }

        private bool _playerUsedStrongLastTurn = false;
        private bool _enemyWillLoseNextTurn = false;
        private bool _playerWillLoseNextTurn = false;
        private bool _lastAttackWasCritical = false;

        public bool CanUseStrongAttack() => !_playerUsedStrongLastTurn;

        #endregion

        // =====================================================================
        #region Public API
        // =====================================================================
        public string InitializeBattle(PlayerManager player)
        {
            _playerUsedStrongLastTurn = false;
            _enemyWillLoseNextTurn = false;
            _playerWillLoseNextTurn = false;
            _lastAttackWasCritical = false;

            IsCombatActive = true;

            CurrentEnemy = GetRandomEnemy();

            return StartNewRound(player, CurrentEnemy);
        }
        public string StartNewRound(PlayerManager player, Enemy enemy)
        {
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
                return $"New Round! {enemy.Name} is stronger this time!";
            }
        }

        public string PlayerTurn(PlayerManager player, int attackType = 1)
        {
            if (CurrentEnemy == null || !IsCombatActive) return "No active combat.";
            if (!IsPlayerTurn) return "Wait for your turn...";

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
                return $"{player.Name} feels momentarily paralyzed by the intensity of the emotion.";
            }

            //  מניעת שימוש כפול ב־Strong Attack
            if (attackType == 2 && !CanUseStrongAttack())
            {
                return $"{player.Name} is still recovering and cannot use a strong attack twice in a row!";
            }

            if (attackType != 2)
            {
                _playerUsedStrongLastTurn = false;
            }

            // ביצוע פעולה לפי סוג ההתקפה
            switch (attackType)
            {
                case 1:
                    string[] focusOptions = {
                                                $"{player.Name} seeks a moment of clarity amidst the noise.",
                                                $"{player.Name} tries to silence the static and find control.",
                                                $"{player.Name} narrows their focus to the present moment."
                                             };
                    string focusMsg = GetRandomMessage(focusOptions);
                    return HandleDamageResult(player, CurrentEnemy, PerformBasicAttack(player, CurrentEnemy), focusMsg);

                case 2:
                    _playerUsedStrongLastTurn = true;
                    player.TempSpeedModifier = 0.8f;

                    string[] ConfrontOptions = {
                                                    $"{player.Name} challenges the validity of the negative thought!",
                                                    $"{player.Name} confronts the emotion with a powerful realization.",
                                                    $"{player.Name} refuses to accept the shadow's version of reality.",
                                                    $"{player.Name} stands firm against the overwhelming feeling."
                                                };
                    string confrontMsg = GetRandomMessage(ConfrontOptions);
                    return HandleDamageResult(player, CurrentEnemy, PerformStrongAttack(player, CurrentEnemy), confrontMsg);

                case 3:
                    return PerformBreathing(player);

                case 4:
                    return PerformGrounding(player);

                case 5:
                    return PerformSelfTalk(player, CurrentEnemy);
                    
                default:
                    return $"{player.Name} hesitates for a moment, trying to find her center...";
            }
        }

        public string EnemyTurn(PlayerManager player)
        {
            if (CurrentEnemy == null || !IsCombatActive || IsPlayerTurn) return "";
            IsPlayerTurn = false;

            // האויב מאבד תור בגלל Strong Attack של השחקן
            if (_enemyWillLoseNextTurn)
            {
                _enemyWillLoseNextTurn = false;
                CurrentEnemy.ResetTempModifiers();
                IsPlayerTurn = true;
                return $"{CurrentEnemy.Name} is stunned and loses its turn!";
            }

            // האויב מפספס בגלל מהירות
            if (EnemyShouldMiss(CurrentEnemy, player))
            {
                CurrentEnemy.ResetTempModifiers();
                IsPlayerTurn = true;
                return $"{CurrentEnemy.Name} lashes out but misses!";
            }

            // האויב תוקף
            string attackMessage = EnemyPerformAttack(player, CurrentEnemy);

            // בדיקת מוות של השחקן
            if (player.Health <= 0)
            {
                player.Defeat();
                LastState = BattleState.PlayerLost;
                IsCombatActive = false;
                return $"{CurrentEnemy.Name} overwhelms {player.Name}. You lose...";
            }

            // איפוס מודיפיירים זמניים של האויב
            CurrentEnemy.ResetTempModifiers();

            // החזרת תור לשחקן
            IsPlayerTurn = true;

            return attackMessage;
        }

        private int CalculateDamage(float attackerStrength, float targetDefense)
        {
            float damage = attackerStrength - targetDefense;
            return (int)Math.Max(5, Math.Round(damage));
        }

        public string EndCombat(PlayerManager player, Enemy enemy)
        {
            IsCombatActive = false;
            StringBuilder summary = new StringBuilder();
            summary.AppendLine($"The intensity of {enemy.Name} begins to fade");
            summary.AppendLine($"{player.Name} feels more grounded and has learned from this experience (+{enemy.RewardXP} XP)");

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

        private string HandleDamageResult(PlayerManager player, Enemy enemy, AttackResult result, string actionDescription)
        {
            switch (result.Outcome)
            {
                case AttackOutcome.Miss:
                    IsPlayerTurn = false;
                    var state = player.Anxiety.GetState();
                    if (state == AnxietyState.High || state == AnxietyState.Panic)
                        return $"{player.Name}'s thoughts are too scattered to focus on a coping technique.";
                    return $"{actionDescription} But the emotion is too loud to focus.";

                case AttackOutcome.CriticalFail:
                    if (player.Health <= 0)
                    {
                        player.Defeat();
                        LastState = BattleState.PlayerLost;
                        IsCombatActive = false;
                        return $"{player.Name} collapses under overwhelming panic... You lose.";
                    }
                    IsPlayerTurn = false;
                    return GetCriticalFailMessage(player);

                case AttackOutcome.Hit:
                    enemy.TakeDamage(result.Damage);
                    
                    // המכה הקריטית תמיד שואפת להחזיר את השחקן למרכז
                    if (_lastAttackWasCritical)
                        player.Anxiety.MoveTowardBalance(15f);
                    
                    if (_playerUsedStrongLastTurn && _random.Next(100) < STRONG_ATTACK_STUN_CHANCE)
                        _enemyWillLoseNextTurn = true;

                    if (enemy.Health <= 0)
                    {
                        LastState = BattleState.PlayerWon;
                        player.ResetTempModifiers();
                        return EndCombat(player, enemy);
                    }
                    player.ResetTempModifiers();
                    IsPlayerTurn = false;
                    string stunSuffix = _enemyWillLoseNextTurn ? " The enemy is dazed!" : "";
                    if (_lastAttackWasCritical)
                        return $"{actionDescription} A moment of profound clarity! (Impact: {result.Damage}){stunSuffix}";
                    return $"{actionDescription} (Impact: {result.Damage}){stunSuffix}";

                default:
                    return $"{actionDescription} Something unexpected happened.";
            }
        }

        #endregion


        // =====================================================================
        #region Player Attacks
        // =====================================================================

        private AttackResult PerformBasicAttack(PlayerManager player, Enemy enemy)
        {
            _lastAttackWasCritical = false;
            int anxietyChange = 6;

            if (player.Anxiety.Value < 30)
                anxietyChange += 8;

            // High / Panic miss
            if (ShouldMiss(player))
            {
                anxietyChange += 2;
                player.Anxiety.Increase(anxietyChange);
                return new AttackResult { Outcome = AttackOutcome.Miss };
            }

            // Critical Fail — self damage
            if (IsCriticalFail(player))
            {
                anxietyChange += 5;
                float selfDamage = SELF_DAMAGE_BASE + player.Level * SELF_DAMAGE_PER_LEVEL;
                player.Anxiety.Increase(anxietyChange);
                player.TakeDamage(selfDamage);
                return new AttackResult { Outcome = AttackOutcome.CriticalFail };
            }

            player.Anxiety.Increase(anxietyChange);

            // Accuracy roll miss
            double finalAcc = GetFinalAccuracy(player);
            if (_random.NextDouble() > finalAcc)
                return new AttackResult { Outcome = AttackOutcome.Miss };

            // Damage calculation
            float basePower = 10f + (player.Level * 2);
            float damage = CalculateDamage(
                basePower * player.TempDamageModifier,
                enemy.EmotionalResistance / 2f
            );

            damage *= GetVarianceByAnxiety(player);

            // Critical Hit
            if (IsCriticalHit(player))
            {
                player.Anxiety.MoveTowardBalance(10);
                damage *= BASIC_CRIT_MULTIPLIER;
                _lastAttackWasCritical = true;
            }

            return new AttackResult { Outcome = AttackOutcome.Hit, Damage = (float)Math.Round(damage) };
        }

        private AttackResult PerformStrongAttack(PlayerManager player, Enemy enemy)
        {
            _lastAttackWasCritical = false;

            double finalAcc = GetFinalAccuracy(player);
            if (_random.NextDouble() > finalAcc)
                return new AttackResult { Outcome = AttackOutcome.Miss };

            float basePower = 20 + (player.Level * 2);
            float damage = CalculateDamage(basePower * player.TempDamageModifier, enemy.EmotionalResistance / 2f);

            damage *= GetVarianceByAnxiety(player);

            if (IsCriticalHit(player))
            {
                damage *= STRONG_CRIT_MULTIPLIER;
                _lastAttackWasCritical = true;
            }

            return new AttackResult { Outcome = AttackOutcome.Hit, Damage = (float)Math.Round(damage) };
        }

        #endregion


        // =====================================================================
        #region Player Actions (Breathing, Grounding, SelfTalk)
        // =====================================================================

        private string PerformGrounding(PlayerManager player)
        {
            int calm = _random.Next(10, 15);
            player.Anxiety.Decrease(calm);
            player.TempAccuracyModifier *= 1.2f;
            player.TempDamageModifier *= 1.3f;

            string[] groundingMessages = {
                                            $"{player.Name} focuses on the weight of their body, anchoring to the floor.",
                                            $"{player.Name} notices the physical details of the room, pushing back the fog.",
                                            $"{player.Name} reaches out to touch something real, grounding their senses.",
                                            $"{player.Name} takes a slow moment to observe their surroundings without judgment.",
                                            $"{player.Name} takes a moment to listen to the quiet sounds around them."
                                        };

            IsPlayerTurn = false;
            return $"{GetRandomMessage(groundingMessages)} (-{calm} anxiety, +accuracy, +power!).";
        }

        private string PerformBreathing(PlayerManager player)
        {
            int calm = _random.Next(20, 30);
            if (player.Anxiety.IsCritical)
                calm = 30;
            player.Anxiety.Decrease(calm);

            string[] breathMessages = {
                                        $"{player.Name} takes a long, stabilizing breath. The world slows down.",
                                        $"{player.Name} inhales deeply, letting the tension melt away.",
                                        $"{player.Name} focuses on the rhythm of their breath, finding a moment of peace."
                                    };

            IsPlayerTurn = false;
            return $"{GetRandomMessage(breathMessages)} (-{calm} anxiety)";
        }

        private string PerformSelfTalk(PlayerManager player, Enemy enemy)
        {
            player.Anxiety.Decrease(5f);
            player.TempDamageModifier *= 1.25f;
            enemy.TempDamageModifier *= 0.85f;

            string[] selfTalkMsgs = {
                                        $"{player.Name} reminds themselves that this feeling is temporary.",
                                        $"{player.Name} whispers: 'I am stronger than my thoughts'.",
                                        $"{player.Name} acknowledges the feeling but chooses to move forward."
                                    };

            IsPlayerTurn = false;
            return $"{GetRandomMessage(selfTalkMsgs)} (Damage +25% next turn, Enemy weakened).";
        }

        #endregion


        // =====================================================================
        #region Player Calculations (Accuracy, Critical, Anxiety)
        // =====================================================================

        private double GetFinalAccuracy(PlayerManager player)
        {
            float accuracy = 1f;
            accuracy *= player.TempAccuracyModifier;
            accuracy = ModifyAccuracy(player, accuracy);

            return (double)Math.Clamp(accuracy, MIN_ACCURACY, MAX_ACCURACY);
        }

        private float ModifyAccuracy(PlayerManager player, float baseAcc)
        {
            float safeMaxSpeed = player.Speed > 0 ? player.Speed : 1f;
            float speedFactor = player.CurrentSpeed / safeMaxSpeed;
            baseAcc *= speedFactor;

            switch (player.Anxiety.GetState())
            {
                case AnxietyState.Freeze: return baseAcc * 0.75f;
                case AnxietyState.Low: return baseAcc * 0.95f;
                case AnxietyState.Balanced: return baseAcc * 1.1f;
                case AnxietyState.High: return baseAcc * 0.9f;
                case AnxietyState.Panic: return baseAcc * 0.8f;
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
        private List<Enemy> _enemyTemplates = new List<Enemy>
        {
            // 1. Stress - נזק גבוה ומהירות ממוצעת
            new Enemy(1, "Pressure Spike", 50, 10f, 10, 12, EnemyType.Stress, 85f),

            // 2. Hopelessness - הרבה חיים, איטי, אבל הדיוק שלו גבוה (הוא עקבי)
            new Enemy(2, "Heavy Veil", 80, 5f, 8, 15, EnemyType.Hopelessness, 90f),

            // 3. Numbness - מאוזן, נזק נמוך יחסית
            new Enemy(3, "Static Silence", 65, 8f, 7, 12, EnemyType.Numbness, 80f),

            // 4. Fear - מהיר מאוד, חיים נמוכים, דיוק בינוני (קצת כאוטי)
            new Enemy(4, "Sudden Tremor", 40, 15f, 11, 8, EnemyType.Fear, 75f),

            // 5. Shame - איטי, גורם לך "להוריד מבט"
            new Enemy(5, "Downward Glance", 60, 7f, 9, 10, EnemyType.Shame, 85f),

            // 6. Trauma - ה"טנק" של המשחק, XP גבוה מאוד
            new Enemy(6, "Echo of the Past", 90, 6f, 10, 18, EnemyType.Trauma, 80f),

            // 7. Anxiety - מהירות שיא, נזק קטן אבל מתיש
            new Enemy(7, "Rushing Heart", 45, 17f, 6, 8, EnemyType.Anxiety, 70f),

            // 8. Dissociate - חמקמק, נתונים ממוצעים
            new Enemy(8, "Fading Signal", 55, 12f, 8, 10, EnemyType.Dissociate, 80f),

            // 9. Impatient - שילוב של מהירות ונזק, אבל מת מהר מאוד
            new Enemy(9, "Frantic Pulse", 30, 20f, 12, 9, EnemyType.Impatient, 85f),

            // 10. Intimidation - חזק ומפחיד, הכי הרבה נזק ו-XP
            new Enemy(10, "Towering Doubt", 75, 5f, 15, 20, EnemyType.Intimidation, 85f)      
        };
        public Enemy GetRandomEnemy()
        {
            int randomIndex = _random.Next(_enemyTemplates.Count);
            Enemy randomEnemy = _enemyTemplates[randomIndex].Clone();

            return randomEnemy;
        }
        
        private string EnemyPerformAttack(PlayerManager player, Enemy enemy)
        {
            // השפעות סוג האויב ועדכון חרדה
            float anxietyChange = ApplyEnemyEffectsByType(player, enemy);
            int finalAnxiety = CalculateAndApplyAnxiety(player, anxietyChange);

            //  חישוב נזק פיזי
            int damage = CalculatePhysicalDamage(player, enemy);
            player.TakeDamage(damage);

            // בניית הודעת הסיכום
            return BuildAttackDescription(enemy, player, damage, anxietyChange, finalAnxiety);
        }

        private bool EnemyShouldMiss(Enemy enemy, PlayerManager player)
        {
            float accuracy = enemy.BaseAccuracy / 100f;

            // אם אולי בלחץ גבוה, לאויב קל יותר לפגוע (פחות סיכוי לפספס)
            var state = player.Anxiety.GetState();
            if (state == AnxietyState.Panic || state == AnxietyState.High)
            {
                accuracy += 0.15f;
            }

            accuracy = Math.Clamp(accuracy, 0.4f, 0.95f);
            return _random.NextDouble() > accuracy;
        }
        private string GetCriticalFailMessage(PlayerManager player)
        {
            var state = player.Anxiety.GetState();

            return state switch
            {
                AnxietyState.Panic => $"{player.Name} spirals into panic and injures themselves!",
                AnxietyState.Freeze => $"{player.Name} freezes up, making the emotional strain even harder to bear.",
                AnxietyState.High => $"{player.Name}'s hands shake violently, making {player.Name} feel even more exhausted",
                _ => $"{player.Name} loses control of the situation and gets hurt!"
            };
        }
        private string GetEnemyAttackMessage(string enemyName, EnemyType type, string playerName, float damage)
        {
            return type switch
            {
                EnemyType.Fear => $"The feeling of {enemyName} tightens around {playerName}'s chest, making it harder to breathe.",
                EnemyType.Stress => $"The weight of {enemyName} feels heavier, making everything seem urgent and overwhelming.",
                EnemyType.Anxiety => $"{enemyName} sends a flood of 'what ifs' through {playerName}'s mind, causing internal strain.",
                EnemyType.Intimidation => $"The scale of {enemyName} makes {playerName} feel small and powerless.",
                EnemyType.Shame => $"A cold wave of {enemyName} washes over {playerName}, bringing up harsh self-criticism.",
                EnemyType.Hopelessness => $"{enemyName} tries to dim {playerName}'s inner light, making the world feel grey.",
                EnemyType.Trauma => $"An old echo of {enemyName} resurfaces, pulling {playerName} back into a difficult moment.",
                EnemyType.Impatient => $"The clock of {enemyName} ticks loudly in {playerName}'s head, rushing thier thoughts.",
                EnemyType.Dissociate => $"{enemyName} creates a fog around {playerName}, making her feel distant from herself.",
                EnemyType.Numbness => $"A heavy blanket of {enemyName} settles in, making it difficult for {playerName} to feel anything at all.",
                _ => $"{enemyName} begins to occupy more space in {playerName}'s mind."
            };
        }

        #endregion

        // =====================================================================
        #region Private Functions
        // =====================================================================
        private string GetRandomMessage(string[] options)
        {
            if (options == null || options.Length == 0) return "";
            return options[_random.Next(options.Length)];
        }
        private float GetVarianceByAnxiety(PlayerManager player)
        {
            var state = player.Anxiety.GetState();
            double range;

            switch (state)
            {
                case AnxietyState.Balanced:
                    range = 0.15;
                    break;
                case AnxietyState.Low:
                case AnxietyState.High:
                    range = 0.30; 
                    break;
                case AnxietyState.Freeze:
                case AnxietyState.Panic:
                    range = 0.60;
                    break;
                default:
                    range = 0.30;
                    break;
            }

            return (float)(_random.NextDouble() * range + (1.0 - range / 2.0));
        }

        private float ApplyEnemyEffectsByType(PlayerManager player, Enemy enemy)
        {
            float currentAnxiety = player.Anxiety.Value;
            float anxietyChange = 0;

            // לפי סוג אויב
            switch (enemy.Type)
            {
                // FEAR — מעלה חרדה ומוריד דיוק
                case EnemyType.Fear:
                    anxietyChange = 12 + _random.Next(-2, 5);
                    player.TempAccuracyModifier = 0.8f;
                    break;

                // STRESS — מעלה חרדה ומוריד מהירות
                case EnemyType.Stress:
                    anxietyChange = 10 + _random.Next(-2, 5);
                    enemy.TempDamageModifier = 1.2f;
                    player.TempSpeedModifier = 0.85f;
                    break;

                // ANXIETY — מעלה חרדה ומוריד דיוק (סיכוי גבוה יותר לפספס)
                case EnemyType.Anxiety:
                    anxietyChange = 15 + _random.Next(-2, 5);
                    player.TempAccuracyModifier = 0.9f;
                    break;

                // INTIMIDATION — מעלה חרדה ומוריד נזק של השחקן
                case EnemyType.Intimidation:
                    anxietyChange = 12 + _random.Next(-2, 5);
                    player.TempDamageModifier = 0.85f;
                    break;

                // IMPATIENT — מעלה חרדה בצורה חדה ומוריד דיוק משמעותית
                case EnemyType.Impatient:
                    anxietyChange = 18 + _random.Next(-5, 5);
                    player.TempAccuracyModifier = 0.75f;
                    break;

                // SHAME — מוריד חרדה אבל מחליש את הנזק של השחקן (פגיעה עצמית)
                case EnemyType.Shame:
                    float shameDec = 12 + _random.Next(-2, 5);
                    anxietyChange = -shameDec;
                    player.TempDamageModifier = 0.9f;
                    break;

                // HOPELESSNESS — מוריד חרדה אבל מחליש מאוד את השחקן (נזק ומהירות)
                case EnemyType.Hopelessness:
                    float hopeDec = 16 + _random.Next(-2, 5); 
                    player.TempDamageModifier = 0.8f;
                    player.TempSpeedModifier = 0.8f;
                    anxietyChange = -hopeDec;
                    break;

                // DISSOSIATE — מוריד חרדה אבל מחליש את הנזק (ניתוק רגשי)
                case EnemyType.Dissociate:
                    float dissDec = 18 + _random.Next(-5, 5);
                    player.TempDamageModifier = 0.85f;
                    anxietyChange = -dissDec;
                    break;

                // NUMBNESS — מוריד חרדה אבל מוריד גם דיוק וגם נזק (קהות)
                case EnemyType.Numbness:
                    float numbDec = 12 + _random.Next(-2, 5);
                    player.TempAccuracyModifier = 0.9f;
                    player.TempDamageModifier = 0.9f;
                    anxietyChange = -numbDec;
                    break;

                // TRAUMA — אפקט מורכב: לפעמים מעלה חרדה, לפעמים מוריד + סיכוי לאבד תור
                case EnemyType.Trauma:
                    if (currentAnxiety < 50)
                    {
                        float traumaDec = 18 + _random.Next(-5, 5);  // אם השחקן רגוע — טראומה גורמת קהות
                        anxietyChange = -traumaDec;
                    }
                    else
                        anxietyChange = 18 + _random.Next(-5, 5);   // אם השחקן לחוץ — טראומה מחמירה

                    player.TempSpeedModifier = 0.7f;  // טראומה מאטה את השחקן

                    // 10% סיכוי שהשחקן יאבד את התור הבא
                    if (_random.NextDouble() < 0.1)
                        _playerWillLoseNextTurn = true;
                    break;
            }

            return anxietyChange;
        }
        private int CalculateAndApplyAnxiety(PlayerManager player, float change)
        {
            if (change < 0)
            {
                player.Anxiety.Decrease(Math.Abs(change));
                return (int)Math.Round(change);
            }
            if (change > 0)
            {
                float shield = player.Inventory.GetEquipmentBonus(StatType.AnxietyShield);
                int final = (int)Math.Round(Math.Max(0, change - shield));
                player.Anxiety.Increase(final);
                return final;
            }
            return 0;
        }

        private int CalculatePhysicalDamage(PlayerManager player, Enemy enemy)
        {
            float rawPower = enemy.BaseDamage * enemy.TempDamageModifier;

            if (player.Anxiety.Value > 70) rawPower *= 1.2f;

            float def = player.Inventory.GetEquipmentBonus(StatType.Defense);
            double variance = _random.NextDouble() * 0.3 + 0.85;

            return (int)Math.Round(Math.Max((rawPower - def) * variance, 5f));
        }
        private string BuildAttackDescription(Enemy enemy, PlayerManager player, int dmg, float rawAnxiety, int finalAnxiety)
        {
            // 1. הודעת בסיס (לפי סוג האויב) + נזק
            StringBuilder sb = new StringBuilder(GetEnemyAttackMessage(enemy.Name, enemy.Type, player.Name, dmg));
            sb.Append($" Deals {dmg} damage.");

            // 2. טיפול בחרדה - משתמשים ב-finalAnxiety כי הוא הערך האמיתי שקרה בסוף
            if (finalAnxiety > 0)
            {
                sb.Append($", Anxiety: +{finalAnxiety}");
                if (finalAnxiety < rawAnxiety)
                {
                    int blocked = (int)Math.Round(rawAnxiety - finalAnxiety);
                    sb.Append($" (Shield blocked {blocked}!)");
                }
            }
            else if (finalAnxiety < 0)
            {
                // כאן השחקן רואה שהחרדה ירדה (למשל Anxiety: -12)
                sb.Append($", Anxiety: {finalAnxiety}");
            }

            // 3. הצגת פגיעה בסטאטים (Debuffs) - החלק החשוב שציינת!
            List<string> debuffs = new List<string>();

            if (player.TempAccuracyModifier < 1f) debuffs.Add("Accuracy ↓");
            if (player.TempDamageModifier < 1f) debuffs.Add("Power ↓");
            if (player.TempSpeedModifier < 1f) debuffs.Add("Speed ↓");
            if (_playerWillLoseNextTurn) debuffs.Add("STUNNED!");

            if (debuffs.Count > 0)
            {
                sb.Append($"\nStatus: [{string.Join(", ", debuffs)}]");
            }

            return sb.ToString();
        }
        #endregion
    }
}
