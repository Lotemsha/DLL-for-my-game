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

        private const float MIN_SPEED_LIMIT = 1.5f;
        private const float MAX_SPEED_LIMIT = 12.0f;
        private const float STARTING_XP_TO_NEXT_LEVEL = 100f;
        private const float LEVEL_UP_MULTIPLIER = 1.2f;

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
                float calculatedSpeed = Speed * TempSpeedModifier;

                // אם החיים מתחת ל-20%, המהירות יורדת ב-50%
                if (Health < (MaxHealth * 0.2f))
                {
                    calculatedSpeed *= 0.5f;
                }
                // אם החיים מתחת ל-50%, המהירות יורדת ב-20%
                else if (Health < (MaxHealth * 0.5f))
                {
                    calculatedSpeed *= 0.8f;
                }

                return Math.Clamp(calculatedSpeed, MIN_SPEED_LIMIT, MAX_SPEED_LIMIT);
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
        #endregion
        // =====================================================================
        #region State
        // =====================================================================
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
