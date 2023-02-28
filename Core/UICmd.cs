using System;
using System.Windows.Input;

namespace GTRCLeagueManager
{
    public class UICmd : ICommand
    {

        readonly Action<object> execute;

        public UICmd(Action<object> _execute)
        {
            this.execute = _execute;
        }

        public void Execute(object parameter)
        {
            this.execute?.Invoke(parameter);
        }

        public bool CanExecute(object parameter) { return true; }

        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;
    }
}
