#ifndef __HANDLE_H
#define __HANDLE_H

#include "typedefs.h"

// ������������ �������� ��������
enum
{
    // �������
    HANDLE_STATUS_SUCCESS,
    
    // ������� ������� �� ��������������
    HANDLE_STATUS_SAME_PROCESS,
    // �� ������� ������� �������
    HANDLE_STATUS_OPEN_PROCESS,

    // �� ������� ������� �������� �����������
    HANDLE_TARGET_DUPLICATE,
    // �� ������� ���������� ��� �����������
    HANDLE_TARGET_QUERY_TYPE,
    // �� ���������� ��� �����������
    HANDLE_TARGET_INVALID_TYPE,
    // �� ������� ���������� ��� �����������
    HANDLE_TARGET_QUERY_NAME,
};

// ��������� ���������� � �����������
typedef struct
{
    // ������ ��������
    uint32_t status;
    // ������
    uint32_t size;
    // �����
    uint32_t buffer[512 / sizeof(uint32_t)];
} handle_info_t;

// ������������� ������
BOOL handle_init(void);
// ������ ���������� � �����������
const handle_info_t * handle_info_query(DWORD process_id, HANDLE handle);

#endif // __HANDLE_H
