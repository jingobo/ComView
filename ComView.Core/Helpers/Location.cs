using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ComView.Core.Helpers
{
    /// <summary>
    /// Класс помошник с расположением приложения
    /// </summary>
    public static class Location
    {
        /// <summary>
        /// Получает путь к директории приложения
        /// </summary>
        public static string AppPath
        {
            get
            {
                // Текущая выполняемая сброка
                var assembly = Assembly.GetExecutingAssembly();
                // Полный путь к приложению терминала
                return Path.GetDirectoryName(assembly.Location);
            }
        }
    }
}
