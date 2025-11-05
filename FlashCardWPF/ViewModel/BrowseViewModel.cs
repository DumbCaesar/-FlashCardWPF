using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlashCardWPF.Model;

namespace FlashCardWPF.ViewModel
{
    /// <summary>
    ///  ViewModel for browsing and managing flashcards across decks in Data/Decks
    ///  Allows user to view, create, edit and delete flash cards from all their decks and delete decks.
    /// </summary>
    public class BrowseViewModel : INotifyPropertyChanged
    {
        // Store original card values if user creates new card instead of editing an existing card
        private string _originalFront;
        private string _originalBack;

        private Card _selectedCard;
        private int _selectedIndex; // Index of selected deck in Combobox
        private ObservableCollection<Card> _listOfCards;
        private ObservableCollection<string> _listOfDecks;
        public string CurrentDeckName { get; set; }
        public event Action? DeckDeleted; // Notifies MainView when a deck is deleted for updating the UI

        public ICommand SaveCardCommand { get; set; } // Command to save changes to selected card
        public ICommand DeleteCardCommand { get; set; } // Command to delete the selected card from deck
        public ICommand CreateNewCardCommand { get; set; } // Command to create new card
        public ICommand DeleteDeckCommand { get; set; } // Command to delete the deck selected in the combobox

        public ObservableCollection<string> ListOfDecks
        {
            get => _listOfDecks;
            set
            {
                if (_listOfDecks != value)
                {
                    _listOfDecks = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    Debug.WriteLine($"New SelectedIndex: {_selectedIndex}");
                    OnPropertyChanged();

                    // Deleting a deck sets index to -1
                    // Check that index is valid before trying to access ListOfDecks
                    if (_selectedIndex >= 0)
                    {
                        CurrentDeckName = ListOfDecks[_selectedIndex];
                        ListOfCards = GetCards();
                        Debug.WriteLine($"Changed deck to {CurrentDeckName}");
                    }
                }
            }
        }
        public Card SelectedCard 
        {
            get => _selectedCard;
            set
            {
                _selectedCard = value;

                if (_selectedCard != null)
                {
                    // Store original values in case user creates new card instead of saving edits
                    _originalFront = value.Front;
                    _originalBack = value.Back;
                    OnPropertyChanged();
                    Debug.WriteLine($"Selected card is: {_selectedCard.Front}");
                }
            }
        }

        public ObservableCollection<Card> ListOfCards
        {
            get => _listOfCards;
            set
            {
                if (_listOfCards != value)
                {
                    _listOfCards = value;
                    OnPropertyChanged();
                }
            }
        }


        public BrowseViewModel()
        {
            // Initialize deck list and set "All Decks" as default
            ListOfDecks = GetDeckNames();
            SelectedIndex = 0;
            CurrentDeckName = ListOfDecks[0];
            ListOfCards = GetCards();

            // Command handlers
            SaveCardCommand = new RelayCommand(_ => SaveQuestion());
            CreateNewCardCommand = new RelayCommand(_ => CreateNewCard());
            DeleteCardCommand = new RelayCommand(_ => DeleteCard());
            DeleteDeckCommand = new RelayCommand(_ => DeleteDeck());
        }

        private void DeleteDeck()
        {
            if (SelectedIndex == 0) // SelectedIndex 0 is "All Decks" which is not an actual deck
            {
                ConfirmDeleteAllDecks();
                SelectedIndex = -1; // Trigger refresh
            }
            else
            {
                ConfirmDeleteDeck();
            }
            ListOfDecks = GetDeckNames();
            SelectedIndex = 0; // Set the view back to All Decks when a deck is deleted

            DeckDeleted?.Invoke(); // Notify MainView so that the deleted deck is removed from there to prevent user from trying to load a file that does not exist
        }

        private void ConfirmDeleteAllDecks()
        {
            // Messagebox for user to confirm their choice, preventing accidental deletion of all decks
            if (MessageBox.Show($"Are you sure you want to delete ALL decks?",
               "Confirm Delete All",
               MessageBoxButton.YesNo,
               MessageBoxImage.Question) == MessageBoxResult.Yes) 
            {
                string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."); // Navigate from bin/Debug/net9.0 up to project root
                string dataPath = Path.Combine(projectRoot, "Data/Decks");
                string[] files = Directory.GetFiles(dataPath);
                Debug.WriteLine("Deleting ALL decks");
                foreach (string file in files)
                    File.Delete(file);
            }
        }

