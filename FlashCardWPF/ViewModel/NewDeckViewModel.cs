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
using System.Threading.Tasks;
using System.Windows.Input;
using FlashCardWPF.Model;

namespace FlashCardWPF.ViewModel
{
    public class NewDeckViewModel : INotifyPropertyChanged
    {
        private string _deckName;
        private string _currentQuestion;
        private string _currentAnswer;
        private Deck NewDeck { get; set; } = new Deck();

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

        public ObservableCollection<Card> Deck {  get; set; }

        public ICommand CreateDeckCommand { get; set; }
        public ICommand AddQuestionCommand { get; set; }
        public ICommand DeleteQuestionCommand { get; set; }

        public NewDeckViewModel()
        {
            Deck = new ObservableCollection<Card>();
            CreateDeckCommand = new RelayCommand(_ => CreateDeck());
            AddQuestionCommand = new RelayCommand(_ => AddQuestion());
            DeleteQuestionCommand = new RelayCommand(_ => DeleteQuestion());
        }

        private void CreateDeck()
        {
            List<Card> deck = new List<Card>();
            if(DeckName != null)
            {
                foreach (var card in Deck)
                {
                    deck.Add(card);
                    Debug.WriteLine("Deck creation successfull!");
                }
            }
            NewDeck.Name = DeckName;
            NewDeck.Cards = deck;
            NewDeck.SaveDeck();
        }

        private void AddQuestion()
        {
            if(CurrentQuestion != null && CurrentAnswer != null)
            {
                Card card = new Card(CurrentQuestion, CurrentAnswer);
                Deck.Add(card);
                Debug.WriteLine("Card creation succesfull!");
                Debug.WriteLine($"Front: {CurrentQuestion} Back: {CurrentAnswer}");

            }
        }

        private void DeleteQuestion()
        {
            Debug.WriteLine("deleting question...");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
