using System.Windows.Input;

namespace CourseRevit2025.FirstProject.Models;

internal class RelayCommand : ICommand
{
    private Action _execute;
    private Action<object> _executeWithObj;

    private Func<object, bool> _canExecute;

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action execute, Func<object, bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _executeWithObj = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _execute?.Invoke();
        _executeWithObj?.Invoke(parameter);
    }
}
