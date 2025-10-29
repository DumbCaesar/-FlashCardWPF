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
        private DateTime _startTime;
        private bool _sessionActive = true;
        private int _cardCount = 0;
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
        public List<Card> LearningCards { get; set; } = new List<Card>();
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
            _startTime = DateTime.Now;
        }

        private void ShowAnswers()
        {
            AreAnswersVisible = true;
        }

        private void GoToNextQuestion(object param)
        {
            AreAnswersVisible = false;

            Debug.WriteLine($"Caller is {param}");
            Button button = (Button)param;
            string caller = button.Content.ToString()!;

            switch (caller)
            {
                case "Again":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(1);
                    LearningCards.Add(CurrentCard);
                    break;
                case "Hard":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(2);
                    LearningCards.Add(CurrentCard);
                    break;
                case "Good":
                    CurrentCard.NextReview = DateTime.Now.AddDays(1);
                    break;
                case "Easy":
                    CurrentCard.NextReview = DateTime.Now.AddDays(2);
                    break;
            }

            if (CurrentCard.IsNew) CurrentCard.IsNew = false; // update new cards to seen after user has reviewed once

            CurrentCard = SetNextCard();
            TimeSpan studyTime = StudyTime();
            TimeSpan studyTimePerCard = StudyTimePerCard(studyTime);

            if (studyTime != TimeSpan.Zero)
            {
                Debug.WriteLine($"Seassion complet! Total time is: {studyTime}");
            }
            if (studyTimePerCard != TimeSpan.Zero)
            {
                Debug.WriteLine($"Seassion complet! Total time per card is: {studyTimePerCard}");
            }
        }

        public TimeSpan StudyTime()
        {
            if (HasCardsLeft())
            {
                return TimeSpan.Zero;
            }
            else
            {
                _sessionActive = false;
                DateTime endTime = DateTime.Now;
                TimeSpan totalTime = endTime - _startTime;

                Debug.WriteLine($"Study time: {totalTime}");
                return totalTime;
            }
        }

        public TimeSpan StudyTimePerCard(TimeSpan timeSpan)
        {
            if(HasCardsLeft())
            {
                _cardCount++;
                return TimeSpan.Zero;
            }
            else
            {
                if(_cardCount == 0)
                {
                    return TimeSpan.Zero;
                }

                TimeSpan timePerCard = TimeSpan.FromSeconds(timeSpan.TotalSeconds / _cardCount);
                return timePerCard;
            }

        }

        private bool HasCardsLeft()
        {
            return
                (LearningCards?.Count > 0) ||
                (ReviewCards?.Count > 0) ||
                (NewCards?.Count > 0);
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
            if (LearningCards.Count != 0)
            {
                // Check for due card
                for (int i = 0; i < LearningCards.Count; i++)
                {
                    if (DateTime.Now > LearningCards[i].NextReview)
                    {
                        card = LearningCards[i];
                        LearningCards.RemoveAt(i);
                        return card;
                    }
                }
            }
            
            if (ReviewCards.Count != 0)
            {
                card = ReviewCards.Dequeue(); // get next review card
                return card;
            }
            
            if (NewCards.Count != 0)
            {
                card = NewCards.Dequeue(); // get next new card
                return card;
            } 
            
            if (LearningCards.Count != 0)  // if no due cards in learning, no review, no next then get next from learning
            {
                card = LearningCards[0];
                for (int i = 0; i < LearningCards.Count; i++) // get lowest review date
                {
                    if (LearningCards[i].NextReview < card.NextReview) card = LearningCards[i];
                }
                LearningCards.Remove(card);
                return card;
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
            return deck;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

