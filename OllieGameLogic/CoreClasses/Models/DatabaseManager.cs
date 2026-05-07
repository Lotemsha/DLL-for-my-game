using System;
using System.IO;
using Newtonsoft.Json;

namespace CoreClasses.Models
{
    public static class DatabaseManager
    {
        private static string _savePath = "";

        public static void SetSavePath(string folderPath)
        {
            _savePath = Path.Combine(folderPath, "OliSaveGame.json");
        }

        public static void SaveGame(PlayerManager player)
        {
            if (_savePath == "")
            {
                Console.WriteLine("Save path not set!");
                return;
            }

            try
            {
                string jsonString = JsonConvert.SerializeObject(player, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                File.WriteAllText(_savePath, jsonString);
                Console.WriteLine("Game Saved Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save Failed: {ex.Message}");
            }
        }

        public static PlayerManager? LoadGame()
        {
            if (!File.Exists(_savePath))
            {
                Console.WriteLine("No save file found.");
                return null;
            }

            try
            {
                string jsonString = File.ReadAllText(_savePath);

                // שימוש ב-Newtonsoft לטעינה
                PlayerManager? loadedPlayer = JsonConvert.DeserializeObject<PlayerManager>(jsonString);

                if (loadedPlayer != null)
                {
                    Console.WriteLine("Game Loaded Successfully!");
                }

                return loadedPlayer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load Failed: {ex.Message}");
                return null;
            }
        }
    }
}
