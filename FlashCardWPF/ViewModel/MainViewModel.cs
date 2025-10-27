using FlashCardWPF.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashCardWPF.ViewModel
{
    public class MainViewModel
    {
        public ObservableCollection<Deck> Decks { get ; set; }        


        public MainViewModel()
        {
            Decks = new ObservableCollection<Deck>();

            Card question1 = new Card("2 + 2", "4");
            Card question2 = new Card("2 + 6", "8");

            Deck math = new Deck("Math");

            math.Cards.Add(question1);
            math.Cards.Add(question2);

            Decks.Add(math);
        }
    }
}
