using System.ComponentModel;

namespace DiplomMurtazin.Model
{
    public partial class Products : INotifyPropertyChanged
    {
        private int _stockQuantity;

        public int StockQuantity
        {
            get => _stockQuantity;
            set
            {
                _stockQuantity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StockQuantity)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}