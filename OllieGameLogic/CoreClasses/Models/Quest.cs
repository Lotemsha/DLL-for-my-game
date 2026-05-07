using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CoreClasses.Models
{
    public class Quest
    {
        public int QuestID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public QuestStatus Status { get; set; }
        public int RewardXP { get; set; }
        public bool IsCompleted { get; set; }
        public Quest(int questID, string title, int rewardXP, string description)
        {
            this.QuestID = questID;
            this.Title = title;
            this.RewardXP = rewardXP;
            Status = QuestStatus.NotStarted;
            Description = description;
            this.IsCompleted = false;
        }

        public string AcceptQuest()
        {
            if (Status == QuestStatus.NotStarted)
            {
                Status = QuestStatus.Active;
                return $"Quest accepted: {Title}";
            }
            return "";
        }

        public void CompleteQuest(PlayerManager player) 
        {
            if (Status == QuestStatus.Active)
            {
                Status = QuestStatus.Finished;
                IsCompleted = true;
                player.WinBattle(RewardXP);
            }
        }
    }
}
