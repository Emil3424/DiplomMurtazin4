using System;
using System.Data.SqlClient;
using System.Windows;

namespace DiplomMurtazin.Core
{
    public static class ConnectionManager
    {
        private static readonly string LocalDbName = "KPMurtazin";
        private static string _currentConnectionString;

        /// <summary>
        /// Получает строку подключения к LocalDB
        /// </summary>
        public static string LocalDbConnectionString
        {
            get
            {
                return $@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog={LocalDbName};Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";
            }
        }

        /// <summary>
        /// Проверяет доступность LocalDB
        /// </summary>
        public static bool IsLocalDBAvailable()
        {
            try
            {
                using (var connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=5"))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверяет существование базы данных в LocalDB
        /// </summary>
        public static bool DatabaseExistsInLocalDB()
        {
            try
            {
                using (var connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True"))
                {
                    connection.Open();

                    var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM sys.databases WHERE name = '{LocalDbName}'", connection);
                    int exists = (int)checkCmd.ExecuteScalar();

                    return exists > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Пытается отключить все соединения с базой данных
        /// </summary>
        public static bool KillAllConnections(string serverName, string databaseName)
        {
            try
            {
                string masterConnection = $"Data Source={serverName};Initial Catalog=master;Integrated Security=True;Connect Timeout=10";

                using (var connection = new SqlConnection(masterConnection))
                {
                    connection.Open();

                    // Переводим БД в однопользовательский режим и закрываем все соединения
                    string killSql = $@"
                        USE master;
                        ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                    using (var cmd = new SqlCommand(killSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Переключает подключение на LocalDB
        /// </summary>
        public static bool SwitchToLocalDB()
        {
            try
            {
                // Проверяем наличие LocalDB
                if (!IsLocalDBAvailable())
                {
                    MessageBox.Show("Microsoft SQL Server LocalDB не установлен.\n" +
                                   "Пожалуйста, установите его с официального сайта Microsoft.",
                                   "Требуется LocalDB",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return false;
                }

                // Если база уже существует в LocalDB, просто подключаемся
                if (DatabaseExistsInLocalDB())
                {
                    _currentConnectionString = LocalDbConnectionString;
                    return true;
                }

                // Если базы нет, нужно создать
                var result = MessageBox.Show(
                    "База данных не найдена в LocalDB. Создать новую базу данных?",
                    "Создание базы данных",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (DatabaseManager.CreateDatabase())
                    {
                        _currentConnectionString = LocalDbConnectionString;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения на LocalDB: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Получает текущую строку подключения
        /// </summary>
        public static string GetCurrentConnectionString()
        {
            if (string.IsNullOrEmpty(_currentConnectionString))
            {
                _currentConnectionString = LocalDbConnectionString;
            }
            return _currentConnectionString;
        }

        /// <summary>
        /// Тестирует подключение к указанному серверу
        /// </summary>
        public static bool TestConnection(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}