        private void ConfirmDeleteDeck()
        {
            // Messagebox for user to confirm their choice, preventing accidental deletion of a deck
            if (MessageBox.Show($"Are you sure you want to delete {ListOfDecks[SelectedIndex]}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."); // Navigate from bin/Debug/net9.0 up to project root
                string dataPath = Path.Combine(projectRoot, "Data/Decks");
                string filePath = Path.Combine(dataPath, ListOfDecks[SelectedIndex] + ".json"); // Add file extension
                Debug.WriteLine($"Deleting {filePath}");
                File.Delete(filePath);
            }
        }
        private void SaveQuestion()
        {
            Deck deck = new Deck(SelectedCard.DeckName);
            if (SelectedIndex == 0) // If user has all decks selected in the combobox, find the appropriate deck for the selected card
            {
                deck.Cards = ListOfCards.Where(c => c.DeckName == SelectedCard.DeckName).ToList();
            }
            else
            {
                deck.Cards = ListOfCards.ToList();
            }
            Debug.WriteLine($"Front: {SelectedCard.Front}");
            Debug.WriteLine("Saving question...");
            deck.SaveDeck();

        }

        private void DeleteCard()
        {
            Deck deck = new Deck(SelectedCard.DeckName);
            if(SelectedCard == null) // Check that a card is selected
            {
                MessageBox.Show("You must select a card to delete", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Remove card and save changes
            ListOfCards.Remove(SelectedCard);
            deck.Cards = ListOfCards.ToList();
            deck.SaveDeck();
        }

        private void CreateNewCard()
        {
            if (SelectedIndex == 0) // Prevent user from trying to add a card to All Decks which is not an actual deck
            {
                MessageBox.Show("You must select a deck to create a new card.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedCard == null)
            {
                MessageBox.Show("You must select a card to create a new one.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newCard = new Card
            {
                Front = SelectedCard.Front,
                Back = SelectedCard.Back,
                DeckName = CurrentDeckName,
                IsNew = true,
                NextReview = DateTime.Now
            };

            ListOfCards.Add(newCard);

            // Restore to original values
            SelectedCard.Front = _originalFront;
            SelectedCard.Back = _originalBack;

            // Force ObservableCollection to refresh the UI
            // OnPropertyChanged(nameof(CurrentCard) does not refresh collection
            var temp = SelectedCard;
            int index = ListOfCards.IndexOf(temp);
            ListOfCards.RemoveAt(index);
            ListOfCards.Insert(index, temp);
            SelectedCard = temp;

            Deck deck = new Deck(CurrentDeckName);
            deck.Cards = ListOfCards.ToList();
            deck.SaveDeck();
        }

        public ObservableCollection<string> GetDeckNames()
        {
            ObservableCollection<string> deckNames = new ObservableCollection<string>();
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."); // Navigate from bin/Debug/net9.0 up to project root
            string dataPath = Path.Combine(projectRoot, "Data/Decks");
            string[] files = Directory.GetFiles(dataPath);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                Debug.WriteLine($"Adding {fileName} to list...");
                deckNames.Add(fileName);
            }
            Debug.WriteLine($"Added {deckNames.Count} decks to list");
            deckNames.Insert(0, "All Decks"); // Option for the combobox used to display cards from all decks
            return deckNames;
        }

        public ObservableCollection<Card> GetCards()
        {
            List<Card> newCardList = new List<Card>();
            if (SelectedIndex == 0) newCardList = LoadAllCards();
            else newCardList = LoadCurrentDeck(SelectedIndex);
            Debug.WriteLine($"Added {newCardList.Count} cards...");
            return new ObservableCollection<Card>(newCardList);
        }

        public List<Card> LoadAllCards()
        {
            Debug.WriteLine($"Loading all cards...");
            List<Card> allCards = new List<Card>();
            for (int i = 1; i < ListOfDecks.Count; i++) // Start at index 1 to skip "All Decks" which is not an actual deck
            {
                List<Card> deckCards = LoadCurrentDeck(i);
                allCards.AddRange(deckCards);
            }
            return allCards;
        }

        public List<Card> LoadCurrentDeck(int index)
        {
            Debug.WriteLine($"Loading {ListOfDecks[index]} deck...");
            Deck deck = Deck.LoadDeck(ListOfDecks[index]);
            return deck.Cards;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
