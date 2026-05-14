using CoreClasses.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CoreClasses.Models
{
    public class PlayerManager : Character
    {
        // =====================================================================
        #region Constants & Fields
        // =====================================================================

        private const float STARTING_XP_TO_NEXT_LEVEL = 100f;
        private const float LEVEL_UP_MULTIPLIER = 1.2f;

        private float _minSpeedLimit = 1.5f;
        private float _maxSpeedLimit = 12.0f;

        #endregion

        // =====================================================================
        #region Properties
        // =====================================================================
        public int Level {  get; private set; }
        public int XPToNextLevel { get; private set; }
        public AnxietyBar Anxiety { get; private set; }
        public EnvironmentType CurrentEnvironment { get; private set; }

        // המהירות המחושבת של אולי
        public float CurrentSpeed
        {
            get
            {
                float calculatedSpeed = Speed * SpeedEffectMultiplier;
                return Math.Clamp(calculatedSpeed, _minSpeedLimit, _maxSpeedLimit);
            }
        }

        #endregion
        // =====================================================================
        #region Constructors
        // =====================================================================
        // בנאי ריק לצורך ה-Deserialization
        public PlayerManager() : base("", 0, 0, 0) 
        {
            Anxiety = new AnxietyBar();
        } 

        public PlayerManager(string name) : base (name, 100, 4f, 100)
        {
            Level = 1;
            XPToNextLevel = (int)STARTING_XP_TO_NEXT_LEVEL;
            Anxiety = new AnxietyBar();
        }
        #endregion

        // =====================================================================
        #region Public API
        // =====================================================================
        public string GainXP (int xpReward)
        {
            ExperiencePoints += xpReward;
            return CheckLevelUp();
        }

        // פונקציה שמקבלת חבר וכמות . מפחיתה את הכמות מהדמות הראשית ומוסיפה לדמות החבר
        // אם עבד מקבלים אמת אחרת שקר - אם נגיד ואין מספיק נק' לדמות לחלק
        public bool ShareXP(Character friend, int amount)
        {
            if (amount <= 0 || ExperiencePoints < amount) return false;

            ExperiencePoints -= amount;
            friend.ExperiencePoints += amount;
            return true;
        }
        public void LoadPlayerData(float savedHealth, float savedAnxiety, int savedXP, int savedLevel)
        {
            this.Level = savedLevel;
            this.XPToNextLevel = (int)(STARTING_XP_TO_NEXT_LEVEL * Math.Pow(LEVEL_UP_MULTIPLIER, Level - 1));
            this.ExperiencePoints = savedXP;
            CheckLevelUp();

            this.Health = Math.Clamp(savedHealth, 0, MaxHealth);
            this.Anxiety.SetValue(savedAnxiety);
        }
        public float GetSpeedFactor()
        {
            var state = Anxiety.GetState();
            return state switch
            {
                AnxietyState.Freeze => 0.4f,
                AnxietyState.Panic => 1.2f,
                AnxietyState.Balanced => 1.0f,
                _ => 1.0f
            };
        }
        public float SpeedEffectMultiplier
        {
            get
            {
                float multiplier = TempSpeedModifier;

                // הפחתת מהירות לפי מצב בריאותי
                if (Health < (MaxHealth * 0.2f)) multiplier *= 0.5f;
                else if (Health < (MaxHealth * 0.5f)) multiplier *= 0.8f;

                // הפחתת/הגברת מהירות לפי מצב חרדה
                multiplier *= GetSpeedFactor();

                return multiplier;
            }
        }

        public void SpeedLimit (float minSpeed, float maxSpeed)
        {
            _maxSpeedLimit = maxSpeed;
            _minSpeedLimit = minSpeed;
        }
        #endregion
        // =====================================================================
        #region State
        // =====================================================================
        public void EnergyDrain(float deltaTime)
        {
            if (Health <= 0 || !IsAlive) return;

            // חסינות מבוססת XP
            float xpBonus = ExperiencePoints / 1000f;
            float resistance = 1f / (1f + xpBonus);

            // השפעת חרדה: חרדה גבוהה (מעל 70) מאיצה את הניקוז
            float anxietyPenalty = 1f;
            if (Anxiety.Value > 70f) anxietyPenalty = 1.5f;

            float baseDrain = 1.0f;
            float finalDrain = baseDrain * resistance * anxietyPenalty * deltaTime;

            Health = Math.Max(0, Health - finalDrain);
        }
        public float Heal(float amount)
        {
            float healthBefore = Health;
            if (Health < MaxHealth)
            {
                Health = Math.Min(Health + amount, MaxHealth);
            }
            return Health - healthBefore;
        }
        public override void Defeat()
        {
            base.Defeat();
            this.ResetTempModifiers();
        }
        public void Revive()
        {
            this.IsAlive = true;
            this.Anxiety.Reset();
            this.Health = MaxHealth;
            this.CurrentEnvironment = EnvironmentType.Home;
        }
        #endregion
        // =====================================================================
        #region Environment & Movement
        // =====================================================================
        public void ApplyEnvironmentEffect(EnvironmentType area, float deltaTime)
        {
            TempSpeedModifier = 1f; // איפוס לפני החלה
            switch (area)
            {
                case EnvironmentType.Home:
                    Anxiety.MoveTowardBalance(3f * deltaTime);
                    Heal(5 * deltaTime);
                    TempSpeedModifier = 1.2f;
                    break;
                case EnvironmentType.CalmSea:
                case EnvironmentType.SecretGarden:
                    Anxiety.MoveTowardBalance(1f * deltaTime);
                    Heal(2 * deltaTime);
                    break;
                case EnvironmentType.Park:
                case EnvironmentType.CoffeeShop:
                    Anxiety.MoveTowardBalance(1f * deltaTime);
                    TempSpeedModifier = 1.1f;
                    break;
                case EnvironmentType.BusyStreet:
                case EnvironmentType.Super:
                    Anxiety.Increase(2f * deltaTime);
                    TempSpeedModifier = 0.8f;
                    break;
                case EnvironmentType.DarkAlley:
                    Anxiety.Increase(2f * deltaTime);
                    TempSpeedModifier = 1.3f;
                    break;
                case EnvironmentType.AcademicBuilding:
                    Anxiety.Increase(3f * deltaTime);
                    break;
                case EnvironmentType.CrowdedSea:
                    Anxiety.Decrease(2 * deltaTime);
                    break;

                default:
                    break;
            }
        }
        public override void Movement()
        {
            // code here
        }
        #endregion
        // =====================================================================
        #region Internal Logic (Private)
        // =====================================================================
        private string CheckLevelUp()
        {
            if (ExperiencePoints < XPToNextLevel) return "";
            while (ExperiencePoints >= XPToNextLevel)
            {
                ExperiencePoints -= XPToNextLevel;
                Level++;

                // בונוס עליית רמה: מחזקים את אולי
                MaxHealth += 10;
                Health = MaxHealth;

                XPToNextLevel = (int)(XPToNextLevel * LEVEL_UP_MULTIPLIER);
            }
                return $"Level Up! Now Level {Level}";
        }
        #endregion
    }
}
