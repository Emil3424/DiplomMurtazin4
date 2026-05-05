using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace DiplomMurtazin.Core
{
    public static class PhotoMigrationHelper
    {
        /// <summary>
        /// Переносит все фото из файловой системы в базу данных
        /// </summary>
        public static void MigratePhotosToDatabase()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    // Перенос фото сотрудников
                    var employees = context.Employees
                        .Where(e => !string.IsNullOrEmpty(e.PhotoPath) && e.PhotoData == null)
                        .ToList();

                    foreach (var emp in employees)
                    {
                        if (File.Exists(emp.PhotoPath))
                        {
                            emp.PhotoData = File.ReadAllBytes(emp.PhotoPath);

                            // Если фото в папке приложения, можно удалить файл
                            if (emp.PhotoPath.Contains("EmployeePhotos"))
                            {
                                try { File.Delete(emp.PhotoPath); } catch { }
                            }

                            emp.PhotoPath = null;
                        }
                    }

                    // Перенос фото товаров (если нужно)
                    //var products = context.Products
                    //    .Where(p => !string.IsNullOrEmpty(p.PhotoPath) && p.PhotoData == null)
                    //    .ToList();

                    //foreach (var prod in products)
                    //{
                    //    if (File.Exists(prod.PhotoPath))
                    //    {
                    //        prod.PhotoData = File.ReadAllBytes(prod.PhotoPath);

                    //        if (prod.PhotoPath.Contains("ProductPhotos"))
                    //        {
                    //            try { File.Delete(prod.PhotoPath); } catch { }
                    //        }

                    //        prod.PhotoPath = null;
                    //    }
                    //}

                    context.SaveChanges();

                    MessageBox.Show($"Перенесено фото:\n" +
                                   $"Сотрудники: {employees.Count}\n" +
                                   $"",
                                   "Миграция фото завершена",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка миграции фото: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }
    }
}