using FlashCardWPF.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlashCardWPF.ViewModel
{
    public class NewDeckViewModel : INotifyPropertyChanged
    {
        private string _deckName;
        private string _currentQuestion;
        private string _currentAnswer;

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

        private List<Card> CreateDeck()
        {
            List<Card> deck = new List<Card>();
            return deck;
            
        }

        private void AddQuestion()
        {
            Debug.WriteLine("Adding question...");
        }

        private void DeleteQuestion()
        {
            Debug.WriteLine("deleting question...");
        }

        private void SaveDeckToFile()
        {

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
