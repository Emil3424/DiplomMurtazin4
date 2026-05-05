using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace DiplomMurtazin
{
    public partial class Employees : INotifyPropertyChanged
    {
        private BitmapImage _photo;

        public string FullName
        {
            get
            {
                string fullName = $"{LastName} {FirstName}".Trim();
                if (!string.IsNullOrWhiteSpace(MiddleName))
                    fullName += $" {MiddleName}";
                return fullName;
            }
        }

        public BitmapImage Photo
        {
            get
            {
                if (_photo != null)
                    return _photo;

                // Загружаем из PhotoData (приоритет)
                if (PhotoData != null && PhotoData.Length > 0)
                {
                    try
                    {
                        _photo = LoadImageFromBytes(PhotoData);
                        return _photo;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки PhotoData: {ex.Message}");
                    }
                }

                // Загружаем из PhotoPath (для обратной совместимости)
                if (!string.IsNullOrEmpty(PhotoPath))
                {
                    try
                    {
                        // Проверяем существование файла
                        if (File.Exists(PhotoPath))
                        {
                            _photo = LoadImageFromFile(PhotoPath);
                            return _photo;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки PhotoPath: {ex.Message}");
                    }
                }

                return null;
            }
        }

        private BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            var image = new BitmapImage();

            try
            {
                using (var stream = new MemoryStream(imageData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.DecodePixelWidth = 200;
                    image.EndInit();
                }
                image.Freeze();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в LoadImageFromBytes: {ex.Message}");
                return null;
            }

            return image;
        }

        private BitmapImage LoadImageFromFile(string path)
        {
            var image = new BitmapImage();

            try
            {
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 200;
                image.EndInit();
                image.Freeze();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в LoadImageFromFile: {ex.Message}");
                return null;
            }

            return image;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}