using DiplomMurtazin.Core;
using System;
using System.Windows;

namespace DiplomMurtazin.Model
{
    public static class DataContext
    {
        private static KPMurtazinEntities _context;

        public static KPMurtazinEntities GetContext()
        {
            if (_context == null)
            {
                try
                {
                    // Проверяем наличие LocalDB
                    if (!ConnectionManager.IsLocalDBAvailable())
                    {
                        MessageBox.Show("Microsoft SQL Server LocalDB не установлен.\n" +
                                       "Пожалуйста, установите его с официального сайта Microsoft.",
                                       "Требуется LocalDB",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Warning);
                        return null;
                    }

                    // Переключаемся на LocalDB
                    if (!ConnectionManager.SwitchToLocalDB())
                    {
                        MessageBox.Show("Не удалось подключиться к LocalDB. Приложение будет закрыто.",
                                       "Критическая ошибка",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Error);
                        Application.Current.Shutdown();
                        return null;
                    }

                    // Создаем контекст с правильной строкой подключения
                    _context = new KPMurtazinEntities();

                    // Настройка контекста
                    _context.Configuration.ProxyCreationEnabled = false;
                    _context.Configuration.LazyLoadingEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}",
                                   "Ошибка",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                    return null;
                }
            }
            return _context;
        }

        public static bool TestConnection()
        {
            try
            {
                // Проверяем наличие LocalDB
                if (!ConnectionManager.IsLocalDBAvailable())
                {
                    return false;
                }

                // Проверяем существование БД в LocalDB
                if (!ConnectionManager.DatabaseExistsInLocalDB())
                {
                    return false;
                }

                using (var context = new KPMurtazinEntities())
                {
                    var canConnect = context.Database.Exists();
                    return canConnect;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}