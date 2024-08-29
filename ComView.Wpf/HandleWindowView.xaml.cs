using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComView.Core.Pool;
using ComView.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ComView.Wpf
{
    /// <summary>
    /// Класс представления окна списка дескрипторов
    /// </summary>
    public sealed partial class HandleWindowView : Window
    {
        #region types
        /// <summary>
        /// Класс модели представления окна
        /// </summary>
        private sealed class WindowModel : ObservableObject
        {
            #region fields, ctor
            /// <summary>
            /// Идентификатор текущего процесса
            /// </summary>
            private int? pid;
            /// <summary>
            /// Опросник процессов
            /// </summary>
            private ProcessPooler pooler;

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public WindowModel()
            {
                // Команда обновления списка процессов
                {
                    var pidsQueryExecuting = false;
                    PidsQueryCommand = new RelayCommand(async () =>
                    {
                        pidsQueryExecuting = true;
                        PidsQueryCommand.NotifyCanExecuteChanged();
                        try
                        {
                            Pids = await Pooler.QueryPids();
                            OnPropertyChanged(nameof(Pids));

                            // Автовыбор первого
                            if (Pids.Count > 0)
                                Pid = Pids.First();
                            else
                                Pid = null;
                        }
                        finally
                        {
                            pidsQueryExecuting = false;
                            PidsQueryCommand.NotifyCanExecuteChanged();
                        }
                    }, () => !pidsQueryExecuting && Pooler != null);
                }

                // Команда обновления списка дескрипторов
                {
                    var handlesQueryExecuting = false;
                    HandlesQueryCommand = new RelayCommand(async () =>
                    {
                        handlesQueryExecuting = true;
                        HandlesQueryCommand.NotifyCanExecuteChanged();
                        try
                        {
                            Handles = await Pooler.QueryHandles((int)Pid);
                            OnPropertyChanged(nameof(Handles));
                        }
                        finally
                        {
                            handlesQueryExecuting = false;
                            HandlesQueryCommand.NotifyCanExecuteChanged();
                        }
                    }, () => !handlesQueryExecuting && Pid != null && Pooler != null);
                }
            }
            #endregion

            #region properties
            /// <summary>
            /// Получает или задает опросник процессов
            /// </summary>
            internal ProcessPooler Pooler
            {
                get => pooler; 
                set
                {
                    if (!SetProperty(ref pooler, value))
                        return;

                    // Оповещение команд
                    PidsQueryCommand.NotifyCanExecuteChanged();
                    HandlesQueryCommand.NotifyCanExecuteChanged();

                    // Автозаполнение списка процессов
                    PidsQueryCommand.SafeExecute();
                }
            }

            /// <summary>
            /// Получает список идентификаторов процессов
            /// </summary>
            public IList<int> Pids
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает или задает идентификатор текущего процесса
            /// </summary>
            public int? Pid
            {
                get => pid;
                set
                {
                    if (!SetProperty(ref pid, value))
                        return;

                    // Состояние команды могло измениться
                    HandlesQueryCommand.NotifyCanExecuteChanged();

                    // Сброс списка если процесс не выбран
                    if (value == null)
                    {
                        Handles = null;
                        OnPropertyChanged(nameof(Handles));
                    }

                    // Автозаполнение списка дескрипторов
                    HandlesQueryCommand.SafeExecute();
                }
            }


            /// <summary>
            /// Получает список дескрипторов текущего процесса
            /// </summary>
            public IList<ProcessPooler.HandleInfo> Handles
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает команду запроса списка идентификаторов процессов
            /// </summary>
            public RelayCommand PidsQueryCommand
            {
                get;
            }

            /// <summary>
            /// Получает команду запроса списка дескрипторов процесса
            /// </summary>
            public RelayCommand HandlesQueryCommand
            {
                get;
            }
            #endregion
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Модель окна
        /// </summary>
        private readonly WindowModel model;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public HandleWindowView()
        {
            InitializeComponent();

            // Инициализация модели
            dc.DataContext = model = new WindowModel();
        }
        #endregion

        #region properties
        /// <summary>
        /// Получает или задает опросник процессов
        /// </summary>
        public ProcessPooler Pooler
        {
            get => model.Pooler;
            set => model.Pooler = value;
        }
        #endregion
    }
}
