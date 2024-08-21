using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ComView.Core.Sort
{
    /// <summary>
    /// Интерфейс сортировщика портов
    /// </summary>
    public interface IComSorter : INotifyPropertyChanged
    {
        #region properties
        /// <summary>
        /// Получает список сортированных портов
        /// </summary>
        List<ComPortModel> Ports
        {
            get;
        }

        /// <summary>
        /// Получает или задает исходный список портов
        /// </summary>
        ObservableCollection<ComPortModel> SourcePorts 
        { 
            get;
            set;
        }
        #endregion
    }

    /// <summary>
    /// Класс расширение для интерфейса сортировщика портов
    /// </summary>
    internal static class ComSorterHelper
    {
        #region methods
        /// <summary>
        /// Производит клонирование списка исходных портов
        /// </summary>
        public static List<ComPortModel> CloneSource(this IComSorter sorter)
        {
            var result = new List<ComPortModel>();
            if (sorter.SourcePorts != null)
                result.AddRange(sorter.SourcePorts);

            return result;
        }
        #endregion
    }
}
