using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class GameManager
    {
        public GameState CurrentState { get; private set; }

        public PlayerManager? Player { get; set; }
        public CombatManager Combat { get; set; }

        public GameManager()
        {
            CurrentState = GameState.MainMenu;
            Combat = new CombatManager();
        }

        public void NewGame(string playerName)
        {
            Player = new PlayerManager(playerName);
            CurrentState = GameState.Roaming;
            Console.WriteLine("New Game Started. Welcome, " + playerName);
        }

        public void LoadSavedGame()
        {
            // טעינת שחקן שעלולה להחזיר null אם אין קובץ שמירה
            PlayerManager? loadedPlayer = DatabaseManager.LoadGame();

            if (loadedPlayer != null)
            {
                Player = loadedPlayer;
                CurrentState = GameState.Roaming;
            }
        }

        public void EnterCombat(Enemy enemy)
        {
            // בדיקת בטיחות: וודא שהשחקן קיים והמצב תקין
            if (CurrentState != GameState.Roaming || Player == null) return;

            CurrentState = GameState.Combat;

            string startMessage = Combat.StartNewRound(Player, enemy);

            Console.WriteLine(startMessage);
            Console.WriteLine($"Switching from Roaming to Combat against {enemy.Name}!");
        }


        public void ExitCombat()
        {
            if (CurrentState == GameState.Combat && !Combat.IsCombatActive)
            {
                CurrentState = GameState.Roaming;
                Console.WriteLine("Returning to Roaming world.");

                // שמירה אוטומטית רק אם השחקן קיים
                if (Player != null)
                {
                    DatabaseManager.SaveGame(Player);
                }
            }
        }
        public void TogglePause()
        {
            if (CurrentState == GameState.Roaming)
                CurrentState = GameState.Paused;
            else if (CurrentState == GameState.Paused)
                CurrentState = GameState.Roaming;
        }
    }
}
