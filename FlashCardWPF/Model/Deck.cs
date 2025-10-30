 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    public class Deck
    {
        public string? Name { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();

        public Deck() { }
        public Deck(string name)
        {
            Name = name;
        }

        public static Deck LoadDeck(string deckName)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataDir = Path.Combine(baseDir, "Data/Decks");
            string filePath = Path.Combine(dataDir, $"{deckName}.json");
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var deck = JsonSerializer.Deserialize<Deck>(json, options);
            if (deck != null)
            {
                deck.Name = deckName;
            }
            return deck ?? throw new InvalidOperationException("Failed to load deck");
        }

        public void SaveDeck()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataDir = Path.Combine(baseDir, "Data/Decks");
            string filePath = Path.Combine(dataDir, $"{Name}.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
            Debug.WriteLine($"Deck saved to {filePath}");
        }
    }
}
