namespace CoreClasses.Models
{
    // סטטוס משימות
    public enum QuestStatus { NotStarted, Active, Finished }

    public enum ItemType { Consumable, QuestItem, Equipment, EmotionalItem }

    // סוגי אויבים
    public enum EnemyType
    {
        Fear, Stress, Anxiety, Intimidation, Shame,
        Hopelessness, Trauma, Impatient, Disosiate, Numbness
    }
    // מצבים של השחקן
    public enum GameState { MainMenu, Roaming, Combat, Paused, GameOver }
    // מצבי שחקן בזמן קרב
    public enum BattleState { Ongoing, PlayerWon, PlayerLost }
    // השפעת מצב השחקן על קרב 
    public enum AnxietyState { Freeze, Low, Balanced, High, Panic }
    public enum StatType
    {
        None,           // EmotionalItem / QuestItem / Diary / Stress Watch
        MaxHealth,      // שמיכת כובד
        AnxietyShield,  // אוזניות מבטלות רעש
        AnxietyRegen,   // אוזניות מוסיקה
        XPBonus,           
        Accuracy,       // פידג'טים
        Speed,          // נשימה עמוקה
        Damage,         // פתק
        Defense
    }
    public enum EnvironmentType { 
        Neutral, 
        CalmSea, 
        CrowdedSea, 
        BusyStreet, 
        DarkAlley, 
        Home, 
        Park, 
        AcademicBuilding, 
        Super,
        CoffeeShop,
        SecretGarden
    }
}