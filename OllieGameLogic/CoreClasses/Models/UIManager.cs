using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class UIManager
    {
        // מחזיר את הסטטוס העליון של המסך (HUD)
        public string GetPlayerStatus(PlayerManager player)
        {
            if (player == null) return "";
            return $"Oli | Level: {player.Level} | HP: {player.Health} | XP: {player.ExperiencePoints}";
        }

        // מחזיר רשימה של כל החפצים בתיק
        public string GetInventoryList(InventoryManager inventory)
        {
            if (inventory == null || inventory.ItemsList.Count == 0)
                return "The bag is empty.";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- Oli's Inventory ---");
            foreach (var item in inventory.ItemsList)
            {
                sb.AppendLine($"- {item.Name} (ID: {item.ItemID})");
            }
            return sb.ToString();
        }

        // מכין הודעת "קרב התחיל" מעוצבת
        public string GetCombatStartMessage(Enemy enemy, bool isPlayerTurn)
        {
            string firstMove = isPlayerTurn ? "Oli moves first!" : $"{enemy.Name} surprises you!";
            return $"Engaging {enemy.Name} in the imaginary world...\n{firstMove}";
        }

        // מעבד נתוני משימות לפורמט של "יומן משימות"
        public string GetQuestJournal(List<Quest> quests)
        {
            if (quests == null || quests.Count == 0)
                return "No active quests.";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Quest Journal:");
            foreach (var q in quests)
            {
                string status = q.IsCompleted ? "[DONE]" : "[ACTIVE]";
                sb.AppendLine($"{status} {q.Title}: {q.Description}");
            }
            return sb.ToString();
        }
    }
}