using System;
using System.IO;

namespace DiplomMurtazin.Core
{
    public static class AppPaths
    {
        private static readonly string AppName = "KPMurtazin";

        /// <summary>
        /// Получить путь к папке приложения в LocalAppData
        /// </summary>
        public static string AppDataFolder
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppName);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return folder;
            }
        }

        /// <summary>
        /// Получить путь к папке с фотографиями сотрудников
        /// </summary>
        public static string EmployeePhotosFolder
        {
            get
            {
                string folder = Path.Combine(AppDataFolder, "EmployeePhotos");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return folder;
            }
        }

        /// <summary>
        /// Получить путь к папке с фотографиями пользователей
        /// </summary>
        public static string UserPhotosFolder
        {
            get
            {
                string folder = Path.Combine(AppDataFolder, "UserPhotos");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return folder;
            }
        }

        /// <summary>
        /// Сохранить фотографию в папку приложения
        /// </summary>
        public static string SavePhoto(string sourcePath, string targetFolder, string prefix = "photo")
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return null;

            try
            {
                string extension = Path.GetExtension(sourcePath).ToLower();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    extension = ".jpg";

                string fileName = $"{prefix}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
                string destPath = Path.Combine(targetFolder, fileName);

                File.Copy(sourcePath, destPath, true);

                return destPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Удалить фотографию
        /// </summary>
        public static bool DeletePhoto(string photoPath)
        {
            if (string.IsNullOrEmpty(photoPath) || !File.Exists(photoPath))
                return false;

            try
            {
                File.Delete(photoPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверить, находится ли файл в папке приложения
        /// </summary>
        public static bool IsInAppFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                string fullPath = Path.GetFullPath(path);
                string appDataFolder = Path.GetFullPath(AppDataFolder);

                return fullPath.StartsWith(appDataFolder, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}