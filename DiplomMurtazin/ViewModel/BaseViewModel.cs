using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiplomMurtazin.ViewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ПРАВИЛЬНЫЙ ВАРИАНТ
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            // Используем EqualityComparer, он умеет сравнивать любые типы T
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}