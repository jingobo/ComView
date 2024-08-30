#include "pipe.h"
#include "handle.h"

void pipe_loop(void)
{
    // ������������� ������ ������������
    if (!handle_init())
        return;

    // �������� ������
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

    // ��������� �������
    typedef struct
    {
        // ������������� ��������
        DWORD process;
        // ������������� ����������
        DWORD handle;
    } request_t;

    // ��� ���������� ��������������
    union
    {
        HANDLE handle;
        DWORD handle_req;
    } u;
    u.handle = 0;

    // ���� ������
    for (;;)
    {
        BOOL result;
        DWORD transfer;

        // ������ �������
        request_t request;
        result = ReadFile(pipe, &request, sizeof(request), &transfer, NULL);
        if (!result || transfer != sizeof(request))
            break;

        // ���������
        u.handle_req = request.handle;
        const handle_info_t * const info = handle_info_query(request.process, u.handle);

        // �������� ������
        result = WriteFile(pipe, info, sizeof(handle_info_t), &transfer, NULL);
        if (!result || transfer != sizeof(handle_info_t))
            break;
    }
}
