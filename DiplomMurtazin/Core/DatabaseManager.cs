using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DiplomMurtazin.Core
{
    public static class DatabaseManager
    {
        private static readonly string DbName = "KPMurtazin";
        private static readonly string ConnectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog={DbName};Integrated Security=True;Connect Timeout=30";

        /// <summary>
        /// Проверяет существование базы данных
        /// </summary>
        public static bool DatabaseExists()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
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
        /// Создает базу данных из встроенного ресурса
        /// </summary>
        public static bool CreateDatabase()
        {
            try
            {
                // Читаем скрипт из ресурсов
                string script = ReadEmbeddedScript("DiplomMurtazin.SQL.ScriptDB.sql");

                if (string.IsNullOrEmpty(script))
                {
                    MessageBox.Show("Не найден скрипт создания базы данных", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Подключаемся к master для создания БД
                string masterConnection = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";

                using (var connection = new SqlConnection(masterConnection))
                {
                    connection.Open();

                    // Проверяем, существует ли уже БД
                    var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM sys.databases WHERE name = '{DbName}'", connection);
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 0)
                    {
                        // Создаем базу данных
                        var createCmd = new SqlCommand($"CREATE DATABASE [{DbName}]", connection);
                        createCmd.ExecuteNonQuery();
                    }
                }

                // Теперь выполняем скрипт в созданной БД
                ExecuteScript(script);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания базы данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Читает встроенный ресурс
        /// </summary>
        private static string ReadEmbeddedScript(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Выполняет SQL скрипт
        /// </summary>
        private static void ExecuteScript(string script)
        {
            // Разделяем скрипт на отдельные команды
            string[] commands = script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                foreach (string commandText in commands)
                {
                    if (string.IsNullOrWhiteSpace(commandText))
                        continue;

                    try
                    {
                        using (var command = new SqlCommand(commandText, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку, но продолжаем выполнение
                        System.Diagnostics.Debug.WriteLine($"Ошибка выполнения команды: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет наличие LocalDB
        /// </summary>
        public static bool IsLocalDBInstalled()
        {
            try
            {
                using (var connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True"))
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
        /// Получает строку подключения
        /// </summary>
        public static string GetConnectionString()
        {
            return ConnectionString;
        }
        /// <summary>
        /// Создает базу данных в LocalDB из встроенного ресурса
        /// </summary>
        public static bool CreateDatabaseInLocalDB()
        {
            try
            {
                // Читаем скрипт из ресурсов
                string script = ReadEmbeddedScript("DiplomMurtazin.SQL.ScriptDB.sql");

                if (string.IsNullOrEmpty(script))
                {
                    MessageBox.Show("Не найден скрипт создания базы данных", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Подключаемся к master в LocalDB
                string masterConnection = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";

                using (var connection = new SqlConnection(masterConnection))
                {
                    connection.Open();

                    // Проверяем, существует ли уже БД
                    var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM sys.databases WHERE name = 'KPMurtazin'", connection);
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        // Если БД существует, удаляем её
                        var dropCmd = new SqlCommand("DROP DATABASE [KPMurtazin]", connection);
                        dropCmd.ExecuteNonQuery();
                    }

                    // Создаем новую базу данных
                    var createCmd = new SqlCommand("CREATE DATABASE [KPMurtazin]", connection);
                    createCmd.ExecuteNonQuery();
                }

                // Теперь выполняем скрипт в созданной БД
                ExecuteScript(script, ConnectionManager.LocalDbConnectionString);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания базы данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Выполняет SQL скрипт с указанной строкой подключения
        /// </summary>
        private static void ExecuteScript(string script, string connectionString)
        {
            // Разделяем скрипт на отдельные команды
            string[] commands = script.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                foreach (string commandText in commands)
                {
                    if (string.IsNullOrWhiteSpace(commandText))
                        continue;

                    try
                    {
                        using (var command = new SqlCommand(commandText, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку, но продолжаем выполнение
                        System.Diagnostics.Debug.WriteLine($"Ошибка выполнения команды: {ex.Message}");
                    }
                }
            }
        }
    }
}