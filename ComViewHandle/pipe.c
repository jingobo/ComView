#include "pipe.h"
#include "handle.h"

void pipe_loop(void)
{
    // Инициализация модуля дескрипторов
    if (!handle_init())
        return;

    // Открытие канала
    const HANDLE pipe = CreateFile(
        TEXT("\\\\.\\pipe\\ComViewHandle"), 
        GENERIC_READ | GENERIC_WRITE, 
        FILE_SHARE_READ | FILE_SHARE_WRITE, 
        NULL, 
        OPEN_EXISTING, 
        FILE_ATTRIBUTE_NORMAL, 
        NULL);

    if (pipe == INVALID_HANDLE_VALUE)
        return;

    // Структура запроса
    typedef struct
    {
        // Идентификатор процесса
        DWORD process;
        // Запрашиваемый дескриптор
        DWORD handle;
    } request_t;

    // Для подавления предупреждения
    union
    {
        HANDLE handle;
        DWORD handle_req;
    } u;
    u.handle = 0;

    // Цикл обмена
    for (;;)
    {
        BOOL result;
        DWORD transfer;

        // Чтение запроса
        request_t request;
        result = ReadFile(pipe, &request, sizeof(request), &transfer, NULL);
        if (!result || transfer != sizeof(request))
            break;

        // Обработка
        u.handle_req = request.handle;
        const handle_info_t * const info = handle_info_query(request.process, u.handle);

        // Передача ответа
        result = WriteFile(pipe, info, sizeof(handle_info_t), &transfer, NULL);
        if (!result || transfer != sizeof(handle_info_t))
            break;
    }
}
