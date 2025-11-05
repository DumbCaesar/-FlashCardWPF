using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    /// <summary>
    /// Class used to represent a single flash card with data used for spaced repetition
    /// </summary>
    public class Card
    {
        public string? Front {  get; set; } // Card question
        public string? Back { get; set; } // Card answer
        public string? DeckName { get; set; } // Optional: Which deck the card belongs to
        public int? Interval { get; set; } // Time between reviews. Used for SM2 algorithm
        public double? EaseFactor { get; set; } // Card difficulty multiplier. Used for SM2 algorithm

        public DateTime NextReview { get; set; } = DateTime.Now; // When the card should be reviewed next (default to now for new cards)
        public bool IsNew { get; set; } = true; // Whether the user has reviewed the card before 

        // Constructor used for deserialization
        public Card() { }

        // Constructor for creating new cards
        public Card(string question, string answer, string? deckName = null)
        {
            Front = question;
            Back = answer;
            DeckName = deckName;
        }
    }
}
