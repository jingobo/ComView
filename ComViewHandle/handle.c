#include "handle.h"

// Прототип функции ZwQueryObject
typedef NTSTATUS(WINAPI * zw_query_object_t)(HANDLE Handle, ULONG oclass, PVOID dest, ULONG dest_size, PULONG written_size);

// Экземпляр ZwQueryObject
static zw_query_object_t zw_query_object;

// События работы и готовности
static HANDLE event_work, event_ready;
// Дескриптор фонового потока
static HANDLE thread_handle = INVALID_HANDLE_VALUE;

// Значение запрашиваемого дескриптора
static HANDLE handle_query;
// Длинна имени дескриптора
static ULONG handle_name_size;
// Буфер информации о дескрипторе
static handle_info_t handle_info;

// Идентификатор текущего процесса
static DWORD process_cur_id;
// Дескриптор текущего процесса
static HANDLE process_cur_handle;

// Точка входа в фоновый поток
static DWORD WINAPI thread_entry(LPVOID params)
{
    UNUSED(params);

    for (;;)
    {
        // Ожидание работы
        WaitForSingleObject(event_work, INFINITE);

        // Запрос
        if (zw_query_object(handle_query, 1, handle_info.buffer, sizeof(handle_info.buffer), &handle_name_size) != 0)
            handle_name_size = 0;

        // Готово
        SetEvent(event_ready);
    }

    return 0;
}

// Производит копирование участка памяти
static void handle_memcpy(void *dest, const void *source, size_t size)
{
    uint8_t *dst = (uint8_t *)dest;
    uint8_t *src = (uint8_t *)source;

    for (; size > 0; size--, dst++, src++)
        *dst = *src;
}

const handle_info_t * handle_info_query(DWORD process_id, HANDLE handle)
{
    // Предварительно
    handle_info.size = 0;

    // Текущий процесс не обслуживаем
    if (process_cur_id == process_id)
    {
        handle_info.status = HANDLE_STATUS_SAME_PROCESS;
        return &handle_info;
    }

    // Открытие процесса
    const HANDLE process_handle = OpenProcess(PROCESS_DUP_HANDLE, TRUE, process_id);
    if (process_handle == INVALID_HANDLE_VALUE)
    {
        handle_info.status = HANDLE_STATUS_OPEN_PROCESS;
        return &handle_info;
    }

    // Клонирование дескриптора
    if (!DuplicateHandle(process_handle, handle, process_cur_handle, &handle_query, 0, FALSE, DUPLICATE_SAME_ACCESS))
    {
        handle_info.status = HANDLE_TARGET_DUPLICATE;
        CloseHandle(process_handle);
        return &handle_info;
    }

    // Буфер типа дескриптора
    union
    {
        // Имя
        UNICODE_STRING name;
        // Дополнительная память
        uint8_t align[1024];
    } type_buffer;

    // Запрос типа дескриптора
    ULONG type_buffer_size;
    if (zw_query_object(handle_query, 2, &type_buffer, sizeof(type_buffer), &type_buffer_size) != 0)
    {
        handle_info.status = HANDLE_TARGET_QUERY_TYPE;
        goto fin;
    }

    // Обработка только файловых дескрипторов
    if (lstrcmpW(TEXT("File"), type_buffer.name.Buffer) != 0)
    {
        handle_info.status = HANDLE_TARGET_INVALID_TYPE;
        goto fin;
    }

    // Создание фонового потока
    if (thread_handle == INVALID_HANDLE_VALUE)
        thread_handle = CreateThread(NULL, 0, thread_entry, NULL, 0, NULL);

    // Сигнал готовности запроса
    SetEvent(event_work);
    // Ожидание готовности ответа
    if (WaitForSingleObject(event_ready, 100) != WAIT_OBJECT_0)
    {
        // Аварийное закрытие потока
        TerminateThread(thread_handle, 1);
        CloseHandle(thread_handle);
        thread_handle = INVALID_HANDLE_VALUE;

        // На всякий случай
        handle_name_size = 0;
        handle_info.status = HANDLE_TARGET_QUERY_NAME;
    }
    else
    {
        handle_info.size = (intptr_t)handle_name_size;
        handle_info.status = (handle_info.size >= 18) ?
            HANDLE_STATUS_SUCCESS :
            HANDLE_TARGET_QUERY_NAME;
    }

    // Сброс сигнала готовности ответа
    ResetEvent(event_ready);
fin:

    // Закрытие дескрипторов
    CloseHandle(handle_query);
    CloseHandle(process_handle);

    return &handle_info;
}

BOOL handle_init(void)
{
    // Подгрузка необходимых функций
    {
        // Загрузка модуля
        const HMODULE module = LoadLibrary(TEXT("ntdll.dll"));
        if (module == INVALID_HANDLE_VALUE)
            return FALSE;

        // Запрос функции
        zw_query_object = (zw_query_object_t)GetProcAddress(module, "ZwQueryObject");
        if (zw_query_object == NULL)
            return FALSE;
    }

    // Данные текущего процесса
    process_cur_handle = GetCurrentProcess();
    process_cur_id = GetProcessId(process_cur_handle);

    // Создание событий
    event_work = CreateEvent(NULL, FALSE, FALSE, NULL);
    event_ready = CreateEvent(NULL, TRUE, FALSE, NULL);

    // Если вдруг нет ресурсов
    return event_work != INVALID_HANDLE_VALUE &&
           event_ready != INVALID_HANDLE_VALUE;
}
