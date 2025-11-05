using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FlashCardWPF.Model;

namespace FlashCardWPF.ViewModel
{
    public class NewDeckViewModel : INotifyPropertyChanged
    {

        // Fields for input binding 
        private string _deckName;
        private string _currentQuestion;
        private string _currentAnswer;
        private Card _selectedCard;

        // The Deck that holds the temp date for the new deck, until saved to Data/Decks
        private Deck NewDeck { get; set; } = new Deck();

        // This event is used to update the ListBox inside the MainWindow. It get's invoked upon a new deck being created, (trough the MainWindow) and then updates the UI in real time
        public event Action<string>? DeckCreated;

        // Currently SelectedCard in the UI
        public Card SelectedCard
        {
            get => _selectedCard;
            set
            {
                if(_selectedCard  != value)
                {
                    _selectedCard = value;
                    OnPropertyChanged();
                    Debug.WriteLine($"Selcted card is: {_selectedCard?.Front}");
                }
            }
        }

        // Name of the new Deck
        public string DeckName 
        {
            get => _deckName;
            set 
            {
                if (_deckName != value)
                {
                    _deckName = value;
                    OnPropertyChanged();
                }
            }
        }

        // current question input 
        public string CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                if (_currentQuestion != value)
                {
                    _currentQuestion = value;
                    OnPropertyChanged();
                }
            }
        }

        // current answer input
        public string CurrentAnswer
        {
            get => _currentAnswer;
            set
            {
                if (_currentAnswer != value)
                {
                    _currentAnswer = value;
                    OnPropertyChanged();
                }
            }
        }

        // ObservableCollection holds the Cards, and refreshes the ListBox automatically to display the new Card when added.
        public ObservableCollection<Card> Deck {  get; set; }

        // Commands for UI buttons. Neccesarry in MVVM.
        public ICommand CreateDeckCommand { get; set; }
        public ICommand AddQuestionCommand { get; set; }
        public ICommand DeleteQuestionCommand { get; set; }

        public NewDeckViewModel()
        {
            // init deck and commands, initilized upon opening the newdeck in the App.
            Deck = new ObservableCollection<Card>();
            CreateDeckCommand = new RelayCommand(_ => CreateDeck());
            AddQuestionCommand = new RelayCommand(_ => AddQuestion());
            DeleteQuestionCommand = new RelayCommand(_ => DeleteQuestion());
        }

        private void CreateDeck()
        {
            // Deck must have at least one card to be created.
            if(string.IsNullOrEmpty(DeckName) || Deck.Count == 0)
            {
                MessageBox.Show("A minimum of one question is needed to create a deck!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<Card> deck = new List<Card>();

            try
            {
                // Copy observable collection to List
                foreach (Card card in Deck)
                {
                    deck.Add(card);
                }

                // Assign data + save to .json
                NewDeck.Name = DeckName;
                NewDeck.Cards = deck;
                NewDeck.SaveDeck();
                DeckCreated?.Invoke(DeckName);
                DeckName = null!;
                MessageBox.Show("Deck created successfully!", "Success", MessageBoxButton.OK);
                Debug.WriteLine("Deck creation successful!");
                
            }

            catch(Exception ex)
            {
                // Error handling.
                MessageBox.Show(
                $"Failed to save deck: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
                Debug.WriteLine(ex);
            }            
        }

        private void AddQuestion()
        {
            // Only add card when both of the card inut fields has a value, not null.
            if(CurrentQuestion != null && CurrentAnswer != null)
            {
                Card card = new Card(CurrentQuestion, CurrentAnswer);
                Deck.Add(card);
                Debug.WriteLine("Card creation succesfull!");
                Debug.WriteLine($"Front: {CurrentQuestion} Back: {CurrentAnswer}");
                CurrentAnswer = null!;
                CurrentQuestion = null!;
            }
        }

        private void DeleteQuestion()
        {
            // Remove selected card
            if (SelectedCard != null)
            {
                Deck.Remove(SelectedCard);
            }
            else
            {
                MessageBox.Show("You must select a question from Q/A to remove.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Property changed event for data binding
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
