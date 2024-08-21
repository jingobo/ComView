#include "handle.h"

// �������� ������� ZwQueryObject
typedef NTSTATUS(WINAPI * zw_query_object_t)(HANDLE Handle, ULONG oclass, PVOID dest, ULONG dest_size, PULONG written_size);

// ��������� ZwQueryObject
static zw_query_object_t zw_query_object;

// ������� ������ � ����������
static HANDLE event_work, event_ready;
// ���������� �������� ������
static HANDLE thread_handle = INVALID_HANDLE_VALUE;

// �������� �������������� �����������
static HANDLE handle_query;
// ������ ����� �����������
static ULONG handle_name_size;
// ����� ����� �����������
static handle_name_t handle_name;

// ������������� �������� ��������
static DWORD process_cur_id;
// ���������� �������� ��������
static HANDLE process_cur_handle;

// ����� ����� � ������� �����
static DWORD WINAPI thread_entry(LPVOID params)
{
    UNUSED(params);

    for (;;)
    {
        // �������� ������
        WaitForSingleObject(event_work, INFINITE);

        // ������
        if (zw_query_object(handle_query, 1, handle_name.buffer, sizeof(handle_name.buffer), &handle_name_size) != 0)
            handle_name_size = 0;

        // ������
        SetEvent(event_ready);
    }

    return 0;
}

const handle_name_t * handle_query_name(DWORD process_id, HANDLE handle)
{
    // ��������������
    handle_name.size = 0;

    // ������� ������� �� �����������
    if (process_cur_id == process_id)
        return &handle_name;

    // �������� ��������
    const HANDLE process_handle = OpenProcess(PROCESS_DUP_HANDLE, TRUE, process_id);
    if (process_handle == INVALID_HANDLE_VALUE)
        return &handle_name;

    // ������������ �����������
    if (!DuplicateHandle(process_handle, handle, process_cur_handle, &handle_query, 0, FALSE, DUPLICATE_SAME_ACCESS))
    {
        CloseHandle(process_handle);
        return &handle_name;
    }

    // ����� ���� �����������
    union
    {
        // ���
        UNICODE_STRING name;
        // �������������� ������
        uint8_t align[1024];
    } type_buffer;

    // ������ ���� �����������
    ULONG type_buffer_size;
    if (zw_query_object(handle_query, 2, &type_buffer, sizeof(type_buffer), &type_buffer_size) != 0)
        goto fin;

    // ��������� ������ �������� ������������
    if (lstrcmpW(TEXT("File"), type_buffer.name.Buffer) != 0)
        goto fin;

    // �������� �������� ������
    if (thread_handle == INVALID_HANDLE_VALUE)
        thread_handle = CreateThread(NULL, 0, thread_entry, NULL, 0, NULL);

    // ������ ���������� �������
    SetEvent(event_work);
    // �������� ���������� ������
    if (WaitForSingleObject(event_ready, 100) != WAIT_OBJECT_0)
    {
        // ��������� �������� ������
        TerminateThread(thread_handle, 1);
        CloseHandle(thread_handle);
        thread_handle = INVALID_HANDLE_VALUE;

        // �� ������ ������
        handle_name_size = 0;
    }

    // ����� ������� ���������� ������
    ResetEvent(event_ready);

    // �������� ������
    assert(handle_name_size >= 0);
    handle_name.size = (intptr_t)handle_name_size;

fin:
    CloseHandle(handle_query);
    CloseHandle(process_handle);

    return &handle_name;
}

BOOL handle_init(void)
{
    // ��������� ����������� �������
    {
        // �������� ������
        const HMODULE module = LoadLibrary(TEXT("ntdll.dll"));
        if (module == INVALID_HANDLE_VALUE)
            return FALSE;

        // ������ �������
        zw_query_object = (zw_query_object_t)GetProcAddress(module, "ZwQueryObject");
        if (zw_query_object == NULL)
            return FALSE;
    }

    // ������ �������� ��������
    process_cur_handle = GetCurrentProcess();
    process_cur_id = GetProcessId(process_cur_handle);

    // �������� �������
    event_work = CreateEvent(NULL, FALSE, FALSE, NULL);
    event_ready = CreateEvent(NULL, TRUE, FALSE, NULL);

    // ���� ����� ��� ��������
    return event_work != INVALID_HANDLE_VALUE &&
           event_ready != INVALID_HANDLE_VALUE;
}
