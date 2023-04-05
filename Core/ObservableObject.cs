using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Core
{
    public class ObservableObject : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
