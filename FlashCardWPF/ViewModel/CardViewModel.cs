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
        public Queue<Card> LearningCards { get; set; } = new Queue<Card>();
        public Queue<Card> ReviewCards { get; set; }
        public Queue<Card> NewCards { get; set; }
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
            ReviewCards = CreateReviewDeck(CurrentDeck);
            NewCards = CreateNewCardDeck(CurrentDeck);
            CurrentCard = SetNextCard();
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

            switch (caller)
            {
                case "Again":
                    break;
                case "Hard":
                    break;
                case "Good":
                    break;
                case "Easy":
                    break;
            }



            AreAnswersVisible = false;
            CurrentCard = SetNextCard();
        }

        public Queue<Card> CreateReviewDeck(Deck deck)
        {
            Queue<Card> newDeck = new Queue<Card>();
            int i = 0;
            while (i < deck.Cards.Count)
            {
                Card card = deck.Cards[i];
                if (!card.IsNew && DateTime.Now >= card.NextReview) newDeck.Enqueue(card);
                i++;
            }
            Debug.WriteLine($"{newDeck.Count} cards in Review Deck");
            return newDeck;
        }

        public Queue<Card> CreateNewCardDeck(Deck deck)
        {
            Queue<Card> newDeck = new Queue<Card>();
            int newCardCounter = 0;
            int i = 0;
            while (i < deck.Cards.Count)
            {
                Card card = deck.Cards[i];
                if (card.IsNew && newCardCounter < 10)
                {
                    newDeck.Enqueue(card);
                    newCardCounter++;
                }
                i++;
            }
            Debug.WriteLine($"{newDeck.Count} cards in New Deck");
            return newDeck;
        }

        public Card SetNextCard()
        {
            Card card = null;
            if (LearningCards.Count != 0) card = LearningCards.Dequeue();
            else if (ReviewCards.Count != 0) card = ReviewCards.Dequeue();
            else if (NewCards.Count != 0) card = NewCards.Dequeue();
            Debug.WriteLine($"Queue returns card {card}");
            return card;
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
            return deck;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

