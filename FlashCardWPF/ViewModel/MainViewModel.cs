using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FlashCardWPF.Model;
using FlashCardWPF.View;

namespace FlashCardWPF.ViewModel
{
    public class MainViewModel
    {
        public ObservableCollection<Deck> Decks { get ; set; } 
        public Deck? SelectedDeck { get; set; }
        public ICommand DeckDoubleClickCommand { get; }


        public MainViewModel()
        {
            Decks = new ObservableCollection<Deck>();

            Card question1 = new Card("2 + 2", "4");
            Card question2 = new Card("2 + 6", "8");

            Deck math = new Deck("Math");

            math.Cards.Add(question1);
            math.Cards.Add(question2);

            Decks.Add(math);

            DeckDoubleClickCommand = new RelayCommand(
                _ => OnDeckDoubleClick(math));
        }

        private void OnDeckDoubleClick(Deck? deck)
        {
            var cardViewModel = new CardViewModel(SelectedDeck);
            var cardView = new CardView { DataContext = cardViewModel };
            cardView.Show();
        }
    }
}
