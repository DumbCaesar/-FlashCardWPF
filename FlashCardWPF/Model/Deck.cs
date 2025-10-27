using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    public class Deck
    {
        public string Name { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();

        public Deck(string name)
        {
            Name = name;
        }
    }
}
