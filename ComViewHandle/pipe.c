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
        const handle_name_t * const name = handle_query_name(request.process, u.handle);

        // Передача ответа
        result = WriteFile(pipe, name, sizeof(handle_name_t), &transfer, NULL);
        if (!result || transfer != sizeof(handle_name_t))
            break;
    }
}
