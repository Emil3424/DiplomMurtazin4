using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiplomMurtazin.Core
{
    public static class DefaultPhotoGenerator
    {
        /// <summary>
        /// Создает простое фото-заглушку и сохраняет в AppData
        /// </summary>
        public static string EnsureDefaultPhoto()
        {
            try
            {
                string defaultPhotoPath = Path.Combine(PhotoHelper.PhotosDirectory, "_default.png");

                // Если уже есть, возвращаем путь
                if (File.Exists(defaultPhotoPath))
                    return defaultPhotoPath;

                // Создаем простое изображение
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

                // Сохраняем в файл
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new FileStream(defaultPhotoPath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                return defaultPhotoPath;
            }
            catch
            {
                return null;
            }
        }
    }
}