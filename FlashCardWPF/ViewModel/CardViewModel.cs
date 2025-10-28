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
using System.Windows.Controls;

namespace FlashCardWPF.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        private int _pos = 0;

        private bool _areAnswersVisible;

        private Card _currentCard;

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

        public ICommand NextQuestionCommand { get; }

        public Deck CurrentDeck { get; }
        public Deck ReviewDeck { get; set; }
        public Card CurrentCard
        {
            get => _currentCard;
            set
            {
                if (_currentCard != value)
                {
                    _currentCard = value;
                    OnPropertyChanged();
                }
            }
        }

        public CardViewModel(string deckName)
        {
            CurrentDeck = LoadDeck(deckName);
            ShowAnswersCommand = new RelayCommand(_ => ShowAnswers());
            NextQuestionCommand = new RelayCommand(param => GoToNextQuestion(param));
            ReviewDeck = CreateReviewDeck(CurrentDeck);
            Debug.WriteLine(ReviewDeck.Cards.Count);
            CurrentCard = SetNextCard(ReviewDeck);
        }

        private void ShowAnswers()
        {
            AreAnswersVisible = true;
        }

        private void GoToNextQuestion(object param)
        {
            Debug.WriteLine($"Caller is {param}");
            Button button = (Button)param;
            string caller = button.Content.ToString()!;
            


            AreAnswersVisible = false;
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
                else if (!card.IsNew && DateTime.Now >= card.NextReview) newDeck.Cards.Add(card);
                i++;
            }
            return newDeck;
        }

        public Card SetNextCard(Deck deck)
        {
            for (int i = _pos; i < deck.Cards.Count;)
            {
                _pos++;
                return deck.Cards[i];
            }
            return null;
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

