#include "pipe.h"


typedef struct _SYSTEM_HANDLE_INFORMATION
{
    ULONG ProcessId;
    UCHAR ObjectTypeNumber;
    UCHAR Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
    USHORT Handle;
    PVOID Object;
    ACCESS_MASK GrantedAccess;

} SYSTEM_HANDLE_INFORMATION, * PSYSTEM_HANDLE_INFORMATION;


// Точка входа в приложение
void main_entry(void)
{
    // Запуск цикла опроса канала
    pipe_loop();
    // Для завершения фоновгых потокв
    ExitProcess(0);
}
