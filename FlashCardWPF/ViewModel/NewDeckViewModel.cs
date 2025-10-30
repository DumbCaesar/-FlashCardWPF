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
using System.Windows;
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
            if(string.IsNullOrEmpty(DeckName) || Deck.Count == 0)
            {
                MessageBox.Show("A minimum of one question is needed to create a deck!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<Card> deck = new List<Card>();
            NewDeck.Name = DeckName;
            NewDeck.Cards = deck;

            try
            {
                NewDeck.SaveDeck();
                MessageBox.Show("Deck created successfully!", "Success", MessageBoxButton.OK);
                Debug.WriteLine("Deck creation successful!");
            }

            catch(Exception ex)
            {
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
