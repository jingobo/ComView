using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComView.Core.Helpers
{
    /// <summary>
    /// Класс расширение для команды
    /// </summary>
    public static class RelayCommandHelper
    {
        /// <summary>
        /// Вызывает выполнение команды если это разрешено
        /// </summary>
        public static void SafeExecute(this RelayCommand command)
        {
            if (command.CanExecute(null))
                command.Execute(null);
        }
    }

    /// <summary>
    /// Класс расширенеие для Linq запросов
    /// </summary>
    public static class LinqHelper
    {
        /// <summary>
        /// Производит создание списка и его сортировку
        /// </summary>
        public static List<T> Sort<T>(this IEnumerable<T> values, Comparison<T> comparison)
        {
            var list = new List<T>(values);
            list.Sort(comparison);
            return list;
        }
    }
}
