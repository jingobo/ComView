using ComView.Core.ComPort;
using ComView.Core.ComPort.Sort;
using ComView.Core.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace ComView.Gui
{
    /// <summary>
    /// Класс представления окна тестирования виджета
    /// </summary>
    public partial class WidgetWindow : Window
    {
        /// <summary>
        /// Класс набора цветов
        /// </summary>
        public sealed class ColorSet
        {
            #region ctor, properties
            /// <summary>
            /// Закрытый конструктор
            /// </summary>
            private ColorSet() 
            { }

            /// <summary>
            /// Получает кисть фона
            /// </summary>
            public Brush Background
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает кисть текста состояния нормального порта
            /// </summary>
            public Brush Normal
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает кисть текста состояния добавленного порта
            /// </summary>
            public Brush Added
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает кисть текста состояния удаленного порта
            /// </summary>
            public Brush Removed
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает кисть шарика открытости порта
            /// </summary>
            public Brush Baloon
            {
                get;
                private set;
            }
            #endregion

            #region sets
            /// <summary>
            /// Светлый цвет панели задач Windows 10
            /// </summary>
            private static readonly Brush WinTaskBarLight10 = new SolidColorBrush(Color.FromRgb(238, 238, 238));
            /// <summary>
            /// Темный цвет панели задач Windows 10
            /// </summary>
            private static readonly Brush WinTaskBarDark10 = new SolidColorBrush(Color.FromRgb(16, 16, 16));

            /// <summary>
            /// Набор цветов светлой темы
            /// </summary>
            public static readonly ColorSet Light = new ColorSet()
            {
                Background = WinTaskBarLight10,
                Normal = WinTaskBarDark10,
                Added = Brushes.Green,
                Removed = Brushes.Crimson,
                Baloon = Brushes.LimeGreen,
            };

            /// <summary>
            /// Набор цветов тёмной темы
            /// </summary>
            public static readonly ColorSet Dark = new ColorSet()
            {
                Background = WinTaskBarDark10,
                Normal = WinTaskBarLight10,
                Added = Brushes.LimeGreen,
                Removed = Brushes.OrangeRed,
                Baloon = Brushes.Lime,
            };
            #endregion
        }

        /// <summary>
        /// Класс модели главного окна
        /// </summary>
        public sealed class WindowModel : ViewModel
        {
            #region types
            /// <summary>
            /// Класс модели оболочки порта
            /// </summary>
            public sealed class ComPortWrapper : ViewModel<WindowModel>
            {
                #region ctor, properties
                /// <summary>
                /// Конструктор по умолчанию
                /// </summary>
                public ComPortWrapper(WindowModel parent, ComPortModel owner) : base(parent)
                {
                    if (parent == null)
                        throw new ArgumentNullException(nameof(parent));

                    if (owner == null) 
                        throw new ArgumentNullException(nameof(owner));

                    Owner = owner;
                }

                /// <summary>
                /// Получает родительский экземпляр
                /// </summary>
                public ComPortModel Owner
                {
                    get;
                }
                #endregion
            }
            #endregion

            #region fields, ctor
            /// <summary>
            /// Текущий набор цветов
            /// </summary>
            private ColorSet colorSet = ColorSet.Light;

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public WindowModel(IComSorter sorter)
            {
                if (sorter == null)
                    throw new ArgumentNullException(nameof(sorter));

                ThemeChanged();

                // Пересоздает список портов
                void RebuildPorts()
                {
                    Ports = sorter.Ports
                        .Select(p => new ComPortWrapper(this, p))
                        .ToList();

                    DoPropertyChanged(nameof(Ports));
                }

                // Подписка на изменение портов сортировщика
                // TODO: утечка памяти
                RebuildPorts();
                sorter.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(sorter.Ports))
                        RebuildPorts();
                };
            }
            #endregion

            #region properties, methods
            /// <summary>
            /// Получает список портов
            /// </summary>
            public List<ComPortWrapper> Ports
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает текущий набор цветов
            /// </summary>
            public ColorSet ColorSet
            {
                get => colorSet;
                private set
                {
                    if (value == null)
                        throw new ArgumentNullException(nameof(value));

                    if (colorSet == value)
                        return;

                    colorSet = value;
                    DoPropertyChanged();
                }
            }

            /// <summary>
            /// Обработчик изменения цветовой темы
            /// </summary>
            public void ThemeChanged()
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null)
                        return;

                    var raw = key.GetValue("SystemUsesLightTheme");
                    if (raw == null)
                        return;

                    ColorSet = (int)raw != 0 ?
                        ColorSet.Light :
                        ColorSet.Dark;
                }
            }
            #endregion
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public WidgetWindow(IComSorter sorter)
        {
            // Инициализация модели
            var model = new WindowModel(sorter);
            DataContext = model;

            InitializeComponent();

            // Подписка на системные события
            var requested = false;
            SystemEvents.UserPreferenceChanged += async (s, e) =>
            {
                if (e.Category != UserPreferenceCategory.General)
                    return;

                // За одну смену темы прилетает несколько событий
                if (requested)
                    return;

                // Отложенный пересчет
                requested = true;
                    await Task.Delay(100);
                requested = false;
                
                model.ThemeChanged();
            };
        }
    }
}
