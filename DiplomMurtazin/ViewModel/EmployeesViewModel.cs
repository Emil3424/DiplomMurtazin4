using DiplomMurtazin.Core;
using DiplomMurtazin.View;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class EmployeesViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<Employees> _allEmployees;
        private ObservableCollection<Employees> _filteredEmployees;
        private ObservableCollection<Positions> _positions;
        private ObservableCollection<SortOption> _sortOptions;
        private Employees _selectedEmployee;
        private Positions _selectedPositionFilter;
        private SortOption _selectedSortOption;
        private string _searchText;
        private string _statusMessage;
        private string _statusColor;
        private int _totalCount;

        public ObservableCollection<Employees> FilteredEmployees
        {
            get => _filteredEmployees;
            set => Set(ref _filteredEmployees, value);
        }

        public ObservableCollection<Positions> Positions
        {
            get => _positions;
            set => Set(ref _positions, value);
        }

        public ObservableCollection<SortOption> SortOptions
        {
            get => _sortOptions;
            set => Set(ref _sortOptions, value);
        }

        public Employees SelectedEmployee
        {
            get => _selectedEmployee;
            set => Set(ref _selectedEmployee, value);
        }

        public Positions SelectedPositionFilter
        {
            get => _selectedPositionFilter;
            set
            {
                if (Set(ref _selectedPositionFilter, value))
                    ApplyFilters();
            }
        }

        public SortOption SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (Set(ref _selectedSortOption, value))
                    ApplyFilters();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                    ApplyFilters();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => Set(ref _statusColor, value);
        }

        public string FilteredEmployeesCount => $"{FilteredEmployees?.Count ?? 0} / {_totalCount}";

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand EmployeeClickCommand { get; }

        public EmployeesViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            AddCommand = new RelayCommand(AddEmployee);
            EditCommand = new RelayCommand(EditEmployee, CanEditDelete);
            DeleteCommand = new RelayCommand(DeleteEmployee, CanEditDelete);
            RefreshCommand = new RelayCommand(RefreshData);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            EmployeeClickCommand = new RelayCommand(OnEmployeeClick);

            InitializeSortOptions();
        }

        private void InitializeSortOptions()
        {
            SortOptions = new ObservableCollection<SortOption>
            {
                new SortOption { Name = "Фамилия (А-Я)", Value = "LastName", IsAscending = true },
                new SortOption { Name = "Фамилия (Я-А)", Value = "LastName", IsAscending = false },
                new SortOption { Name = "Дата приема (сначала новые)", Value = "HireDate", IsAscending = false },
                new SortOption { Name = "Дата приема (сначала старые)", Value = "HireDate", IsAscending = true },
                new SortOption { Name = "Должность (А-Я)", Value = "Position", IsAscending = true },
                new SortOption { Name = "Должность (Я-А)", Value = "Position", IsAscending = false }
            };
        }

        private bool CanEditDelete(object parameter) => SelectedEmployee != null;

        private void OnLoaded(object parameter)
        {
            LoadData();
            LoadPositions();
        }

        private void LoadData()
        {
            try
            {
                _context = new KPMurtazinEntities();

                _allEmployees = new ObservableCollection<Employees>(
                    _context.Employees.Include(e => e.Positions).ToList()
                );

                _totalCount = _allEmployees.Count;

                // Устанавливаем сортировку по умолчанию (А-Я)
                _selectedSortOption = SortOptions.FirstOrDefault(s => s.Name == "Фамилия (А-Я)");
                OnPropertyChanged(nameof(SelectedSortOption));

                ApplyFilters();

                SetStatus("Готов к работе", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки: {ex.Message}", true);
            }
        }

        private void LoadPositions()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    var positionsList = context.Positions.OrderBy(p => p.PositionName).ToList();

                    // Создаем элемент "Все должности"
                    var allPositions = new Positions
                    {
                        PositionID = 0,
                        PositionName = "Все должности"
                    };

                    positionsList.Insert(0, allPositions);

                    Positions = new ObservableCollection<Positions>(positionsList);

                    // Устанавливаем "Все должности" как выбранное
                    SelectedPositionFilter = allPositions;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки должностей: {ex.Message}", true);
            }
        }

        private void ApplyFilters()
        {
            if (_allEmployees == null) return;

            try
            {
                var query = _allEmployees.AsEnumerable();

                // Фильтр по поиску
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search = SearchText.ToLower();
                    query = query.Where(e =>
                        e.LastName.ToLower().Contains(search) ||
                        e.FirstName.ToLower().Contains(search) ||
                        (e.MiddleName != null && e.MiddleName.ToLower().Contains(search)) ||
                        (e.Email != null && e.Email.ToLower().Contains(search)) ||
                        (e.Phone != null && e.Phone.Contains(search))
                    );
                }

                // Фильтр по должности (если выбрана не "Все должности")
                if (SelectedPositionFilter != null && SelectedPositionFilter.PositionID > 0)
                {
                    query = query.Where(e => e.PositionID == SelectedPositionFilter.PositionID);
                }

                // Сортировка
                if (SelectedSortOption != null)
                {
                    switch (SelectedSortOption.Value)
                    {
                        case "LastName":
                            query = SelectedSortOption.IsAscending
                                ? query.OrderBy(e => e.LastName)
                                : query.OrderByDescending(e => e.LastName);
                            break;
                        case "HireDate":
                            query = SelectedSortOption.IsAscending
                                ? query.OrderBy(e => e.HireDate)
                                : query.OrderByDescending(e => e.HireDate);
                            break;
                        case "Position":
                            query = SelectedSortOption.IsAscending
                                ? query.OrderBy(e => e.Positions?.PositionName ?? "")
                                : query.OrderByDescending(e => e.Positions?.PositionName ?? "");
                            break;
                    }
                }
                else
                {
                    // Сортировка по умолчанию (А-Я)
                    query = query.OrderBy(e => e.LastName);
                }

                FilteredEmployees = new ObservableCollection<Employees>(query);
                OnPropertyChanged(nameof(FilteredEmployeesCount));

                SetStatus($"Найдено: {FilteredEmployees.Count}", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка фильтрации: {ex.Message}", true);
            }
        }

        private void ResetFilters(object parameter)
        {
            SearchText = "";

            // Сбрасываем на "Все должности"
            if (Positions != null)
            {
                SelectedPositionFilter = Positions.FirstOrDefault(p => p.PositionID == 0);
            }

            // Сбрасываем на сортировку по умолчанию
            SelectedSortOption = SortOptions.FirstOrDefault(s => s.Name == "Фамилия (А-Я)");

            ApplyFilters();
            SetStatus("Фильтры сброшены", false);
        }

        private void AddEmployee(object parameter)
        {
            try
            {
                var editWindow = new EmployeeEditWindow();
                editWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow);

                if (editWindow.ShowDialog() == true)
                {
                    using (var context = new KPMurtazinEntities())
                    {
                        var newEmployee = editWindow.GetEmployee();
                        context.Employees.Add(newEmployee);
                        context.SaveChanges();
                        AuditLogger.Log("CREATE", "Employee", $"Добавлен сотрудник {newEmployee.FullName}", newEmployee.EmployeeID.ToString());

                        RefreshData(null);
                        SetStatus("Сотрудник добавлен", false);
                    }
                }
                else
                {
                    SetStatus("Добавление сотрудника отменено", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void EditEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            try
            {
                var editWindow = new EmployeeEditWindow(SelectedEmployee);
                editWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow);

                if (editWindow.ShowDialog() == true)
                {
                    using (var context = new KPMurtazinEntities())
                    {
                        var editedEmployee = editWindow.GetEmployee();
                        var dbEmployee = context.Employees.Find(editedEmployee.EmployeeID);

                        if (dbEmployee != null)
                        {
                            context.Entry(dbEmployee).CurrentValues.SetValues(editedEmployee);
                            context.SaveChanges();
                            AuditLogger.Log("UPDATE", "Employee", $"Обновлен сотрудник {dbEmployee.FullName}", dbEmployee.EmployeeID.ToString());

                            RefreshData(null);
                            SetStatus("Сотрудник обновлен", false);
                        }
                    }
                }
                else
                {
                    SetStatus("Редактирование сотрудника отменено", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void DeleteEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            var result = MessageBox.Show(
                $"Удалить сотрудника {SelectedEmployee.FullName}?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new KPMurtazinEntities())
                    {
                        bool hasRelations = context.Sales.Any(s => s.EmployeeID == SelectedEmployee.EmployeeID) ||
                                          context.InventoryActs.Any(i => i.PerformedBy == SelectedEmployee.EmployeeID) ||
                                          context.Invoices.Any(i => i.CreatedBy == SelectedEmployee.EmployeeID) ||
                                          context.Shifts.Any(s => s.EmployeeID == SelectedEmployee.EmployeeID) ||
                                          context.Users.Any(u => u.EmployeeID == SelectedEmployee.EmployeeID);

                        if (hasRelations)
                        {
                            SetStatus("Нельзя удалить: сотрудник используется в документах", true);
                            return;
                        }

                        var employee = context.Employees.Find(SelectedEmployee.EmployeeID);
                        if (employee != null)
                        {
                            context.Employees.Remove(employee);
                            context.SaveChanges();
                            AuditLogger.Log("DELETE", "Employee", $"Удален сотрудник {employee.FullName}", employee.EmployeeID.ToString());

                            RefreshData(null);
                            SetStatus("Сотрудник удален", false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetStatus($"Ошибка: {ex.Message}", true);
                }
            }
            else
            {
                SetStatus("Удаление сотрудника отменено", false);
            }
        }

        private void OnEmployeeClick(object parameter)
        {
            if (parameter is Employees employee)
            {
                SelectedEmployee = employee;
                EditEmployee(null);
            }
        }

        private void RefreshData(object parameter)
        {
            _context?.Dispose();
            LoadData();
            LoadPositions();
            SetStatus("Данные обновлены", false);
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? "#e74c3c" : "#3498db";
        }

        public void DisposeContext() => _context?.Dispose();
    }
}