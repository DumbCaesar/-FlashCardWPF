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
using System.Windows.Input;
using FlashCardWPF.Model;

namespace FlashCardWPF.ViewModel
{
    public class BrowseViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> ListOfDecks { get; set; }
        public ICommand SaveCardCommand { get; set; }
        private ObservableCollection<Card> _listOfCards;
        public string CurrentDeckName { get; set; }
        private Card _selectedCard;
        private int _selectedIndex;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    OnPropertyChanged();

                    CurrentDeckName = ListOfDecks[_selectedIndex];
                    ListOfCards = GetCards();
                    Debug.WriteLine($"Changed deck to {CurrentDeckName}");
                }
            }
        }
        public Card SelectedCard 
        {
            get => _selectedCard;
            set
            {
                if (_selectedCard != value)
                {
                    _selectedCard = value;
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
            ListOfDecks = GetDeckNames();
            SelectedIndex = 0;
            CurrentDeckName = ListOfDecks[0];
            ListOfCards = GetCards();
            SaveCardCommand = new RelayCommand(_ => SaveQuestion());
        }

        private void SaveQuestion()
        {
            Deck deck = new Deck(SelectedCard.DeckName);
            if (SelectedIndex == 0)
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

        public ObservableCollection<string> GetDeckNames()
        {
            ObservableCollection<string> deckNames = new ObservableCollection<string>();
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataPath = Path.Combine(projectRoot, "Data/Decks");
            string[] files = Directory.GetFiles(dataPath);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                Debug.WriteLine($"Adding {fileName} to list...");
                deckNames.Add(fileName);
            }
            Debug.WriteLine($"Added {deckNames.Count} decks to list");
            deckNames.Insert(0, "All Decks");
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
            for (int i = 1; i < ListOfDecks.Count; i++)
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
