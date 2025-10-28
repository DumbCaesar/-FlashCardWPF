using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FlashCardWPF.Model;
using System.Windows.Input;

namespace FlashCardWPF.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        private bool _areAnswersVisible; 

        public bool AreAnswersVisible
        {
            get => _areAnswersVisible;
            set
            {
                if (_areAnswersVisible != value)
                {
                    _areAnswersVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowAnswersCommand { get; }

        public Deck CurrentDeck { get; }
        public Deck ReviewDeck { get; set; }
        public Card CurrentCard { get; set; }

        public CardViewModel(string deckName)
        {
            CurrentDeck = LoadDeck(deckName);
            ShowAnswersCommand = new RelayCommand(_ => ShowAnswers());
            ReviewDeck = CreateReviewDeck(CurrentDeck);
            Debug.WriteLine(ReviewDeck.Cards.Count);
            CurrentCard = SetNextCard(ReviewDeck);
        }

        private void ShowAnswers()
        {
            AreAnswersVisible = true;
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
                else if (!card.IsNew && DateTime.Now >= card.NextReview) newDeck.Cards.Add(card);
                i++;
            }
            return newDeck;
        }

        public Card SetNextCard(Deck deck)
        {
            return deck.Cards[0];
        }

        public Deck LoadDeck(string deckName)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataDir = Path.Combine(baseDir, "Data");
            string filePath = Path.Combine(dataDir, $"{deckName}.json");
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var deck = JsonSerializer.Deserialize<Deck>(json, options);
            Debug.WriteLine(deck.Cards.Count);
            return deck;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

