using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace ComView.Core.Sort
{
    /// <summary>
    /// Класс сортировщика портов по номеру
    /// </summary>
    public sealed class NumberSorter : ObservableObject, IComSorter
    {
        #region fields, ctor
        /// <summary>
        /// Признак запроса на сортировку
        /// </summary>
        private bool isSortRequested;
        /// <summary>
        /// Получает исходный список портов
        /// </summary>
        private ObservableCollection<ComPortModel> sourcePorts;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public NumberSorter() =>
            DoSort();

        #endregion

        #region properties
        /// <summary>
        /// Получает список сортированных портов
        /// </summary>
        public List<ComPortModel> Ports
        {
            get;
            private set;
        }

        /// <summary>
        /// Получает или задает исходный список портов
        /// </summary>
        public ObservableCollection<ComPortModel> SourcePorts
        {
            get => sourcePorts;
            set
            {
                if (sourcePorts == value) 
                    return;

                if (sourcePorts != null)
                    sourcePorts.CollectionChanged -= SourceCollectionChanged;
                
                sourcePorts = value;

                if (sourcePorts != null)
                    sourcePorts.CollectionChanged += SourceCollectionChanged;

                OnPropertyChanged();
                DoSort();
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Обработчик события изменения родительской коллекции портов
        /// </summary>
        private async void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (isSortRequested)
                return;

            isSortRequested = true;
            try
            {
                await Task.Yield();
            }
            finally
            {
                isSortRequested = false;
            }

            DoSort();
        }

        /// <summary>
        /// Производит сортировку списка
        /// </summary>
        private void DoSort()
        {
            // Клонирование списка
            var result = this.CloneSource();

            // Сортировка списка
            result.Sort((a, b) => a.Number.CompareTo(b.Number));

            // Обновление свойства
            Ports = result;
            OnPropertyChanged(nameof(Ports));
        }
        #endregion
    }
}
