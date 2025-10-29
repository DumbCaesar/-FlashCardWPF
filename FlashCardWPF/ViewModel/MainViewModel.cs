using FlashCardWPF.View;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FlashCardWPF.ViewModel 
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private CardViewModel _cardViewModel;

        public CardViewModel CardViewModel
        {
            get => _cardViewModel;
            set
            {
                if (_cardViewModel != value)
                {
                    _cardViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

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
        }

        private void OnDeckDoubleClick()
        {
            CardViewModel = new CardViewModel(SelectedDeck);
            var cardView = new CardView { DataContext = CardViewModel };
            cardView.Show();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
