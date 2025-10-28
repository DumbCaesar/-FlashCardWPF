using FlashCardWPF.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlashCardWPF.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public Deck CurrentDeck { get; }
        public Deck ReviewDeck { get; set; }
        public Card CurrentCard { get; set; }

        public CardViewModel(Deck deck)
        {
            CurrentDeck = deck;
            ReviewDeck = CreateReviewDeck(CurrentDeck);
            CurrentCard = SetNextCard(ReviewDeck);
        }

        public Deck CreateReviewDeck(Deck deck)
        {
            Deck newDeck = new Deck("test");
            int newCardCounter = 0;
            int i = 0;
            while (i < deck.Cards.Count)
            {
                Card card = deck.Cards[i];
                if (card.IsNew && newCardCounter < 10)
                {
                    newDeck.Cards.Add(card);
                    newCardCounter++;
                }
                else if (card.NextReview > DateTime.Now) newDeck.Cards.Add(card);
                i++;
            }
            return newDeck;
        }

        public Card SetNextCard(Deck deck)
        {
            return deck.Cards[0];
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

