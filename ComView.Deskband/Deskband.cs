using ComView.Core;
using ComView.Core.Pool;
using ComView.Wpf;
using CSDeskBand;
using CSDeskBand.ContextMenu;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ComView.Deskband
{
    /// <summary>
    /// Класс элемента панели задач
    /// </summary>
    [ComVisible(true)]
    [Guid("BA01ADB4-6CCC-497C-9CE6-9211F2EDFC10")]
    [CSDeskBandRegistration(Name = "ComView")]
    public sealed class Deskband : CSDeskBandWpf
    {
        #region fields
        /// <summary>
        /// Элемент виджета
        /// </summary>
        private readonly WidgetView view;
        /// <summary>
        /// Окно просмотра дескрипторов
        /// </summary>
        private HandleWindowView handleWindow;
        /// <summary>
        /// Список активных опросников
        /// </summary>
        private IComPooler[] poolers = new IComPooler[0];

        #endregion

        #region ctor
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public Deskband()
        {
            // Подстройка размеров под панель задач
            HwndSource.SizeToContent = SizeToContent.Manual;

            // Инициализация диспечера
            SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(
                        Dispatcher.CurrentDispatcher));

            // Инициализация виджета
            view = new WidgetView()
            {
                Ports = new ObservableCollection<ComPortModel>(),
            };

            Options.MinHorizontalSize = new DeskBandSize(50, -1);

            // Инициализация контекстного меню
            var demoMode = new DeskBandMenuAction("Режим демо");
            var handleWindowMode = new DeskBandMenuAction("Список дескрипторов");
            Options.ContextMenuItems = new List<DeskBandMenuItem>()
            {
                demoMode,
                handleWindowMode
            };

            // Пересоздание опросников
            async void RebuildPoolers()
            {
                CloseHandleWindow();

                // Остановка старых опросников
                await poolers.Stop();

                // Сброс портов
                view.Ports.Clear();

                // Создание демо опросника
                if (demoMode.Checked)
                {
                    poolers = new IComPooler[]
                    {
                        new DemoPooler(view.Ports),
                    };
                    return;
                }

                // Создание опросника процессов
                var processPooler = new ProcessPooler(view.Ports);

                // Показ окна дескрипторов
                if (handleWindowMode.Checked)
                {
                    handleWindow = new HandleWindowView()
                    {
                        Pooler = processPooler,
                    };

                    handleWindow.Closed += (s, e) =>
                    {
                        if (handleWindow == null)
                            return;

                        // Закрыли окно вручную
                        handleWindowMode.Checked = false;
                        handleWindow = null;
                    };
                    handleWindow.Show();
                }
                
                // Создание списка опросников
                poolers = new IComPooler[]
                {
                    new PresentPooler(view.Ports),
                    processPooler,
                };
            }
            RebuildPoolers();

            // Обработчик клика по меню режима демо
            demoMode.Clicked += (s, e) =>
            {
                demoMode.Checked = !demoMode.Checked;
                RebuildPoolers();
            };

            // Обработчик клика по меню показа окна дескрипторов
            handleWindowMode.Clicked += (s, e) =>
            {
                handleWindowMode.Checked = !handleWindowMode.Checked;
                RebuildPoolers();
            };
        }
        #endregion

        #region methods
        /// <summary>
        /// Закрывает окно просмотра дескрипторов
        /// </summary>
        private void CloseHandleWindow()
        {
            if (handleWindow == null)
                return;

            var window = handleWindow;
            handleWindow = null;
            window.Close();
        }
        #endregion

        #region overriden
        /// <summary>
        /// Получает корневой визуальный элемент
        /// </summary>
        protected override UIElement UIElement => view;

        /// <summary>
        /// Обработчик закрытия панели
        /// </summary>
        protected override void DeskbandOnClosed()
        {
            base.DeskbandOnClosed();
            CloseHandleWindow();
            poolers.Stop();
        }
        #endregion
    }
}
