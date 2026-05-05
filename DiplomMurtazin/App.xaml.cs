using DiplomMurtazin.Core;
using PdfSharp.Fonts;
using System.Windows;

namespace DiplomMurtazin
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Users CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            GlobalFontSettings.FontResolver = new FontResolver();

            // Проверяем наличие LocalDB
            if (!ConnectionManager.IsLocalDBAvailable())
            {
                MessageBox.Show("Microsoft SQL Server LocalDB не установлен.\n" +
                               "Пожалуйста, установите его с официального сайта Microsoft.\n" +
                               "https://go.microsoft.com/fwlink/?linkid=2137029",
                               "Требуется LocalDB",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);

                Current.Shutdown();
                return;
            }

            // Проверяем и переключаемся на LocalDB
            if (!ConnectionManager.SwitchToLocalDB())
            {
                MessageBox.Show("Не удалось подключиться к базе данных. Приложение будет закрыто.",
                               "Критическая ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);

                Current.Shutdown();
                return;
            }
        }
    }
}