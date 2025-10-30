using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FlashCardWPF.Model;
using System.Windows.Input;
using System.Windows.Controls;
using FlashCardWPF.Services;

namespace FlashCardWPF.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        private const int MAX_NEW_CARDS_PER_SESSION = 10;
        private readonly StatsService _statsService;
        private DailyStats _dailyStats;
        private DateTime _sessionStartTime;
        private int _sessionCardCount = 0;
        private bool _areAnswersVisible;
        private bool _showButtonHidden;
        private Card _currentCard;
        private bool _deckFinished;

        public string StudySummary =>
       $"Total: {_dailyStats.TotalStudyTime:mm\\:ss} | " +
       $"Per card: {_dailyStats.AverageTimePerCard:ss\\.ff}s | " +
       $"Cards: {_dailyStats.TotalCards}";


        public bool ShowButtonHidden
        {
            get => _showButtonHidden;
            set
            {
                if (_showButtonHidden != value)
                {
                    _showButtonHidden = value;
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
            _statsService = new StatsService();
            _dailyStats = _statsService.LoadTodayStats();

            CurrentDeck = Deck.LoadDeck(deckName);
            ShowAnswersCommand = new RelayCommand(_ => ShowAnswers());
            NextQuestionCommand = new RelayCommand(param => GoToNextQuestion(param));
            ReviewCards = CreateReviewDeck(CurrentDeck);
            NewCards = CreateNewCardDeck(CurrentDeck);
            CurrentCard = SetNextCard();
            if (_deckFinished) ShowButtonHidden = true;
            Debug.WriteLine($"Deck finished: {_deckFinished}");
            Debug.WriteLine($"ShowButtonHidden value: {ShowButtonHidden}");
            _sessionStartTime = DateTime.Now;
            OnPropertyChanged(nameof(StudySummary));
        }

        private void ShowAnswers()
        {
            ShowButtonHidden = true;
            AreAnswersVisible = true;
        }

        private void GoToNextQuestion(object param)
        {
            AreAnswersVisible = false;
            _sessionCardCount++;

            Debug.WriteLine($"Caller is {param}");
            Button button = (Button)param;
            string caller = button.Content.ToString()!;
            UpdateCardScheduling(caller);

            CurrentDeck.SaveDeck();
            SaveSessionStats();

            CurrentCard = SetNextCard();
            if (!_deckFinished) ShowButtonHidden = false;
        }

        private void SaveSessionStats()
        {
            TimeSpan sessionTime = DateTime.Now - _sessionStartTime;
            _statsService.UpdateStats(_sessionCardCount, sessionTime);

            // Reload stats to update UI
            _dailyStats = _statsService.LoadTodayStats();
            OnPropertyChanged(nameof(StudySummary));

            Debug.WriteLine(
                $"Session complete: {_sessionCardCount} cards in {sessionTime:mm\\:ss}"
            );
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
            if (CurrentCard.NextReview <= DateTime.Now.AddDays(1)) LearningCards.Enqueue(CurrentCard, CurrentCard.NextReview);
        }

        private void UpdateNewCardScheduling(string rating)
        { 
            // Basic Anki SM-2 algorithm
            switch (rating)
            {
                case "Again":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(SpacedRepetitionConstants.AGAIN_NEW_CARD_MINUTES);
                    break;
                case "Hard":
                    CurrentCard.NextReview = DateTime.Now.AddMinutes(SpacedRepetitionConstants.HARD_NEW_CARD_MINUTES);
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
                    break;
                case "Hard":
                    interval = interval == 0 ? SpacedRepetitionConstants.GOOD_INITIAL_INTERVAL : (int)(interval * SpacedRepetitionConstants.HARD_INTERVAL_MULTIPLIER);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    CurrentCard.EaseFactor = Math.Max(SpacedRepetitionConstants.MIN_EASE_FACTOR, easeFactor - SpacedRepetitionConstants.HARD_EASE_PENALTY);
                    break;
                case "Good":
                    interval = interval == 0 ? SpacedRepetitionConstants.GOOD_INITIAL_INTERVAL : (int)(interval * easeFactor);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    break;
                case "Easy":
                    interval = interval == 0 ? SpacedRepetitionConstants.EASY_INITIAL_INTERVAL : (int)(interval * easeFactor);
                    CurrentCard.NextReview = DateTime.Now.AddDays(interval);
                    CurrentCard.Interval = interval;
                    CurrentCard.EaseFactor = easeFactor + SpacedRepetitionConstants.EASY_EASE_BONUS;
                    break;
            }
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
            
            if (ReviewCards.Count > 0) return ReviewCards.Dequeue(); // get next review card
            if (NewCards.Count > 0) return NewCards.Dequeue(); // get next new card
            if (LearningCards.Count > 0) return LearningCards.Dequeue(); // if no due cards in learning, no review, no next then get next from learning
            _deckFinished = true;
            return new Card() { Front = "Congrats!" };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

