#include "pipe.h"

// Точка входа в приложение
void main_entry(void)
{
    // Запуск цикла опроса канала
    pipe_loop();
    // Для завершения фоновых потокв
    ExitProcess(0);
}
