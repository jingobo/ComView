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
            Options.ContextMenuItems = new List<DeskBandMenuItem>()
            {
                demoMode
            };

            // Пересоздание опросников
            async void RebuildPoolers()
            {
                // Остановка старых опросников
                await poolers.Stop();

                // Сброс портов
                view.Ports.Clear();

                // Создание новых
                poolers = demoMode.Checked ?
                    new IComPooler[]
                    {
                        new DemoPooler(view.Ports),
                    } :
                    new IComPooler[]
                    {
                        new PresentPooler(view.Ports),
                        new ProcessPooler(view.Ports),
                    };
            }
            RebuildPoolers();

            // Обработчик клика по меню режима демо
            demoMode.Clicked += (s, e) =>
            {
                demoMode.Checked = !demoMode.Checked;
                RebuildPoolers();
            };
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
            poolers.Stop();
        }
        #endregion
    }
}
