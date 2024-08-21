using CommunityToolkit.Mvvm.ComponentModel;
using ComView.Core;
using ComView.Core.Sort;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ComView.Wpf
{
    /// <summary>
    /// Класс представления виджета отображения портов
    /// </summary>
    public sealed partial class WidgetView : UserControl
    {
        #region types
        /// <summary>
        /// Класс набора цветов
        /// </summary>
        private sealed class ColorSet
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
            /// Получает кисть при наведенной мыши
            /// </summary>
            public Brush Hovered
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
            private static readonly Brush WinTaskBarDark10 = new SolidColorBrush(Color.FromRgb(43, 43, 43));

            /// <summary>
            /// Набор цветов светлой темы
            /// </summary>
            public static readonly ColorSet Light = new ColorSet()
            {
                Background = WinTaskBarLight10,
                Hovered = new SolidColorBrush(Color.FromArgb(125, 255, 255, 255)),
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
                Hovered = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                Normal = WinTaskBarLight10,
                Added = Brushes.LimeGreen,
                Removed = Brushes.OrangeRed,
                Baloon = Brushes.Lime,
            };
            #endregion
        }

        /// <summary>
        /// Класс модели виджета
        /// </summary>
        private sealed class WidgetModel : ObservableObject
        {
            #region types
            /// <summary>
            /// Класс модели оболочки порта
            /// </summary>
            public sealed class ComPortWrapper : ObservableObject
            {
                #region ctor, properties
                /// <summary>
                /// Конструктор по умолчанию
                /// </summary>
                public ComPortWrapper(WidgetModel parent, ComPortModel owner)
                {
                    if (parent == null)
                        throw new ArgumentNullException(nameof(parent));

                    if (owner == null)
                        throw new ArgumentNullException(nameof(owner));

                    Parent = parent;
                    Owner = owner;
                }

                /// <summary>
                /// Получает родительский виджет
                /// </summary>
                public WidgetModel Parent
                {
                    get;
                }

                /// <summary>
                /// Получает исходный экземпляр
                /// </summary>
                public ComPortModel Owner
                {
                    get;
                }
                #endregion
            }

            /// <summary>
            /// Класс модели оболочки сортировщика портов
            /// </summary>
            public sealed class ComSorterWrapper : ObservableObject
            {
                #region fields, ctor
                /// <summary>
                /// Исходный сортировщик портов
                /// </summary>
                private readonly IComSorter sorter;

                /// <summary>
                /// Конструктор по умолчанию
                /// </summary>
                public ComSorterWrapper(WidgetModel parent, IComSorter sorter)
                {
                    if (parent == null)
                        throw new ArgumentNullException(nameof(parent));

                    if (sorter == null)
                        throw new ArgumentNullException(nameof(sorter));

                    this.sorter = sorter;
                    Parent = parent;

                    RebuildPorts();
                }
                #endregion

                #region properties
                /// <summary>
                /// Получает родительский виджет
                /// </summary>
                public WidgetModel Parent
                {
                    get;
                }

                /// <summary>
                /// Получает список портов
                /// </summary>
                public List<ComPortWrapper> Ports
                {
                    get;
                    private set;
                }

                /// <summary>
                /// Получает или задает список исходных портов
                /// </summary>
                public ObservableCollection<ComPortModel> SourcePorts
                {
                    get => sorter.SourcePorts;
                    set
                    {
                        if (sorter.SourcePorts == value)
                            return;

                        if (sorter.SourcePorts != null)
                            sorter.PropertyChanged -= SorterPropertyChanged;

                        sorter.SourcePorts = value;

                        if (sorter.SourcePorts != null)
                            sorter.PropertyChanged += SorterPropertyChanged;

                        OnPropertyChanged();
                        RebuildPorts();
                    }
                }

                #endregion

                #region methods
                /// <summary>
                /// Пересоздает список портов
                /// </summary>
                private void RebuildPorts()
                {
                    Ports = sorter.Ports
                        .Select(p => new ComPortWrapper(Parent, p))
                        .ToList();

                    OnPropertyChanged(nameof(Ports));
                }

                /// <summary>
                /// Обработчик изменения свойств сортировщика
                /// </summary>
                private void SorterPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(sorter.Ports))
                        RebuildPorts();
                }
                #endregion
            }
            #endregion

            #region fields, ctor
            /// <summary>
            /// Количество столбцов и строк
            /// </summary>
            private int columns = 1, rows = 2;
            /// <summary>
            /// Текущий набор цветов
            /// </summary>
            private ColorSet colorSet = ColorSet.Light;

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public WidgetModel()
            {
                MenuSorter = new ComSorterWrapper(this, new NumberSorter());
                MainSorter = new ComSorterWrapper(this, new PrioritySorter());

                ThemeChanged();
            }
            #endregion

            #region properties
            /// <summary>
            /// Получает или задает список исходных портов
            /// </summary>
            public ObservableCollection<ComPortModel> SourcePorts
            {
                get => MenuSorter.SourcePorts;
                set
                {
                    if (MenuSorter.SourcePorts == value) 
                        return;

                    MenuSorter.SourcePorts = value;
                    MainSorter.SourcePorts = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Получает модель основного сортировщика портов
            /// </summary>
            public ComSorterWrapper MainSorter
            {
                get;
                private set;
            }

            /// <summary>
            /// Получает модель дополнительного сортировщика портов
            /// </summary>
            public ComSorterWrapper MenuSorter
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
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Получает или задает количество столцов
            /// </summary>
            public int Columns
            {
                get => columns;
                set
                {
                    if (value < 1)
                        value = 1;

                    columns = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Получает или задает количество строк
            /// </summary>
            public int Rows
            {
                get => rows;
                set
                {
                    if (value > 2)
                        value = 2;

                    rows = value;
                    OnPropertyChanged();
                }
            }
            #endregion

            #region methods
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
        #endregion

        #region static
        /// <summary>
        /// Свойство зависимости исходных портов
        /// </summary>
        public static readonly DependencyProperty PortsProperty;

        /// <summary>
        /// Статический конструктор
        /// </summary>
        static WidgetView()
        {
            PortsProperty = DependencyProperty.Register(
                nameof(Ports), 
                typeof(ObservableCollection<ComPortModel>), 
                typeof(WidgetView), 
                new UIPropertyMetadata((s, e) =>
                {
                    // Исходные данные события
                    var widget = (WidgetView)s;
                    var newValue = (ObservableCollection<ComPortModel>)e.NewValue;
                    var oldValue = (ObservableCollection<ComPortModel>)e.OldValue;

                    // Только изменение
                    if (oldValue == newValue) 
                        return;

                    // Подписка/Отписка от системных событий
                    if (oldValue == null && newValue != null)
                        SystemEvents.UserPreferenceChanged += widget.UserPreferenceChanged;
                    else if (oldValue != null && newValue == null)
                        SystemEvents.UserPreferenceChanged -= widget.UserPreferenceChanged;

                    // Установка значения в модель
                    widget.model.SourcePorts = newValue;
                }));
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Модель виджета
        /// </summary>
        private readonly WidgetModel model;
        /// <summary>
        /// Признак запроса на изменение темы
        /// </summary>
        private bool isThemeChangedRequested;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public WidgetView()
        {
            InitializeComponent();
            dc.Tag = 4;
            dc.DataContext = model = new WidgetModel();
        }
        #endregion

        #region properties, methods
        /// <summary>
        /// Получает или задает исходных список портов
        /// </summary>
        public ObservableCollection<ComPortModel> Ports
        {
            get => (ObservableCollection<ComPortModel>)GetValue(PortsProperty);
            set => SetValue(PortsProperty, value);
        }

        /// <summary>
        /// Обработчик системных событий пользователя
        /// </summary>
        private async void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General)
                return;

            // За одну смену темы прилетает несколько событий
            if (isThemeChangedRequested)
                return;

            // Отложенный пересчет
            isThemeChangedRequested = true;
                await Task.Delay(100);
            isThemeChangedRequested = false;

            model.ThemeChanged();
        }

        /// <summary>
        /// Обработчик изменения размеров основного элемента
        /// </summary>
        private void DoSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                model.Columns = (int)e.NewSize.Width / 45;

            if (e.HeightChanged)
                model.Rows = (int)e.NewSize.Height / 20;
        }
        #endregion
    }
}
