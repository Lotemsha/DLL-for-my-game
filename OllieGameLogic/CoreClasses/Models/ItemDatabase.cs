using CoreClasses.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class ItemDatabase
{
    private static Dictionary<int, Item> _itemsByID = new Dictionary<int, Item>();
    private static Dictionary<string, Item> _itemsByName = new Dictionary<string, Item>();

    public static IReadOnlyDictionary<int, Item> ItemsByID => _itemsByID;
    public static IReadOnlyDictionary<string, Item> ItemsByName => _itemsByName;

    public static void Initialize(string jsonContent)
    {
        var items = JsonConvert.DeserializeObject<List<Item>>(jsonContent);

        if (items != null)
        {
            _itemsByID = items.ToDictionary(i => i.ItemID, i => i);
            _itemsByName = items.ToDictionary(i => i.Name, i => i);
        }
        else
        {
            _itemsByID = new Dictionary<int, Item>();
            _itemsByName = new Dictionary<string, Item>();
        }
    }

    public static void LoadFromFile(string filePath = "items.json")
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Initialize(json);
        }
    }

    public static Item GetByID(int id)
    {
        return _itemsByID.TryGetValue(id, out var item) ? item : null;
    }

    public static Item GetByName(string name)
    {
        return _itemsByName.TryGetValue(name, out var item) ? item : null;
    }

    public static Item GetRandom()
    {
        var consumables = _itemsByID.Values
            .Where(i => i.Type == ItemType.Consumable)
            .ToList();

        if (consumables.Count == 0)
            return null;

        var random = new System.Random();
        return consumables[random.Next(consumables.Count)];
    }
}