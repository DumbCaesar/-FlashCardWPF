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
        private const int MAX_NEW_CARDS_PER_SESSION = 10; // Amount of new cards to be seen per session
        private readonly StatsService _statsService;
        private DailyStats _dailyStats;
        private DateTime _sessionStartTime;
        private bool _areAnswersVisible; // flag to indicate if answer should be hidden
        private bool _showButtonHidden; // flag to indicate if Show button should be hidden
        private Card _currentCard; // Current displayed card
        private bool _deckFinished; // flag to indicate if there are cards left in the deck

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

        public ICommand ShowAnswersCommand { get; } // Command to show answer

        public ICommand NextQuestionCommand { get; } // Command to go to the next question

        public Deck CurrentDeck { get; } // Deck object for the deck selected from MainView.
        public PriorityQueue<Card, DateTime> LearningCards { get; set; } = new PriorityQueue<Card, DateTime>(); // Queue sorted by NextReview
        public Queue<Card> ReviewCards { get; set; } // Cards due for review
        public Queue<Card> NewCards { get; set; } // New cards for session
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
            // Initializes card view
            // Create stats session
            _statsService = new StatsService();
            _dailyStats = _statsService.LoadTodayStats();

            // Load the deck, create queues and set current card
            CurrentDeck = Deck.LoadDeck(deckName);
            ReviewCards = CreateReviewDeck(CurrentDeck);
            NewCards = CreateNewCardDeck(CurrentDeck);
            CurrentCard = SetNextCard();

            // Command handlers
            ShowAnswersCommand = new RelayCommand(_ => ShowAnswers());
            NextQuestionCommand = new RelayCommand(param => GoToNextQuestion(param));

            // Check if there are cards in the deck 
            if (_deckFinished) ShowButtonHidden = true;
            Debug.WriteLine($"Deck finished: {_deckFinished}");
            Debug.WriteLine($"ShowButtonHidden value: {ShowButtonHidden}");
            _sessionStartTime = DateTime.Now;
        }

        private void ShowAnswers()
        {
            // set visibility of show button and answers when answer is shown
            ShowButtonHidden = true;
            AreAnswersVisible = true;
        }

        private void GoToNextQuestion(object param)
        {
            AreAnswersVisible = false; // hide answer

            Debug.WriteLine($"Caller is {param}");
            Button button = (Button)param;
            string caller = button.Content.ToString()!;
            UpdateCardScheduling(caller); // update spaced repetion data for card

            CurrentDeck.SaveDeck(); // save user progress 
            SaveSessionStats();

            CurrentCard = SetNextCard(); // update CurrentCard to next card
            if (!_deckFinished) ShowButtonHidden = false; // make Show button visible if there are still cards left in the deck

            _sessionStartTime = DateTime.Now;
        }

        private void SaveSessionStats()
        {
            TimeSpan cardTime = DateTime.Now - _sessionStartTime;
            _statsService.UpdateStats(1, cardTime);

            // Reload stats to update UI
            _dailyStats = _statsService.LoadTodayStats();
            OnPropertyChanged(nameof(StudySummary));

            Debug.WriteLine(
            $"Card reviewed in {cardTime:mm\\:ss}"
            );
        }

        private void UpdateCardScheduling(string rating)
        {
            DateTime tomorrow = DateTime.Now.AddDays(1); // get datetime for now + 24 hours before setting nextreview to avoid +1 day review being re-added to queue
            if (CurrentCard.IsNew)
            {
                UpdateNewCardScheduling(rating);
                CurrentCard.IsNew = false;  // update new cards to seen after user has reviewed once
            }
            else
            {
                UpdateReviewCardScheduling(rating);
            }
            if (CurrentCard.NextReview <= tomorrow) LearningCards.Enqueue(CurrentCard, CurrentCard.NextReview); // Adds card to learning queue if next review was within a day
        }

        private void UpdateNewCardScheduling(string rating)
        { 
            // Basic Anki SM2 implementation for new cards
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
            // Basic Anki SM2 implementation for cards that user has seen before
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
            var newCards = deck.Cards.Where(c => !c.IsNew && DateTime.Now >= c.NextReview); // Get cards that are not new and due for review
            var reviewQueue = new Queue<Card>(newCards); // Add cards to queue
            Debug.WriteLine($"{reviewQueue.Count()} cards in Review Deck");
            return reviewQueue;
        }

        public Queue<Card> CreateNewCardDeck(Deck deck)
        {
            var newCards = deck.Cards.Where(c => c.IsNew).Take(MAX_NEW_CARDS_PER_SESSION); // Get specified number of cards where IsNew is true
            var newQueue = new Queue<Card>(newCards); // Add new cards to queue
            Debug.WriteLine($"{newQueue.Count()} cards in New Deck");
            return new Queue<Card>(newCards);
        }

        public Card SetNextCard()
        {
            // Check for due card in learning deck and return if it is due
            if (LearningCards.Count > 0 &&
                LearningCards.TryPeek(out Card topCard, out DateTime priority) &&
                DateTime.Now >= priority)
            {
                return LearningCards.Dequeue();
            }
            
            if (ReviewCards.Count > 0) return ReviewCards.Dequeue(); // get next review card
            if (NewCards.Count > 0) return NewCards.Dequeue(); // get next new card
            if (LearningCards.Count > 0) return LearningCards.Dequeue(); // if no due cards in learning, no review, no next then get next from learning
            _deckFinished = true;
            return new Card() { Front = "Congrats!" }; // Card used to indicate to user that deck is finished
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

