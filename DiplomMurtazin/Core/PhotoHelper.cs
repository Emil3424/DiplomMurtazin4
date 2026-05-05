using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiplomMurtazin.Core
{
    public static class PhotoHelper
    {
        private static string _photosDirectory;

        /// <summary>
        /// Получить путь к папке с фотографиями сотрудников
        /// </summary>
        public static string PhotosDirectory
        {
            get
            {
                if (_photosDirectory == null)
                {
                    // Используем папку в AppData для хранения фотографий
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    _photosDirectory = Path.Combine(appData, "KPMurtazin", "EmployeePhotos");

                    if (!Directory.Exists(_photosDirectory))
                        Directory.CreateDirectory(_photosDirectory);
                }
                return _photosDirectory;
            }
        }

        /// <summary>
        /// Сохранить фотографию и вернуть относительный путь
        /// </summary>
        public static string SavePhoto(string sourcePath, int employeeId)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return null;

            try
            {
                // Генерируем уникальное имя файла
                string extension = Path.GetExtension(sourcePath).ToLower();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    extension = ".jpg";

                string fileName = $"emp_{employeeId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                string destPath = Path.Combine(PhotosDirectory, fileName);

                // Копируем файл
                File.Copy(sourcePath, destPath, true);

                // Возвращаем относительный путь
                return Path.Combine("EmployeePhotos", fileName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Сохранить временное фото и вернуть путь
        /// </summary>
        public static string SaveTempPhoto(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return null;

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "KPMurtazin");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                string fileName = $"temp_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                string destPath = Path.Combine(tempDir, fileName);

                File.Copy(sourcePath, destPath, true);

                return destPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Загрузить изображение по относительному пути
        /// </summary>
        public static BitmapImage LoadImage(string relativePath, int decodeWidth = 150)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            try
            {
                string fullPath = GetFullPath(relativePath);
                if (!File.Exists(fullPath))
                {
                    // Пробуем найти фото в ресурсах приложения (для встроенных фото)
                    return LoadEmbeddedImage("default-avatar.png");
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(fullPath, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.DecodePixelWidth = decodeWidth;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return LoadEmbeddedImage("default-avatar.png");
            }
        }

        /// <summary>
        /// Загрузить изображение из ресурсов приложения
        /// </summary>
        private static BitmapImage LoadEmbeddedImage(string fileName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 150;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Получить полный путь из относительного
        /// </summary>
        public static string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            if (Path.IsPathRooted(relativePath))
                return relativePath;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KPMurtazin",
                relativePath
            );
        }

        /// <summary>
        /// Удалить фотографию
        /// </summary>
        public static bool DeletePhoto(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            try
            {
                string fullPath = GetFullPath(relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Очистить временные файлы
        /// </summary>
        public static void CleanupTempFiles()
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "KPMurtazin");
                if (Directory.Exists(tempDir))
                {
                    foreach (var file in Directory.GetFiles(tempDir, "temp_*"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Инициализация фото при первом запуске
        /// </summary>
        public static void InitializeDefaultPhotos()
        {
            try
            {
                // Создаем папку если её нет
                if (!Directory.Exists(PhotosDirectory))
                    Directory.CreateDirectory(PhotosDirectory);

                // Здесь можно скопировать фотографии из ресурсов приложения
                // если нужно предустановить какие-то фото
            }
            catch { }
        }
        /// <summary>
        /// Конвертировать изображение в массив байт
        /// </summary>
        public static byte[] ImageToBytes(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;

            try
            {
                return File.ReadAllBytes(imagePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Конвертировать массив байт в BitmapImage
        /// </summary>
        public static BitmapImage BytesToImage(byte[] imageData, int decodeWidth = 150)
        {
            if (imageData == null || imageData.Length == 0)
            {
                Debug.WriteLine("BytesToImage: imageData is null or empty");
                return null;
            }

            try
            {
                Debug.WriteLine($"BytesToImage: imageData length = {imageData.Length} bytes");

                // Важно! Создаем копию массива байт в новом MemoryStream
                // и не закрываем stream до завершения инициализации изображения
                var stream = new MemoryStream(imageData);

                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.None; // Изменено с IgnoreImageCache
                image.DecodePixelWidth = decodeWidth;
                image.EndInit();

                // Замораживаем для использования в других потоках
                image.Freeze();

                // Закрываем stream после завершения инициализации
                stream.Dispose();

                Debug.WriteLine("BytesToImage: Image created successfully");
                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BytesToImage error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Создать изображение-заглушку в виде массива байт
        /// </summary>
        public static byte[] CreateDefaultImage()
        {
            try
            {
                Debug.WriteLine("CreateDefaultImage: Creating default image");

                int width = 150;
                int height = 180;

                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Серый фон
                    context.DrawRectangle(
                        new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        null,
                        new Rect(0, 0, width, height));

                    // Синий круг
                    context.DrawEllipse(
                        new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                        null,
                        new Point(width / 2, height / 2 - 20),
                        40, 40);

                    // Текст "Фото"
                    var text = new FormattedText(
                        "Фото",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        16,
                        Brushes.White,
                        96);

                    context.DrawText(text, new Point(width / 2 - 25, height / 2 + 30));
                }

                bitmap.Render(visual);

                // Конвертируем в байты
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    var result = stream.ToArray();
                    Debug.WriteLine($"CreateDefaultImage: Created {result.Length} bytes");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateDefaultImage error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Обработать выбранное фото (оптимизация)
        /// </summary>
        public static byte[] ProcessSelectedPhoto(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.WriteLine("ProcessSelectedPhoto: File not exists");
                return null;
            }

            try
            {
                Debug.WriteLine($"ProcessSelectedPhoto: Loading from {filePath}");

                // Просто читаем файл и возвращаем байты
                // без дополнительной оптимизации для начала
                var bytes = File.ReadAllBytes(filePath);
                Debug.WriteLine($"ProcessSelectedPhoto: Read {bytes.Length} bytes");
                return bytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProcessSelectedPhoto error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Создать тестовое фото для отладки
        /// </summary>
        public static byte[] CreateTestPhoto()
        {
            try
            {
                int width = 100;
                int height = 120;

                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();

                using (var context = visual.RenderOpen())
                {
                    context.DrawRectangle(Brushes.LightBlue, null, new Rect(0, 0, width, height));

                    var text = new FormattedText(
                        "Test",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        16,
                        Brushes.Black,
                        96);

                    context.DrawText(text, new Point(20, 40));
                }

                bitmap.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}