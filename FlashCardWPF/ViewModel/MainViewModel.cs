using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        public CardViewModel CardViewModel { get; }
        public ObservableCollection<string> Decks { get ; set; } 
        public string? SelectedDeck { get; set; }
        public ICommand DeckDoubleClickCommand { get; }

        

        public MainViewModel()
        {
            Decks = new ObservableCollection<string>();
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataPath = Path.Combine(projectRoot, "Data");
            string[] files = Directory.GetFiles(dataPath);
            foreach(string file in files)
            {
                Decks.Add(Path.GetFileNameWithoutExtension(file));
            }

            DeckDoubleClickCommand = new RelayCommand(
                _ => OnDeckDoubleClick());

            CardViewModel = new CardViewModel("test");
        }

        private void OnDeckDoubleClick()
        {
            var cardViewModel = new CardViewModel(SelectedDeck);
            var cardView = new CardView { DataContext = cardViewModel };
            cardView.Show();
        }

    }
}
