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
        public string Attack(PlayerManager player)
        {
            string message = Type switch
            {
                EnemyType.Fear =>
                    $"{Name} grips {player.Name}'s chest with fear, dealing {BaseDamage} emotional damage.",

                EnemyType.Stress =>
                    $"{Name} piles pressure onto {player.Name}, causing {BaseDamage} emotional strain.",

                EnemyType.Anxiety =>
                    $"{Name} floods {player.Name}'s mind with racing thoughts, dealing {BaseDamage} emotional damage.",

                EnemyType.Intimidation =>
                    $"{Name} towers over {player.Name}, overwhelming them with {BaseDamage} emotional damage.",

                EnemyType.Shame =>
                    $"{Name} whispers harsh self-judgments, hurting {player.Name} for {BaseDamage} emotional damage.",

                EnemyType.Hopelessness =>
                    $"{Name} drains {player.Name}'s will, inflicting {BaseDamage} emotional damage.",

                EnemyType.Trauma =>
                    $"{Name} resurfaces painful memories, striking {player.Name} for {BaseDamage} emotional damage.",

                EnemyType.Impatient =>
                    $"{Name} rushes {player.Name}'s thoughts, hitting for {BaseDamage} emotional damage.",

                EnemyType.Disosiate =>
                    $"{Name} pulls {player.Name} into emotional detachment, causing {BaseDamage} emotional damage.",

                EnemyType.Numbness =>
                    $"{Name} dulls {player.Name}'s senses, dealing {BaseDamage} emotional damage.",

                _ =>
                    $"{Name} affects {player.Name} emotionally for {BaseDamage} damage."
            };

            return message;
        }

        public override void Movement() { }
    }
}
