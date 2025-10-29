using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FlashCardWPF.Model;
using System.Windows.Input;
using System.Windows.Controls;

namespace FlashCardWPF.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        private DateTime _startTime;
        private int _cardCount = 0;
        private bool _areAnswersVisible;
        private Card _currentCard;
        private TimeSpan _studyTime;
        private TimeSpan _studyTimePerCard;

        public string StudySummary =>
        $"Total: {StudyTimeDeck:mm\\:ss} | Per card: {StudyTimePerCardDeck:ss\\.ff}s";

        public TimeSpan StudyTimePerCardDeck
        {
            get => _studyTimePerCard;
            set
            {
                if(_studyTimePerCard != value)
                {
                    _studyTimePerCard = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan StudyTimeDeck
        {
            get => _studyTime;
            set
            {
                if (_studyTime != value)
                {
                    _studyTime = value;
                    OnPropertyChanged();
                }
            }
        }

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
            _cardCount++;

            TimeSpan totalElapsed = StudyTime();
            TimeSpan totalTimePerCard = StudyTimePerCard(totalElapsed);
            StudyTimeDeck = totalElapsed;
            StudyTimePerCardDeck = totalTimePerCard;

            Debug.WriteLine($"Caller is {param}");
            Button button = (Button)param;
            string caller = button.Content.ToString()!;
            UpdateCardScheduling(caller);
            CurrentCard = SetNextCard();
        }

        private void UpdateCardScheduling(string rating)
        {
            if (CurrentCard.IsNew)
            {
                UpdateNewCardScheduling(rating);
                CurrentCard.IsNew = false;  // update new cards to seen after user has reviewed once
            }
            else
            {
                UpdateReviewCardScheduling(rating);
            }
        }

        private void UpdateNewCardScheduling(string rating)
        { 
            // Basic Anki SM-2 algorithm
            switch (rating)
            {
                case "Again":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(1);
                    LearningCards.Add(CurrentCard);
                    break;
                case "Hard":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(10);
                    LearningCards.Add(CurrentCard);
                    break;
                case "Good":
                    CurrentCard.NextReview = DateTime.Now.AddDays(1);
                    CurrentCard.Interval = 1;
                    CurrentCard.EaseFactor = 2.5;
                    break;
                case "Easy":
                    CurrentCard.NextReview = DateTime.Now.AddDays(4);
                    CurrentCard.Interval = 4;
                    CurrentCard.EaseFactor = 2.6;
                    break;
            }
        }

        private void UpdateReviewCardScheduling(string rating)
        {      
            double easeFactor = CurrentCard.EaseFactor ?? 2.5;
            int interval = CurrentCard.Interval ?? 1;
            switch (rating)
            {
                case "Again":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(10);
                    CurrentCard.Interval = 0;
                    CurrentCard.EaseFactor = Math.Max(1.3, easeFactor - 0.2);
                    LearningCards.Add(CurrentCard);
                    break;
                case "Hard":
                    interval = (int)(interval * 1.2);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    CurrentCard.EaseFactor = Math.Max(1.3, easeFactor - 0.15);
                    LearningCards.Add(CurrentCard);
                    break;
                case "Good":
                    interval = (int)(interval * easeFactor);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    break;
                case "Easy":
                    interval = (int)(interval * easeFactor);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    CurrentCard.EaseFactor = easeFactor + 0.15;
                    break;
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
                DateTime endTime = DateTime.Now;
                TimeSpan totalTime = endTime - _startTime;

                Debug.WriteLine($"Study time: {totalTime}");
                return totalTime;
            }
        }

        public TimeSpan StudyTimePerCard(TimeSpan totalTime)
        {
            if (_cardCount == 0)
            {
                return TimeSpan.Zero;
            }

            TimeSpan timePerCard = TimeSpan.FromSeconds(totalTime.TotalSeconds / _cardCount);
            Debug.WriteLine($"Study time per card: {timePerCard}");
            return timePerCard;
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
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // If either timing property changes, notify the UI that StudySummary also changed
            if (propertyName is nameof(StudyTimeDeck) or nameof(StudyTimePerCardDeck))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudySummary)));
            }
        }

        //public event PropertyChangedEventHandler? PropertyChanged;
        //protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        //    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

