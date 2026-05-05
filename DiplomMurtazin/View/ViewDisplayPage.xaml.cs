using DiplomMurtazin.ViewModel;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class ViewDisplayPage : Page
    {
        private ViewDisplayViewModel _viewModel;
        private int? _viewId;

        // Конструктор по умолчанию
        public ViewDisplayPage()
        {
            InitializeComponent();
            _viewModel = new ViewDisplayViewModel();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // Конструктор с ID представления
        public ViewDisplayPage(int viewId) : this()
        {
            _viewId = viewId;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadedCommand.Execute(null);

            // Небольшая задержка для загрузки списка представлений
            await System.Threading.Tasks.Task.Delay(100);

            // Если передан ID представления, загружаем его автоматически
            if (_viewId.HasValue)
            {
                LoadViewById(_viewId.Value);
            }
        }

        private void LoadViewById(int viewId)
        {
            try
            {
                // Ищем представление в загруженном списке
                var view = _viewModel.SavedViews?.FirstOrDefault(v => v.Id == viewId);

                if (view != null)
                {
                    // Выбираем представление
                    _viewModel.SelectedView = view;

                    // Загружаем данные
                    _viewModel.LoadViewCommand.Execute(null);

                    // Обновляем заголовок
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ViewDataGrid.Items.Count > 0)
                        {
                            _viewModel.StatusMessage = $"Представление '{view.Name}' загружено";
                            _viewModel.StatusColor = "#27ae60";
                        }
                    }));
                }
                else
                {
                    // Если представление еще не загрузилось в список, пробуем еще раз через секунду
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() => LoadViewById(viewId));
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки представления: {ex.Message}");
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.DisposeContext();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewDisplayViewModel.ColumnNames) ||
                e.PropertyName == nameof(ViewDisplayViewModel.ViewData))
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateDataGridColumns()));
            }
        }

        private void UpdateDataGridColumns()
        {
            ViewDataGrid.Columns.Clear();

            if (_viewModel.ColumnNames == null || _viewModel.ColumnNames.Count == 0)
            {
                var column = new DataGridTextColumn
                {
                    Header = "Нет данных",
                    Binding = new System.Windows.Data.Binding("."),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };
                ViewDataGrid.Columns.Add(column);
                return;
            }

            foreach (var columnName in _viewModel.ColumnNames)
            {
                var column = new DataGridTextColumn
                {
                    Header = columnName,
                    Binding = new System.Windows.Data.Binding($"[{columnName}]"),
                    Width = DataGridLength.Auto
                };

                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(5)));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                column.ElementStyle = style;

                ViewDataGrid.Columns.Add(column);
            }

            ViewDataGrid.Items.Refresh();
        }

        private void ViewDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Cancel = true;
        }
    }
}