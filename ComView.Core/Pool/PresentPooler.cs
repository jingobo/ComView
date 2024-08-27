using ComView.Core.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace ComView.Core.Pool
{
    /// <summary>
    /// Класс опросника присутствия портов
    /// </summary>
    public sealed class PresentPooler : IComPooler
    {
        #region types
        /// <summary>
        /// Класс закрытых данных порта
        /// </summary>
        private sealed class ComPortPvt
        {
            #region properties
            /// <summary>
            /// Получает или задает таймаут состояния
            /// </summary>
            public int Timeout
            {
                get;
                set;
            }

            /// <summary>
            /// Получает или задает признак нахождения
            /// </summary>
            public bool Found
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Признак активности опроса
        /// </summary>
        private bool isActive = true;
        /// <summary>
        /// Событие завершения остановки опроса
        /// </summary>
        private ManualResetEvent stopPoolEvent;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public PresentPooler(IList<ComPortModel> ports)
        {
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));

            Ports = ports;
            Pool();
        }
        #endregion

        #region properties
        /// <summary>
        /// Получает управляемый список портов
        /// </summary>
        public IList<ComPortModel> Ports
        {
            get;
        }
        #endregion

        #region methods
        /// <summary>
        /// Производит остановку с асинхронным ожиданием
        /// </summary>
        public Task Stop()
        {
            isActive = false;
            return Task.Run(() => stopPoolEvent.WaitSafe());
        }

        /// <summary>
        /// Асинхронный метод опроса
        /// </summary>
        private async void Pool()
        {
            // Словарь приватных данных портов
            var pvtList = new Dictionary<ComPortModel, ComPortPvt>();

            // Поисковик WMI объектов
            var wmiSearcher = new ManagementObjectSearcher(@"root\CIMV2", @"SELECT * FROM Win32_PnPEntity");
            // Событие завершения WMI поиска
            var wmiReadyEvent = new AutoResetEvent(false);
            // Событие завершения остановки
            stopPoolEvent = new ManualResetEvent(false);
            try
            {
                // Асинхронная задержка
                var delay = new Delay(100);

                for (var isFirstLoop = true; isActive; isFirstLoop = false)
                {
                    // Начало измерения
                    delay.Start();

                    // Определение списка портов
                    {
                        // Словарь системных портов <номер, имя устройства>
                        var sysPorts = new Dictionary<int, string>();

                        await Task.Run(() =>
                        {
                            // Имена портов
                            string[] comNames;
                            // Имена устройств
                            string[] deviceNames;

                            // Корневой раздел реестра списка портов
                            using (var regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM", false))
                            {
                                if (regKey == null)
                                    return;

                                // Чтение размела
                                deviceNames = regKey.GetValueNames();
                                comNames = deviceNames
                                    .Select(n => regKey.GetValue(n))
                                    .Select(o => o.ToString())
                                    .ToArray();
                            }

                            // Обход устройств
                            for (var i = 0; i < deviceNames.Length; i++)
                            {
                                // Проверка префикса имени порта
                                var com = comNames[i];
                                if (!com.StartsWith(ComPortModel.NamePreffix))
                                    continue;

                                // Попытка определить номер по имени
                                int number;
                                if (!int.TryParse(com.Substring(ComPortModel.NamePreffix.Length), out number))
                                    continue;

                                // Проверка на лимит номер порта
                                if (!ComPortModel.NumberLimit.IsPassed(number))
                                    continue;

                                // Возможно дубли
                                if (sysPorts.ContainsKey(number))
                                    continue;

                                // Проверка имени устройства
                                var deviceName = deviceNames[i];
                                if (string.IsNullOrEmpty(deviceName))
                                    continue;

                                // Подходящий порт
                                sysPorts.Add(number, deviceName);
                            }
                        });

                        // Максимальное значение таймаута
                        const int timeoutMax = 50;

                        // Сброс признака найденности
                        foreach (var port in Ports)
                            pvtList[port].Found = false;

                        // Обход системных портов
                        foreach (var sysPort in sysPorts)
                        {
                            // Поиск порта по номеру
                            var port = Ports.FirstOrDefault(cp => cp.Number == sysPort.Key);
                            if (port == null)
                            {
                                // Создание порта и его данных
                                port = new ComPortModel(sysPort.Key, sysPort.Value);
                                pvtList.Add(port, new ComPortPvt());

                                // В первый цикл считаем что порт уже был
                                if (isFirstLoop)
                                    port.State = ComPortModel.PresentState.Normal;

                                // Добавление по возрастанию
                                var prev = Ports.FirstOrDefault(cp => cp.Number < port.Number);
                                var index = prev != null ? Ports.IndexOf(prev) : 0;
                                Ports.Insert(index, port);
                            }

                            // Отметка что найден
                            var pvt = pvtList[port];
                            pvt.Found = true;

                            // Коррекция состояния если был удален
                            if (port.State == ComPortModel.PresentState.Removed)
                            {
                                port.State = ComPortModel.PresentState.Newest;
                                pvt.Timeout = 0;
                            }
                        }

                        // Обход не найденных
                        var nfPorts = pvtList
                            .Where(pair => !pair.Value.Found)
                            .Select(pair => pair.Key)
                            .ToArray();

                        foreach (var port in nfPorts)
                            switch (port.State)
                            {
                                case ComPortModel.PresentState.Newest:
                                case ComPortModel.PresentState.Normal:
                                    // Если был нормальным или новым
                                    port.State = ComPortModel.PresentState.Removed;
                                    pvtList[port].Timeout = 0;
                                    break;

                                case ComPortModel.PresentState.Removed:
                                    // Не обрабатывается
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException(nameof(port.State));
                            }

                        // Обход всех портов
                        foreach (var pair in pvtList.ToArray())
                        {
                            // Обработка таймаута
                            var pvt = pvtList[pair.Key];
                            if (pvt.Timeout < timeoutMax)
                            {
                                pvt.Timeout++;
                                continue;
                            }

                            // Таймаут истек, обработка состояния
                            var port = pair.Key;
                            switch (port.State)
                            {
                                case ComPortModel.PresentState.Newest:
                                    port.State = ComPortModel.PresentState.Normal;
                                    break;

                                case ComPortModel.PresentState.Removed:
                                    Ports.Remove(port);
                                    pvtList.Remove(port);
                                    break;

                                case ComPortModel.PresentState.Normal:
                                    // Не обрабатывается
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException(nameof(port.State));
                            }
                        }
                    }

                    // Определение описаний портов
                    if (Ports.Any(p => p.IsDescriptionEmpty()))
                    {
                        // Список заголовков
                        var captions = new List<string>();

                        // Подготовка слушателя
                        var results = new ManagementOperationObserver();
                        results.ObjectReady += (s, e) =>
                        {
                            var caption = e.NewObject["Caption"];
                            if (caption != null)
                                captions.Add(caption.ToString().Trim());
                        };
                        results.Completed += (s, e) => wmiReadyEvent.Set();

                        // Запуск поиска
                        wmiSearcher.Get(results);
                        await Task.Run(wmiReadyEvent.WaitOne);

                        // Разбор результатов
                        foreach (var port in Ports.Where(p => p.IsDescriptionEmpty()))
                        {
                            var suffix = $"({port.Name})";
                            var caption = captions.FirstOrDefault(cp => cp.EndsWith(suffix));
                            if (caption != null)
                                port.Description = caption.Substring(0, caption.Length - suffix.Length);
                        }
                    }

                    // Пауза опроса
                    await delay.Pause();
                }
            }
            finally
            {
                // Освобождение ресурсов
                wmiSearcher.Dispose();
                wmiReadyEvent.Dispose();

                // Сигнал остановки
                stopPoolEvent.DisposeSafe();
            }
        }
        #endregion
    }
}
