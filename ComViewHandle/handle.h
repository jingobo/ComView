#ifndef __HANDLE_H
#define __HANDLE_H

#include "typedefs.h"

// Структура имени дескриптора
typedef struct
{
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
