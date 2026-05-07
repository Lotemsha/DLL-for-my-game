using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public abstract class Character
    {
        public string Name { get; set; }
        public float Health { get; set;}
        public float MaxHealth {  get; set;}
        public float Speed { get; set;}
        public int ExperiencePoints { get; set;}
        public InventoryManager Inventory { get; set; }

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
            Health -= damage;
            if (Health < 0) Health = 0;
        }
    }
}
