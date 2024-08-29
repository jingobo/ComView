// Включение тестового опросника
#if DEBUG
    //#define Demo
#endif

using CommunityToolkit.Mvvm.ComponentModel;
using ComView.Core;
using ComView.Core.Pool;
using ComView.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ComView.Gui
{
    /// <summary>
    /// Класс представления главного окна
    /// </summary>
    internal sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Класс модели представления главного окна
        /// </summary>
        private sealed class MainWindowModel : ObservableObject
        {
            #region fields, ctor
            /// <summary>
            /// Список опросников портов
            /// </summary>
            private readonly IComPooler[] comPoolers;

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public MainWindowModel()
            {
#if Demo
                comPoolers = new IComPooler[]
                {
                    new DemoPooler(Ports),
                };
#else
                var handleWindow = new HandleWindowView()
                {
                    Pooler = new ProcessPooler(Ports),
                };

                comPoolers = new IComPooler[]
                {
                    new PresentPooler(Ports),
                    handleWindow.Pooler,
                };

                handleWindow.Show();
#endif
            }
            #endregion

            #region properties, methods
            /// <summary>
            /// Получает список моделей портов
            /// </summary>
            public ObservableCollection<ComPortModel> Ports
            {
                get;
            } = new ObservableCollection<ComPortModel>();

            /// <summary>
            /// Производит остановку опросников с асинхронным ожиданием
            /// </summary>
            public Task Stop() =>
                comPoolers.Stop();

            #endregion
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Инициализация модели
            var model = new MainWindowModel();
            DataContext = model;

            #region closing

            // Автомат ожидания закрытия приложения
            var closingPhase = 0;
            Closing += async (s, e) =>
            {
                switch (closingPhase)
                {
                    // Начало остановки
                    case 0:
                        // Код ниже
                        break;

                    case 1:
                        // Процесс остановки
                        e.Cancel = true;
                        return;

                    case 2:
                        // Закрытие разрешено
                        return;

                    default:
                        throw new InvalidOperationException();
                }

                e.Cancel = true;
                Title += " (закрытие)";
                closingPhase = 1;
                    await model.Stop();
                closingPhase = 2;
                Close();
            };

            #endregion
        }
    }
}
