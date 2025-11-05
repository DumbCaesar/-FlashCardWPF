using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlashCardWPF
    {
    /// <summary>
    /// Represents a command that can be executed in response to user actions or other events for MVVM pattern
    /// Makes ViewModel methods callable from XAML UI elements
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute; // The method to execute when the command is called
        private readonly Predicate<object?>? _canExecute; // Optional parameter to see if the command can be called

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Checks if the command can execute
        // Returns false to disable UI elements, such as buttons
        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter) ?? true;

        // Executes the command's action
        public void Execute(object? parameter) =>
            _execute(parameter);

        // Happens when conditions affecting CanExecute change
        // Hooked to CommandManager so WPF automatically re-evaluates states, such as on buttons
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
