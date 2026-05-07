using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class PlayerManager : Character
    {
        public int Level {  get; private set; }
        public int XPToNextLevel { get; private set; }
        public AnxietyBar Anxiety { get; private set; }
        public float TempAccuracyModifier { get; set; } = 1f;
        public float TempSpeedModifier { get; set; } = 1f;
        public float TempDamageModifier { get; set; } = 1f;

        public PlayerManager() : base("", 0, 0, 0) 
        {
            Anxiety = new AnxietyBar();
        } // בנאי ריק לצורך ה-Deserialization

        public PlayerManager(string name) : base (name, 100, 4f, 100)
        {
            Level = 1;
            XPToNextLevel = 100;
            Anxiety = new AnxietyBar();
        }

        public void WinBattle (int xpReward)
        {
            ExperiencePoints += xpReward;
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            while (ExperiencePoints >= XPToNextLevel)
            {
                ExperiencePoints -= XPToNextLevel;
                Level++;

                // בונוס עליית רמה: מחזקים את אולי
                MaxHealth += 10;
                Health = MaxHealth;
                Speed += 0.2f;

                XPToNextLevel = (int)(XPToNextLevel * 1.2);

                Console.WriteLine($"Level Up! Now Level {Level}");
            }
        }

        // פונקציה שמקבלת חבר וכמות . מפחיתה את הכמות מהדמות הראשית ומוסיפה לדמות החבר
        // אם עבד מקבלים אמת אחרת שקר - אם נגיד ואין מספיק נק' לדמות לחלק
        public bool ShareXP (Character friend , int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (ExperiencePoints >= amount)
            {
                ExperiencePoints -= amount;
                friend.ExperiencePoints += amount;
                return true;
            }
            return false;
        }
        // המהירות המחושבת של אולי
        public float CurrentSpeed
        {
            get
            {
                // אם החיים מתחת ל-20%, המהירות יורדת ב-50%
                if (Health < (MaxHealth * 0.2f))
                {
                    return Speed * 0.5f;
                }
                // אם החיים מתחת ל-50%, המהירות יורדת ב-20%
                else if (Health < (MaxHealth * 0.5f))
                {
                    return Speed * 0.8f;
                }

                return Speed; // מהירות רגילה
            }
        }

        public override void Movement() 
        {
            // code here
        }
    }
}
