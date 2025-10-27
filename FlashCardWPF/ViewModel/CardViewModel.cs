using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlashCardWPF.Model;

namespace FlashCardWPF.ViewModel
{
    public class CardViewModel
    {
        public Deck CurrentDeck { get;  }
        public CardViewModel(Deck deck)
        {
            CurrentDeck = deck;
        }
    }
}
