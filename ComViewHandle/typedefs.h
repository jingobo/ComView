#ifndef __TYPEDEFS_H
#define __TYPEDEFS_H

// Исключите редко используемые компоненты из заголовков Windows
#define WIN32_LEAN_AND_MEAN
// Файлы заголовков Windows
#include <stdint.h>
#include <assert.h>
#include <windows.h>
#include <ntsecapi.h>

// Не используемый символ
#define UNUSED(x)       ((void)(x))

#endif // __TYPEDEFS_H
