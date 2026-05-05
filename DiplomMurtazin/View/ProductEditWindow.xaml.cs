using DiplomMurtazin.Core;
using DiplomMurtazin.ViewModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class ProductEditWindow : Window
    {
        private ProductEditViewModel _viewModel;

        public ProductEditWindow() : this(null) { }

        public ProductEditWindow(Products product = null)
        {
            InitializeComponent();
            _viewModel = new ProductEditViewModel(product);
            DataContext = _viewModel;

            // Подписываемся на изменение ошибки
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductEditViewModel.ErrorMessage))
            {
                // Показываем/скрываем ошибку
                ErrorBorder.Visibility = string.IsNullOrEmpty(_viewModel.ErrorMessage)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private void ShowMaskHint(string message)
        {
            _viewModel.ErrorMessage = message;
            ErrorBorder.Visibility = Visibility.Visible;
            UpdateLayout();
        }

        private void TryHideMaskHint(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }

            string text = textBox.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                _viewModel.ErrorMessage = string.Empty;
                return;
            }

            bool isNumericValue = Regex.IsMatch(text, @"^\d+([.,]\d{0,2})?$");
            bool isDigitsOnly = text.All(char.IsDigit);
            if (isNumericValue || isDigitsOnly)
            {
                _viewModel.ErrorMessage = string.Empty;
            }
        }

        public Products GetProduct()
        {
            return _viewModel.Product;
        }
        private void BarcodeTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                InputMask.ApplyBarcodeMask(textBox, ShowMaskHint);
                textBox.TextChanged += (s, args) => TryHideMaskHint(textBox);
            }
        }

        private void PriceTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                InputMask.ApplyNumericMask(textBox, false, true, 2, ShowMaskHint); // положительные, с десятичной точкой
                textBox.TextChanged += (s, args) => TryHideMaskHint(textBox);
            }
        }

        private void QuantityTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                InputMask.ApplyQuantityMask(textBox, ShowMaskHint);
                textBox.TextChanged += (s, args) => TryHideMaskHint(textBox);
            }
        }

        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ErrorMessage = string.Empty;
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }
}