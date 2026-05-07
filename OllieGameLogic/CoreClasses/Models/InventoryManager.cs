using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreClasses.Models
{
  public class InventoryManager
    {
        public List<Item> ItemsList { get; private set; }
        public InventoryManager() 
        {
            ItemsList = new List<Item>();
        }
        public bool HasEquipment(StatType statName)
        {
            return ItemsList.Any(i => i.Type == ItemType.Equipment && i.StatName == statName);
        }
        public float GetEquipmentBonus(StatType statName)
        {
            return ItemsList
                .Where(i => i.Type == ItemType.Equipment && i.StatName == statName)
                .Sum(i => i.EffectValue);
        }
        public string AddItem(Item item)
        {
            if (item == null) return "";

            if (item.Type == ItemType.Equipment && HasEquipment(item.StatName))
                return $"You already have {item.Name}!";

            ItemsList.Add(item);
            return $"{item.Name} was added to your bag.";
        }

        public bool RemoveItem(Item item)
        {
            if (item.Type == ItemType.Equipment || item.Type == ItemType.QuestItem)
                return false;

            if (ItemsList.Contains(item))
            {
                ItemsList.Remove(item);
                return true;
            }
            return false;
        }

        public string UseItem(int itemId, PlayerManager player)
        {
            var item = ItemsList.FirstOrDefault(i => i.ItemID == itemId);
            if (item == null) return "Item not found.";

            if (item.Type == ItemType.Equipment || item.Type == ItemType.QuestItem)
                return $"{item.Name} cannot be used manually.";

            string message = item.Use(player);
            if (item.Type == ItemType.Consumable)
                RemoveItem(item);

            return message;
        }
    }
}
