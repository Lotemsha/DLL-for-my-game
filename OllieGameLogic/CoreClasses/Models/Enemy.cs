using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class Enemy : Character
    {
        public int EnemyID { get; set; }
        public int BaseDamage { get; set; }
        public float BaseAccuracy { get; set; }
        public int RewardXP { get; set; }
        public EnemyType Type { get; set; }
        public int EmotionalResistance { get; set; }

        public Enemy(int id, string name, int health, float speed, int damage, int xpReward, EnemyType enemyType, float baseAccuracy)
            : base(name, health, speed, health)
        {
            EnemyID = id;
            BaseDamage = damage;
            RewardXP = xpReward;
            Type = enemyType;
            EmotionalResistance = health / 10;
            BaseAccuracy = baseAccuracy;
        }
        public Enemy Clone()
        {
            return new Enemy(
                this.EnemyID,
                this.Name,
                (int)this.MaxHealth, 
                this.Speed,
                (int)this.BaseDamage,
                this.RewardXP,
                this.Type,
                this.BaseAccuracy
            );
        }

        public override void Movement() { }
    }
}
