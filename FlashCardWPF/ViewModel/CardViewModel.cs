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
        private const int MAX_NEW_CARDS_PER_SESSION = 10;
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
        public PriorityQueue<Card, DateTime> LearningCards { get; set; } = new PriorityQueue<Card, DateTime>();
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
            if (!HasCardsLeft()) SaveDeck();
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
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(SpacedRepetitionConstants.AGAIN_NEW_CARD_MINUTES);
                    LearningCards.Enqueue(CurrentCard, CurrentCard.NextReview);
                    break;
                case "Hard":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(SpacedRepetitionConstants.HARD_NEW_CARD_MINUTES);
                    LearningCards.Enqueue(CurrentCard, CurrentCard.NextReview);
                    break;
                case "Good":
                    CurrentCard.NextReview = DateTime.Now.AddDays(SpacedRepetitionConstants.GOOD_NEW_CARD_DAYS);
                    CurrentCard.Interval = SpacedRepetitionConstants.GOOD_INITIAL_INTERVAL;
                    CurrentCard.EaseFactor = SpacedRepetitionConstants.DEFAULT_EASE_FACTOR;
                    break;
                case "Easy":
                    CurrentCard.NextReview = DateTime.Now.AddDays(SpacedRepetitionConstants.EASY_NEW_CARD_DAYS);
                    CurrentCard.Interval = SpacedRepetitionConstants.EASY_INITIAL_INTERVAL;
                    CurrentCard.EaseFactor = SpacedRepetitionConstants.EASY_INITIAL_EASE_FACTOR;
                    break;
            }
        }

        private void UpdateReviewCardScheduling(string rating)
        {      
            double easeFactor = CurrentCard.EaseFactor ?? SpacedRepetitionConstants.DEFAULT_EASE_FACTOR;
            int interval = CurrentCard.Interval ?? SpacedRepetitionConstants.GOOD_INITIAL_INTERVAL;
            switch (rating)
            {
                case "Again":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(SpacedRepetitionConstants.AGAIN_REVIEW_MINUTES);
                    CurrentCard.Interval = 0;
                    CurrentCard.EaseFactor = Math.Max(SpacedRepetitionConstants.MIN_EASE_FACTOR, easeFactor - SpacedRepetitionConstants.AGAIN_EASE_PENALTY);
                    LearningCards.Enqueue(CurrentCard, CurrentCard.NextReview);
                    break;
                case "Hard":
                    interval = (int)(interval * SpacedRepetitionConstants.HARD_INTERVAL_MULTIPLIER);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    CurrentCard.EaseFactor = Math.Max(SpacedRepetitionConstants.MIN_EASE_FACTOR, easeFactor - SpacedRepetitionConstants.HARD_EASE_PENALTY);
                    LearningCards.Enqueue(CurrentCard, CurrentCard.NextReview);
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
                    CurrentCard.EaseFactor = easeFactor + SpacedRepetitionConstants.EASY_EASE_BONUS;
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
            var newCards = deck.Cards.Where(c => !c.IsNew && DateTime.Now >= c.NextReview);
            var reviewQueue = new Queue<Card>(newCards);
            Debug.WriteLine($"{reviewQueue.Count()} cards in Review Deck");
            return reviewQueue;
        }

        public Queue<Card> CreateNewCardDeck(Deck deck)
        {
            var newCards = deck.Cards.Where(c => c.IsNew).Take(MAX_NEW_CARDS_PER_SESSION);
            var newQueue = new Queue<Card>(newCards);
            Debug.WriteLine($"{newQueue.Count()} cards in New Deck");
            return new Queue<Card>(newCards);
        }

        public Card SetNextCard()
        {
            if (LearningCards.Count > 0 &&
                LearningCards.TryPeek(out Card topCard, out DateTime priority) &&
                DateTime.Now >= priority) // Check for due card in learning deck
            {
                return LearningCards.Dequeue();
            }
            
            if (ReviewCards.Count != 0) return ReviewCards.Dequeue(); // get next review card
            if (NewCards.Count != 0) return NewCards.Dequeue(); // get next new card
            if (LearningCards.Count != 0) return LearningCards.Dequeue(); // if no due cards in learning, no review, no next then get next from learning
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
            if (deck != null)
            {
                deck.Name = deckName;
            }
            return deck ?? throw new InvalidOperationException("Failed to load deck");
        }

        public void SaveDeck()
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataDir = Path.Combine(baseDir, "Data");
            string filePath = Path.Combine(dataDir, $"{CurrentDeck.Name}.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(CurrentDeck, options);
            File.WriteAllText(filePath, json);
            Debug.WriteLine($"Deck saved to {filePath}");
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

