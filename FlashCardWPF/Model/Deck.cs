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
    /// <summary>
    /// Class used to represent a deck of flash cards
    /// </summary>
    public class Deck
    {
        public string? Name { get; set; } // Name of deck. Used as filename and for UI
        public List<Card> Cards { get; set; } = new List<Card>(); // List of Card objects for each flash card belonging to the deck

        public Deck() { } // constructor used for deserialization
        public Deck(string name) // constructor used for creating new decks
        {
            Name = name;
        }

        public static Deck LoadDeck(string deckName)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."); // Navigate from bin/Debug/net9.0 up to project root
            string dataDir = Path.Combine(baseDir, "Data/Decks");
            string filePath = Path.Combine(dataDir, $"{deckName}.json");
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var deck = JsonSerializer.Deserialize<Deck>(json, options); // deserialize json
            if (deck != null)
            {
                deck.Name = deckName;
                foreach (var card in deck.Cards)
                {
                    card.DeckName = deckName; // add name of deck to each card
                }
            }
            return deck ?? throw new InvalidOperationException("Failed to load deck");
        }

        public void SaveDeck()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."); // Navigate from bin/Debug/net9.0 up to project root
            string dataDir = Path.Combine(baseDir, "Data/Decks");
            string filePath = Path.Combine(dataDir, $"{Name}.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(this, options); // serialize deck object into json
            File.WriteAllText(filePath, json); // write json to data/decks/deckname.json
            Debug.WriteLine($"Deck saved to {filePath}");
        }
    }
}
