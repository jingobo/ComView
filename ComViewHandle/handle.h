#ifndef __HANDLE_H
#define __HANDLE_H

#include "typedefs.h"

// Перечисление статусов операции
enum
{
    // Успешно
    HANDLE_STATUS_SUCCESS,
    
    // Текущий процесс не обрабатывается
    HANDLE_STATUS_SAME_PROCESS,
    // Не удалось открыть процесс
    HANDLE_STATUS_OPEN_PROCESS,

    // Не удалось создать дубликат дескриптора
    HANDLE_TARGET_DUPLICATE,
    // Не удалось определить тип дескриптора
    HANDLE_TARGET_QUERY_TYPE,
    // Не корректный тип дескриптора
    HANDLE_TARGET_INVALID_TYPE,
    // Не удалось определить имя дескриптора
    HANDLE_TARGET_QUERY_NAME,
};

// Структура имени дескриптора
typedef struct
{
    // Статус операции
    uint32_t status;
    // Длинна
    uint32_t size;
    // Буфер
    uint32_t buffer[512 / sizeof(uint32_t)];
} handle_name_t;

// Инициализация модуля
BOOL handle_init(void);
// Запрос имени дескриптора
const handle_name_t * handle_query_name(DWORD process_id, HANDLE handle);

#endif // __HANDLE_H
