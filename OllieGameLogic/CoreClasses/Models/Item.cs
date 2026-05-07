using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class Item
    {
        public int ItemID { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public float EffectValue { get; set; }
        public StatType StatName { get; set; }

        public Item( int itemID, string name, ItemType type, float effectValue, StatType statName = StatType.None) 
        {
            this.ItemID = itemID;
            this.Name = name;
            this.Type = type;
            this.EffectValue = effectValue;
            this.StatName = statName;
        }
        public string Use(PlayerManager player)
        {
            if (Type == ItemType.Consumable)
            {
                switch (StatName)
                {
                    case StatType.MaxHealth:
                        float oldHealth = player.Health;
                        player.Health = Math.Min(player.Health + EffectValue, player.MaxHealth);
                        float healed = player.Health - oldHealth;
                        return $"{player.Name} used {Name} and recovered {healed} HP!";

                    case StatType.AnxietyRegen:
                        player.Anxiety.Decrease(EffectValue);
                        return $"{player.Name} used {Name} and feels calmer.";

                    case StatType.Accuracy:
                        player.TempAccuracyModifier *= (1 + EffectValue);
                        return $"{player.Name} feels more focused (+accuracy).";

                    case StatType.Speed:
                        player.TempSpeedModifier *= (1 + EffectValue);
                        return $"{player.Name} feels quicker (+speed).";

                    case StatType.Damage:
                        player.TempDamageModifier *= (1 + EffectValue);
                        return $"{player.Name} feels stronger (+damage).";

                    case StatType.XPBonus:
                        player.ExperiencePoints += (int)EffectValue;
                        return $"{player.Name} gained {EffectValue} bonus XP!";
                }
            }

            if (Type == ItemType.Equipment)
                return $"{Name} is already active passively.";

            if (Type == ItemType.QuestItem)
                return $"{Name} is a quest item and cannot be used.";

            if (Type == ItemType.EmotionalItem)
                return $"{Name} stirs an emotion within Oli...";

            return $"{Name} was used, but nothing happened.";
        }
    }
}
