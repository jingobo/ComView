﻿using ComView.Core.Helpers;
using ComView.Core.Pool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComView.Core.Pool
{
    /// <summary>
    /// Класс опросника процессов использующи порты
    /// </summary>
    public sealed class ProcessPooler : IComPooler
    {
        #region native
        /// <summary>
        /// Перечисление статуса NT
        /// </summary>
        private enum NtStatus
        {
            /// <summary>
            /// Успешно
            /// </summary>
            Success,
            /// <summary>
            /// Не корректная длинна
            /// </summary>
            WrongLength = -1073741820
        }

        /// <summary>
        /// WinApi функция запроса системной информации
        /// </summary>
        [DllImport("ntdll.dll")]
        private static extern NtStatus NtQuerySystemInformation(
            [In] int informationClass,
            [In] IntPtr dest,
            [In] int destSize,
            [Out] out int writtenSize);

        #endregion

        #region types
        /// <summary>
        /// Структура системной информации о дескрипторе
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SystemHandleInfo
        {
            /// <summary>
            /// Указатель на объект
            /// </summary>
            public ulong Object;

            /// <summary>
            /// Идентификатор процесса
            /// </summary>
            public int Pid;
            /// <summary>
            /// Выравнивание
            /// </summary>
            public int PidAlign;

            /// <summary>
            /// Исходный дескриптор
            /// </summary>
            public int Handle;
            /// <summary>
            /// Выравнивание
            /// </summary>
            public int HandleAlign;

            /// <summary>
            /// Права доступа
            /// </summary>
            public int Access;
            /// <summary>
            /// Не известно
            /// </summary>
            public short TraceIndex;
            /// <summary>
            /// Не известно
            /// </summary>
            public short TypeIndex;
            /// <summary>
            /// Не известно
            /// </summary>
            public int Attributes;
            /// <summary>
            /// Выравнивание
            /// </summary>
            public int AttributesAlign;
        }

        /// <summary>
        /// Структра запроса в канал
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct PipeRequest
        {
            /// <summary>
            /// Идентификатор процесса
            /// </summary>
            public int Pid;
            /// <summary>
            /// Дескриптор
            /// </summary>
            public int Handle;
        }
        
        /// <summary>
        /// Перечисление статуса дескриптора
        /// </summary>
        public enum HandleStatus : int
        {
            /// <summary>
            /// Успешно
            /// </summary>
            Success,
            /// <summary>
            /// Текущий процесс не обрабатывается
            /// </summary>
            SameProcess,
            /// <summary>
            /// Не удалось открыть процесс
            /// </summary>
            OpenProcess,
            /// <summary>
            /// Не удалось создать дубликат дескриптора
            /// </summary>
            TargetDuplicate,
            /// <summary>
            /// Не удалось определить тип дескриптора
            /// </summary>
            TargetQueryType,
            /// <summary>
            /// Не корректный тип дескриптора
            /// </summary>
            TargetInvalidType,
            /// <summary>
            /// Не удалось определить имя дескриптора
            /// </summary>
            TargetQueryName,
        }

        /// <summary>
        /// Структура ответа из канала
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct PipeResponse
        {
            /// <summary>
            /// Статус операции
            /// </summary>
            public HandleStatus Status;
            /// <summary>
            /// Размер в байтах
            /// </summary>
            public int Size;
            /// <summary>
            /// Буфер имени дескриптора
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] Data;
        }

        /// <summary>
        /// Класс элемента дескриптора кэша
        /// </summary>
        internal sealed class HandleCacheEntry
        {
            #region fields
            /// <summary>
            /// Имя дескриптора
            /// </summary>
            public string Name;
            /// <summary>
            /// Попытка опроса
            /// </summary>
            public int Probe;
            /// <summary>
            /// Признак присутствия в системе
            /// </summary>
            public bool IsExists;
            /// <summary>
            /// Получает статус последней операции
            /// </summary>
            public HandleStatus? Status;

            #endregion
        }

        /// <summary>
        /// Класс информации о дескрипторе
        /// </summary>
        public sealed class HandleInfo
        {
            #region ctor, properties
            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            internal HandleInfo(int handle, HandleCacheEntry entry)
            {
                Handle = handle;
                Name = entry.Name;
                Status = entry.Status;
            }

            /// <summary>
            /// Получает идентификатор дескриптора
            /// </summary>
            public int Handle
            {
                get;
            }

            /// <summary>
            /// Получает имя дескриптора
            /// </summary>
            public string Name
            {
                get;
            }

            /// <summary>
            /// Получает статус последней операции
            /// </summary>
            public HandleStatus? Status
            {
                get;
            }
            #endregion
        }

        /// <summary>
        /// Класс ожидания данных запроса
        /// </summary>
        private sealed class QueryWaiter<T> : IDisposable
        {
            #region fields
            /// <summary>
            /// Данные результата
            /// </summary>
            private T data;
            /// <summary>
            /// Опциональный аргумент
            /// </summary>
            private object arg;
            /// <summary>
            /// Признак активности запроса
            /// </summary>
            private bool isRequested;
            /// <summary>
            /// Событие ожидания
            /// </summary>
            private EventWaitHandle waitEvent = new AutoResetEvent(false);

            #endregion

            #region methods
            /// <summary>
            /// Производит запрос и синхронное ожидание
            /// </summary>
            public Task<T> Query(object arg = null)
            {
                // Установка признака ожидания
                lock (this)
                {
                    this.arg = arg;
                    isRequested = true;
                }

                return Task.Run(() =>
                {
                    // Синхронное ожидание
                    waitEvent.WaitOne();

                    // Сброс аргумента
                    this.arg = null;

                    // Сброс ссылки на данные
                    var result = data;
                    data = default;
                    return result;
                });
            }

            /// <summary>
            /// Производит обработку
            /// </summary>
            public Task Work(Func<object, T> cb)
            {
                if (cb == null)
                    throw new ArgumentNullException(nameof(cb));

                // Проверка на необходимость обработки
                lock (this)
                    if (isRequested)
                        isRequested = false;
                    else
                        return Task.Delay(0);

                // Обработка
                return Task.Run(() =>
                {
                    data = cb(arg);
                    waitEvent.Set();
                });
            }

            /// <summary>
            /// Производит освобождение ресурсов
            /// </summary>
            public void Dispose() =>
                waitEvent.Dispose();

            #endregion
        }
        #endregion

        #region fields, ctor
        /// <summary>
        /// Период опроса в мС
        /// </summary>
        private const int PoolTime = 500;

        /// <summary>
        /// Признак активности опроса
        /// </summary>
        private bool isActive = true;

        /// <summary>
        /// События завершения остановки опроса
        /// </summary>
        private ManualResetEvent stopWatchdogEvent, stopPoolEvent;

        /// <summary>
        /// Токен отмены подключения канала связи
        /// </summary>
        private readonly CancellationTokenSource pipeConnectCancel = 
            new CancellationTokenSource();

        /// <summary>
        /// Ожидатель данных списка идентификаторов процессов
        /// </summary>
        private readonly QueryWaiter<int[]> queryPids = new QueryWaiter<int[]>();
        /// <summary>
        /// Ожидатель данных списка дескрипторов процесса
        /// </summary>
        private readonly QueryWaiter<HandleInfo[]> queryHandles = new QueryWaiter<HandleInfo[]>();

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public ProcessPooler(IList<ComPortModel> ports)
        {
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));

            Ports = ports;
            Pool();
            Watchdog();
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
        /// Асинхронный метод ватчдога процесса
        /// </summary>
        private async void Watchdog()
        {
            // Путь к файлу процесса
            var handleFilePath = Path.Combine(
                Location.AppPath,
                "ComViewHandle.exe");

            // Событие завершения остановки
            stopWatchdogEvent = new ManualResetEvent(false);
            try
            {
                // Цикл опроса
                Process handleProcess = null;
                while (isActive)
                {
                    // Запуск процесса
                    if (handleProcess == null)
                        handleProcess = Process.Start(handleFilePath);

                    // Проверка состояния
                    if (handleProcess.HasExited)
                    {
                        handleProcess.Dispose();
                        handleProcess = null;
                        continue;
                    }

                    // Пауза опроса
                    await Task.Delay(PoolTime);
                }
            }
            finally
            {
                stopWatchdogEvent.DisposeSafe();
            }
        }

        /// <summary>
        /// Производит остановку с асинхронным ожиданием
        /// </summary>
        public Task Stop()
        {
            isActive = false;
            pipeConnectCancel.Cancel();

            return Task.Run(() =>
            {
                stopPoolEvent.WaitSafe();
                stopWatchdogEvent.WaitSafe();
            });
        }

        /// <summary>
        /// Асинхронный метод опроса
        /// </summary>
        private async void Pool()
        {
            // Канал связи с нативным приложением
            NamedPipeServerStream pipe = null;

            // Закрытвает канал связи
            void ClosePipe()
            {
                if (pipe == null)
                    return;

                pipe.Dispose();
                pipe = null;
            }

            // Размеры структур запроса/ответа в канале
            var pipeReqestSize = Marshal.SizeOf<PipeRequest>();
            var pipeResponseSize = Marshal.SizeOf<PipeResponse>();

            // Буферы структур запроса/ответа в канале
            var pipeRequestBuffer = new byte[pipeReqestSize];
            var pipeResponseBuffer = new byte[pipeResponseSize];

            // Идентификатор текущего процесса
            var currentPid = Process.GetCurrentProcess().Id;
            // Кэш дескрипторов системы <pid:<handle:entry>>
            var cache = new Dictionary<int, Dictionary<int, HandleCacheEntry>>();

            // Размер и указатель системного буфера
            var sysBufferSize = 0x10000;
            var sysBufferPtr = IntPtr.Zero;

            // Событие завершения остановки
            stopPoolEvent = new ManualResetEvent(false);
            try
            {
                // Цикл опроса
                for (var delay = new Delay(PoolTime); isActive; )
                {
                    // Начало измерения
                    delay.Start();

                    // Подключение к процессу
                    if (pipe == null)
                    {
                        // Открытие канала связи с нативным приложением
                        try
                        {
                            pipe = new NamedPipeServerStream("ComViewHandle", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, pipeResponseSize, pipeReqestSize);
                        }
                        catch (IOException)
                        {
                            // Не удалось открыть канал
                            await Task.Delay(100);
                            continue;
                        }

                        // Ожидание подключения
                        try
                        {
                            await pipe.WaitForConnectionAsync(pipeConnectCancel.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            ClosePipe();
                            break;
                        }
                    }

                    // Список имен дескрипторов протов
                    var portHandleNames = Ports
                        .Select(p => p.DeviceName)
                        .ToArray();

                    // Фоновая обработка
                    await Task.Run(() =>
                    {
                        // Загрузка общего списка дескрипторов
                        var processes = new Dictionary<int, List<int>>();
                        {
                            NtStatus status;
                            do
                            {
                                // Выделение нативной памяти
                                if (sysBufferPtr == IntPtr.Zero)
                                    sysBufferPtr = Marshal.AllocHGlobal(sysBufferSize);

                                // Пробуем запросить список дескрипторов
                                int size;
                                status = NtQuerySystemInformation(64, sysBufferPtr, sysBufferSize, out size);
                                if (status == NtStatus.WrongLength)
                                {
                                    // Увеличение памяти до 64КВ
                                    sysBufferSize = (size + 0xffff) & ~0xffff;
                                    Marshal.FreeHGlobal(sysBufferPtr);
                                    sysBufferPtr = IntPtr.Zero;
                                    continue;
                                }

                                // Далее если успех
                                if (status != NtStatus.Success)
                                    continue;

                                // Загрузка списка дескрипторов из нативной памяти
                                var handleCount = Marshal.ReadInt64(sysBufferPtr);
                                unsafe
                                {
                                    var entry = (SystemHandleInfo*)(sysBufferPtr + sizeof(long) * 2);
                                    for (var i = 0; i < handleCount; i++)
                                    {
                                        var pid = entry[i].Pid;
                                        if (pid == currentPid)
                                            continue;

                                        // Добавление в словарь процессов
                                        List<int> list;
                                        if (!processes.TryGetValue(pid, out list))
                                        {
                                            list = new List<int>();
                                            processes.Add(pid, list);
                                        }

                                        // Добавление в список дескрипторов
                                        list.Add(entry[i].Handle);
                                    }
                                }
                            }
                            while (status == NtStatus.WrongLength);
                        }

                        // Предварительное снятие признака присутствия в системе
                        foreach (var process in cache.Values)
                            foreach (var handle in process.Values)
                                handle.IsExists = false;

                        // Обход процессов
                        foreach (var process in processes)
                        {
                            // Идентификатор процесса
                            var pid = process.Key;

                            // Получаем список дескрипторов из кэша по процессу
                            Dictionary<int, HandleCacheEntry> cacheHandles;
                            if (cache.ContainsKey(pid))
                                cacheHandles = cache[pid];
                            else
                            {
                                // Создание нового
                                cacheHandles = new Dictionary<int, HandleCacheEntry>();
                                cache.Add(pid, cacheHandles);
                            }

                            // Обход дескрипторов
                            foreach (var handle in process.Value)
                            {
                                // Запрос информации о дескрипторе из кэша
                                HandleCacheEntry cacheHandle;
                                if (!cacheHandles.TryGetValue(handle, out cacheHandle))
                                {
                                    cacheHandle = new HandleCacheEntry();
                                    cacheHandles.Add(handle, cacheHandle);
                                }

                                // Дескриптор в системе присутствует
                                cacheHandle.IsExists = true;

                                // Запрос имени если не запрашивался или это точно порт
                                if (cacheHandle.Name != null &&
                                    portHandleNames.All(p => p != cacheHandle.Name))
                                    continue;

                                // Формирование запроса
                                {
                                    var request = new PipeRequest()
                                    {
                                        Pid = pid,
                                        Handle = handle
                                    };

                                    // Конвертирование в массив байт
                                    var ptr = Marshal.AllocHGlobal(pipeReqestSize);
                                    try
                                    {
                                        Marshal.StructureToPtr(request, ptr, true);
                                        Marshal.Copy(ptr, pipeRequestBuffer, 0, pipeReqestSize);
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(ptr);
                                    }
                                }

                                // Транзакция канала
                                try
                                {
                                    // Запрос
                                    pipe.Write(pipeRequestBuffer, 0, pipeRequestBuffer.Length);

                                    // Ответ
                                    if (pipe.Read(pipeResponseBuffer, 0, pipeResponseBuffer.Length) != pipeResponseBuffer.Length)
                                        throw new IOException();
                                }
                                catch (IOException)
                                {
                                    // Канал закрыт
                                    ClosePipe();
                                    return;
                                }

                                // Конвертирование ответа в структуру
                                PipeResponse response;
                                {
                                    var ptr = Marshal.AllocHGlobal(pipeResponseSize);
                                    try
                                    {
                                        Marshal.Copy(pipeResponseBuffer, 0, ptr, pipeResponseSize);
                                        response = (PipeResponse)Marshal.PtrToStructure(ptr, typeof(PipeResponse));
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(ptr);
                                    }
                                }

                                // Для внешних запросов
                                cacheHandle.Status = response.Status;

                                // Проверка результата
                                var isBreak = false;
                                switch (response.Status)
                                {
                                    case HandleStatus.Success:
                                        // Конвертирование имени дескриптора
                                        cacheHandle.Probe = 0;
                                        cacheHandle.Name = Encoding.Unicode.GetString(response.Data, 16, response.Size - 18);
                                        break;

                                    case HandleStatus.OpenProcess:
                                    case HandleStatus.SameProcess:
                                        // Остановить обработку процесса
                                        isBreak = true;
                                        break;

                                    case HandleStatus.TargetDuplicate:
                                    case HandleStatus.TargetQueryType:
                                        // Возможно дескриптор не корректный
                                        cacheHandle.Name = string.Empty;
                                        break;
                                    case HandleStatus.TargetInvalidType:
                                        // Возможно тип еще не актуальный
                                        if (++cacheHandle.Probe > 1)
                                            cacheHandle.Name = string.Empty;
                                        break;

                                    case HandleStatus.TargetQueryName:
                                        // Пробуем дать второй шанс
                                        if (cacheHandle.Name == null)
                                            cacheHandle.Name = string.Empty;
                                        break;

                                    default:
                                        // Что то странное
                                        ClosePipe();
                                        return;
                                }

                                // Если процесс не подходит
                                if (isBreak)
                                    break;
                            }
                        }

                        // Обход процессов для чистки
                        foreach (var pid in cache.Keys.ToArray())
                        {
                            // Удаление дескрипторов отсутствующих в системе
                            var handles = cache[pid];
                            foreach (var handle in handles.Where(pair => !pair.Value.IsExists).ToArray())
                                handles.Remove(handle.Key);

                            // Удаление процесса из кэша если дескрипторов нет
                            if (handles.Count <= 0)
                                cache.Remove(pid);
                        }
                    });

                    // Проверка состояния канала
                    if (pipe == null)
                        continue;

                    // Снятие признака занятости
                    foreach (var port in Ports)
                        port.ProcessName = null;
                    
                    // Установка состояния занятости порта
                    foreach (var processInfo in cache)
                    {
                        var pid = processInfo.Key;
                        var handles = processInfo.Value;

                        foreach (var handle in handles.Values)
                        {
                            // Получаем имя дескриптора
                            var name = handle.Name;
                            if (string.IsNullOrEmpty(name))
                                continue;

                            // Есть ли порт с таким дескриптором
                            var port = Ports.FirstOrDefault(p => p.DeviceName == name);
                            if (port == null)
                                continue;

                            // Запрос имени процесса
                            try
                            {
                                var process = Process.GetProcessById(processInfo.Key);
                                try
                                {
                                    port.ProcessName = process.ProcessName;
                                }
                                finally
                                {
                                    process.Dispose();
                                }
                            }
                            catch
                            {
                                port.ProcessName = "Unknown";
                            }
                        }
                    }

                    // Обработка запроса списка процессов
                    await queryPids.Work(_ => cache.Keys.Sort((a, b) => a.CompareTo(b)).ToArray());

                    // Обработка запроса списка дескрипторов процесса
                    await queryHandles.Work(pid =>
                    {
                        // Список дескрипторов процесса
                        Dictionary<int, HandleCacheEntry> handles;

                        // Если отсутствует
                        if (!cache.TryGetValue((int)pid, out handles))
                            return new HandleInfo[0];

                        // Подготовка результата
                        return handles
                            .Select(pair => new HandleInfo(pair.Key, pair.Value))
                            .Sort((a, b) => a.Handle.CompareTo(b.Handle))
                            .ToArray();
                    });

                    // Пазуа опроса
                    await delay.Pause();
                }
            }
            finally
            {
                // Канал закрыт
                ClosePipe();

                // Освобождение ресурсов
                if (sysBufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(sysBufferPtr);

                // Сигнал остановки
                stopPoolEvent.DisposeSafe();

                // Сброс ожидателей запросов
                queryPids.Dispose();
            }
        }
        #endregion

        #region query
        /// <summary>
        /// Производит запрос идентификаторов процессов
        /// </summary>
        public Task<int[]> QueryPids() =>
            queryPids.Query();

        /// <summary>
        /// Производит запрос дескрипторов процесса
        /// </summary>
        public Task<HandleInfo[]> QueryHandles(int pid) =>
            queryHandles.Query(pid);

        #endregion
    }
}
