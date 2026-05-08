using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public abstract class Character
    {
        public string Name { get; set; }
        public float Health { get; protected set;}
        public float MaxHealth { get; set;}
        public float Speed { get; set;}
        public int ExperiencePoints { get; set;}
        public bool IsAlive { get; protected set; } = true;
        public InventoryManager Inventory { get; set; }
        // Modifiers זמניים לקרב ולסביבה
        public float TempAccuracyModifier { get; set; } = 1f;
        public float TempSpeedModifier { get; set; } = 1f;
        public float TempDamageModifier { get; set; } = 1f;

        protected Character(string name, float health, float speed, float maxHealth)
        {
            Name = name;
            Health = health;
            Speed = speed;
            MaxHealth = maxHealth;
            ExperiencePoints = 0;
            Inventory = new InventoryManager();
        }
        // פונקציה מופשטת - כל מי שיורש חייב לממש אותה בדרך שלו
        public abstract void Movement();

        // פונקציה רגילה - כולם מקבלים נזק באותה צורה
        public virtual void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            Health -= damage;
            if (Health <= 0) 
                Defeat();
        }
        public virtual void Defeat()
        {
            this.IsAlive = false;
            Health = 0;
        }
        public void ResetTempModifiers()
        {
            this.TempAccuracyModifier = 1f;
            this.TempSpeedModifier = 1f;
            this.TempDamageModifier = 1f;
        }
    }
}
