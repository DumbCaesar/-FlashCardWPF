using FlashCardWPF.Model;
using FlashCardWPF.View;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace FlashCardWPF.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ViewModels for different views, used to pass data from/to the views.

        private CardViewModel _cardViewModel;
        private NewDeckViewModel _newDeckViewModel;

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

        public BrowseViewModel BrowseViewModel { get; set; }
        public NewDeckViewModel NewDeckViewModel
        {
            get => _newDeckViewModel;
            set
            {
                if (_newDeckViewModel != value)
                {
                    _newDeckViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        // List of decks
        public ObservableCollection<string> Decks { get; set; }

        // Selected deck to open
        public string? SelectedDeck { get; set; }

        // Commands used for the buttons
        public ICommand DeckDoubleClickCommand { get; }
        public ICommand CreateNewDeckCommand { get; }
        public ICommand ImportNewDeckCommand { get; }
        public ICommand OpenBrowseCommand { get; }



        public MainViewModel()
        {
            Decks = new ObservableCollection<string>();
            LoadDecks(); // Load saved decks upon opening the app

            DeckDoubleClickCommand = new RelayCommand(
                _ => OnDeckDoubleClick());

            CreateNewDeckCommand = new RelayCommand(_ => OnCreateDeck());

            ImportNewDeckCommand = new RelayCommand(_ => OnImportDeck());
            OpenBrowseCommand = new RelayCommand(_ => OnBrowseCards());
        }

        private void OnCreateDeck()
        {
            // opens the new deck creation window
            NewDeckViewModel = new NewDeckViewModel();
            // Uses the Action event from the NewDeckViewModel
            // to update observableCollection inside MainWindow in real time
            NewDeckViewModel.DeckCreated += deckName =>
            {
                if (!Decks.Contains(deckName))
                    Decks.Add(deckName);
            };

            var newDeck = new NewDeckView { DataContext = NewDeckViewModel };
            newDeck.ShowDialog();
        }

        private void OnBrowseCards()
        {
            // opens browse view
            BrowseViewModel = new BrowseViewModel();
            // Also uses an event to remove the deleted deck from the UI upon deletion.
            BrowseViewModel.DeckDeleted += LoadDecks;

            var browseView = new BrowseView { DataContext = BrowseViewModel };
            browseView.ShowDialog();
        }

        private void LoadDecks()
        {
            // Loads the decks from the Data/Decks folder
            Decks.Clear();
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataPath = Path.Combine(projectRoot, "Data/Decks");
            string[] files = Directory.GetFiles(dataPath);
            foreach (string file in files)
            {
                Decks.Add(Path.GetFileNameWithoutExtension(file));
            }

        }

        private void OnImportDeck()
        {
            // Import external .json deck file
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string dataDir = Path.Combine(baseDir, "Data/Decks");

            var dialog = new OpenFileDialog
            {
                Title = "Select a JSON file",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };

            if (dialog.ShowDialog() == true)
            {
                bool success = false;
                string selectedFile = dialog.FileName;
                var json = File.ReadAllText(selectedFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                Directory.CreateDirectory(dataDir);
                string destFile = Path.Combine(
                    dataDir,
                    Path.GetFileName(selectedFile)
                );

                try
                {
                    File.WriteAllText(destFile, json);
                    success = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                if (success) MessageBox.Show("Import Successfull!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("Error Importing Deck", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void OnDeckDoubleClick()
        {
            // Opens selected deck in card view
            CardViewModel = new CardViewModel(SelectedDeck);
            var cardView = new CardView { DataContext = CardViewModel };
            cardView.Show();
        }

        // Raises property changed for binding
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
