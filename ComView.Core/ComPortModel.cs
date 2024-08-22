using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComView.Core.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace ComView.Core
{
    /// <summary>
    /// Класс модели порта
    /// </summary>
    public sealed class ComPortModel : ObservableObject
    {
        #region types
        /// <summary>
        /// Перечисление состояния присутствия
        /// </summary>
        public enum PresentState
        {
            /// <summary>
            /// Новый
            /// </summary>
            Newest,
            /// <summary>
            /// Нормально
            /// </summary>
            Normal,
            /// <summary>
            /// Удален
            /// </summary>
            Removed
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Состояние
        /// </summary>
        private PresentState state;
        /// <summary>
        /// Имя процесса занявшего порт
        /// </summary>
        private string processName;
        /// <summary>
        /// Описание
        /// </summary>
        private string description;

        /// <summary>
        /// Префикс имени порта
        /// </summary>
        internal const string NamePreffix = "COM";
        /// <summary>
        /// Лимит номера порта
        /// </summary>
        internal static readonly Limit<int> NumberLimit = new Limit<int>(1, 255);
        /// <summary>
        /// Путь к директории профилей терминала
        /// </summary>
        private static readonly string TermProfileDirPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ComView",
                "Profiles");

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        internal ComPortModel(int number, string deviceName)
        {
            if (!NumberLimit.IsPassed(number))
                throw new ArgumentOutOfRangeException(nameof(number));

            if (string.IsNullOrEmpty(deviceName))
                throw new ArgumentNullException(nameof(deviceName));

            Number = number;
            DeviceName = deviceName;

            OpenTerminalCommand = new RelayCommand(() =>
            {
                // Создание директории
                if (!Directory.Exists(TermProfileDirPath))
                    Directory.CreateDirectory(TermProfileDirPath);

                // Разделитель настройки ключа и значения
                const char optionSplitter = '=';
                // Имя ключа настройки порта
                const string optionPortName = "Port";

                // Путь к файлу провиля
                var filePath = Path.ChangeExtension(Path.Combine(TermProfileDirPath, optionPortName + Number), ".stc");

                // Проверка на корректность порта
                var fileIsGood = File.Exists(filePath);
                while (fileIsGood)
                {
                    // Чтение строк
                    var lines = File.ReadAllLines(filePath);
                    // Разделение на пары ключ:значение
                    var pairs = lines
                        .Select(l => l.Split(optionSplitter)
                            .Select(p => p.Trim())
                            .ToArray())
                        .Where(p => p.Length > 0)
                        .ToArray();

                    // Поиск значения порта
                    var port = pairs.FirstOrDefault(p => p[0] == optionPortName);
                    if (port == null)
                    {
                        fileIsGood = false;
                        break;
                    }

                    // Проверка на соответствие
                    if (port.Length < 2 || port[1] != Name)
                        fileIsGood = false;

                    // Всё ок
                    break;
                }

                // Если файл нужно пересоздавать
                if (!fileIsGood)
                    File.WriteAllText(filePath, string.Concat(optionPortName, " ", optionSplitter, " ", Name));

                // Полный путь к приложению терминала
                var termFilePath = Path.Combine(
                    Location.AppPath,
                    "CT",
                    "CoolTerm.exe");

                // Запуск терминала
                Process.Start(termFilePath, filePath);
            }, () => State != PresentState.Removed);
        }
        #endregion

        #region properties
        /// <summary>
        /// Получает или задает состояние присутствия
        /// </summary>
        public PresentState State
        {
            get => state;
            internal set
            {
                if (!SetProperty(ref state, value))
                    return;

                if (state == PresentState.Removed)
                    ProcessName = null;

                OpenTerminalCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Получает номер
        /// </summary>
        public int Number
        {
            get;
        }

        /// <summary>
        /// Получает имя устройства
        /// </summary>
        public string DeviceName
        {
            get;
        }

        /// <summary>
        /// Получает имя
        /// </summary>
        public string Name
        {
            get => NamePreffix + Number;
        }

        /// <summary>
        /// Получает или задает описание
        /// </summary>
        public string Description
        {
            get => description;
            internal set => SetProperty(ref description, value);
        }

        /// <summary>
        /// Получает или задает имя процесса занявшего порт
        /// </summary>
        public string ProcessName
        {
            get => processName;
            internal set => SetProperty(ref processName, value);
        }

        /// <summary>
        /// Получает команду открытия терминала
        /// </summary>
        public RelayCommand OpenTerminalCommand
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Класс расширение класса модели порта
    /// </summary>
    internal static class ComPortModelHelper
    {
        /// <summary>
        /// Получает признак отсутствия описания
        /// </summary>
        public static bool IsDescriptionEmpty(this ComPortModel model) =>
            model.Description == null;
    }
}
