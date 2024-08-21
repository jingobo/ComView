#ifndef __HANDLE_H
#define __HANDLE_H

#include "typedefs.h"

// ��������� ����� �����������
typedef struct
{
    // ������
    uint32_t size;
    // �����
    uint32_t buffer[512 / sizeof(uint32_t)];
} handle_name_t;

// ������������� ������
BOOL handle_init(void);
// ������ ����� �����������
const handle_name_t * handle_query_name(DWORD process_id, HANDLE handle);

#endif // __HANDLE_H
