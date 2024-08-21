using ComView.Core.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComView.Core.Pool
{
    /// <summary>
    /// Класс тестового опросника портов
    /// </summary>
    public sealed class DemoPooler : IComPooler
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
            /// Получает или задает признак установленного описания
            /// </summary>
            public bool IsDescSetting
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Период опроса в мС
        /// </summary>
        private const int PoolTime = 500;
        /// <summary>
        /// Экземпляр генератора случайных чисел
        /// </summary>
        private static readonly Random random = new Random();

        /// <summary>
        /// Признак активности опроса
        /// </summary>
        private bool isActive = true;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public DemoPooler(IList<ComPortModel> ports)
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
            return Task.Delay(PoolTime);
        }

        /// <summary>
        /// Асинхронный метод опроса
        /// </summary>
        private async void Pool()
        {
            // Список английских слов
            var englishWords = Resources.words
                .Split('\n')
                .Select(s => s.Trim())
                .ToArray();

            // Получает случайное слово
            string RandomEnglishWord() =>
                englishWords[random.Next(englishWords.Length)];

            // Получает случайное слово
            string RandomEnglishWordUpper()
            {
                var word = RandomEnglishWord();
                return word.Substring(0, 1).ToUpperInvariant() + word.Substring(1);
            }

            // Конечное количество портов
            var targetPortCount = 0;
            // Словарь приватных данных портов
            var pvtList = new Dictionary<ComPortModel, ComPortPvt>();

            // Цикл опроса
            while (isActive)
            {
                // Обработка добавления/удаления порта
                {
                    // Максимальное значение таймаута
                    const int timeoutMax = 10;

                    foreach (var port in Ports.ToArray())
                    {
                        var pvt = pvtList[port];

                        switch (port.State)
                        {
                            case ComPortModel.PresentState.Normal:
                                // Не обрабатывается
                                break;

                            case ComPortModel.PresentState.Newest:
                                // Таймаут
                                if (++pvt.Timeout < timeoutMax)
                                    break;

                                // Переход в нормальное состояние
                                pvt.Timeout = 0;
                                port.State = ComPortModel.PresentState.Normal;
                                break;

                            case ComPortModel.PresentState.Removed:
                                // Таймаут
                                if (++pvt.Timeout < timeoutMax)
                                    break;

                                // Удаление
                                pvtList.Remove(port);
                                Ports.Remove(port);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(port.State));
                        }
                    }
                }

                // Следующее целевое количество протов
                if (targetPortCount == 0 || random.Next(10) == 0)
                    targetPortCount = random.Next(1, 25);

                // Установка описания
                if (random.Next(2) == 0)
                {
                    var ports = Ports
                        .Where(p => !pvtList[p].IsDescSetting)
                        .ToList();

                    if (ports.Count > 0)
                    {
                        // Подбор имени
                        var desc = RandomEnglishWordUpper();
                        var count = random.Next(1, 5);
                        for (var i = 0; i < count; i++)
                            desc += " " + RandomEnglishWord();

                        // Установка
                        var port = ports[random.Next(ports.Count)];
                        pvtList[port].IsDescSetting = true;
                        port.Description = desc;
                    }
                }

                // Устанолвка процесса
                if (random.Next(2) == 0)
                {
                    var ports = Ports
                        .Where(p => p.State != ComPortModel.PresentState.Removed)
                        .ToList();

                    if (ports.Count > 0)
                    {
                        var port = ports[random.Next(ports.Count)];
                        port.ProcessName = random.Next(2) == 0 ?
                            RandomEnglishWordUpper() :
                            null;
                    }
                }

                // Добавление новых портов
                if (targetPortCount > Ports.Count)
                {
                    // Список свободных портов
                    var numbers = new List<int>();
                    for (var i = ComPortModel.NumberLimit.Min; i <= ComPortModel.NumberLimit.Max; i++)
                        if (Ports.All(p => p.Number != i))
                            numbers.Add(i);

                    // Можно ли добавить
                    if (numbers.Count > 0)
                    {
                        // Подбор имени устройства
                        var deviceName = string.Join("", Enumerable
                            .Range(0, 4)
                            .Select(_ => RandomEnglishWordUpper()));

                        var port = new ComPortModel(numbers[random.Next(numbers.Count)], deviceName);
                        Ports.Add(port);
                        pvtList.Add(port, new ComPortPvt());
                    }
                }

                // Удаление портов
                if (targetPortCount < Ports.Count)
                {
                    var ports = Ports
                        .Where(p => p.State == ComPortModel.PresentState.Normal)
                        .ToList();

                    if (ports.Count > 0)
                        ports[random.Next(ports.Count)].State = ComPortModel.PresentState.Removed;
                }

                // Задержка опроса
                await Task.Delay(PoolTime);
            }
        }
        #endregion
    }
}
