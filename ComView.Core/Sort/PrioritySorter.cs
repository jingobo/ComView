using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ComView.Core.Sort
{
    /// <summary>
    /// Класс сортировщика портов по приоритету
    /// </summary>
    public sealed class PrioritySorter : ObservableObject, IComSorter
    {
        #region types
        /// <summary>
        /// Класс закрытых данных порта
        /// </summary>
        private sealed class ComPortPvt : IDisposable
        {
            #region ctor
            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public ComPortPvt(PrioritySorter sorter, ComPortModel owner)
            {
                if (sorter == null)
                    throw new ArgumentNullException(nameof(sorter));
                if (owner == null)
                    throw new ArgumentNullException(nameof(owner));

                Sorter = sorter;
                Owner = owner;
                Owner.PropertyChanged += OwnerPropertyChanged;
            }
            #endregion

            #region properties
            /// <summary>
            /// Получает исходный сортировщик
            /// </summary>
            public PrioritySorter Sorter
            {
                get;
            }

            /// <summary>
            /// Получает исходный порт
            /// </summary>
            public ComPortModel Owner
            {
                get;
            }

            /// <summary>
            /// Получает или задает приоритет
            /// </summary>
            public int Priority
            {
                get;
                set;
            }
            #endregion

            #region methods
            /// <summary>
            /// Обработчик изменения значения свойств исходного порта
            /// </summary>
            private void OwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(ComPortModel.State):
                        // На переход в нормальное состояние нет реакции
                        if (Owner.State == ComPortModel.PresentState.Normal)
                            return;

                        // Обработка ниже
                        break;

                    case nameof(ComPortModel.ProcessName):
                        // Обработка ниже
                        break;

                    default:
                        // На остальные свойства нет реакции
                        return;
                }

                Priority = Sorter.NextPriority;
                Sorter.RequestSort();
            }

            /// <summary>
            /// Производит освобождение ресурсов
            /// </summary>
            public void Dispose()
            {
                Owner.PropertyChanged -= OwnerPropertyChanged;
            }
            #endregion
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Признак запроса на сортировку
        /// </summary>
        private bool isSortRequested;
        /// <summary>
        /// Аллокатор приоритета
        /// </summary>
        private int priorityAllocator;
        /// <summary>
        /// Получает исходный список портов
        /// </summary>
        private ObservableCollection<ComPortModel> sourcePorts;
        /// <summary>
        /// Словарь приватных данных портов
        /// </summary>
        private readonly Dictionary<ComPortModel, ComPortPvt> pvtData = 
            new Dictionary<ComPortModel, ComPortPvt>();

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public PrioritySorter() =>
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

        /// <summary>
        /// Получает значение следующего приоритета
        /// </summary>
        /// <remarks>Побочный эффект</remarks>
        private int NextPriority
        {
            get => priorityAllocator++;
        }
        #endregion

        #region methods
        /// <summary>
        /// Производит запрос на сортировку
        /// </summary>
        private async void RequestSort()
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
        /// Обработчик события изменения родительской коллекции портов
        /// </summary>
        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            RequestSort();

        /// <summary>
        /// Производит сортировку списка
        /// </summary>
        private void DoSort()
        {
            // Клонирование списка
            var source = this.CloneSource();

            // Удаление приватных данных
            foreach (var port in pvtData.Keys
                .Where(p => !source.Contains(p))
                .ToArray())
                pvtData.Remove(port);

            // Добавление приватных данных
            foreach (var port in source
                .Where(p => !pvtData.ContainsKey(p))
                .ToArray())
                pvtData.Add(port, new ComPortPvt(this, port) 
                    { 
                        Priority = NextPriority 
                    });

            // Клонирование и сортировка списка
            var pvtList = pvtData.Values.ToList();
            pvtList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Обновление свойства
            Ports = pvtList.Select(pvt => pvt.Owner).ToList();
            OnPropertyChanged(nameof(Ports));
        }
        #endregion
    }
}
