#pragma once
#pragma warning(push)
#pragma warning(disable: 4244)
#pragma warning(disable: 4267)
#include "aipbuffer.pb.h"
#pragma warning(pop)

typedef NTSTATUS (NTAPI *NtDuplicateHandle_f)(HANDLE SourceProcessHandle, HANDLE SourceHandle, HANDLE TargetProcessHandle, HANDLE *TargetHandle, ACCESS_MASK DesiredAccess, ULONG HandleAttributes, ULONG Options);
typedef NTSTATUS (NTAPI *NtDelayExecution_f)(BOOLEAN Alertable, LARGE_INTEGER *DelayInterval);
typedef NTSTATUS (NTAPI *NtQueryKey_f)(HANDLE KeyHandle, int KeyInformationClass, void *KeyInformation, ULONG Length, ULONG *ResultLength);
typedef NTSTATUS (NTAPI *NtClose_f)(HANDLE Handle);

class DynamicApiManager
{
  CNktFastMutex mutex;
  auto_array_ptr<byte_t> buffer;
  size_t buffer_size;
  auto_array_ptr<size_t> offsets;
  unsigned offset_count;
  size_t DuplicateHandleOffset;
  byte_t *executable_memory;
  void FinalInitialization();
public:
  DynamicApiManager(const AipBuffer &aipbuffer);
  NtDuplicateHandle_f GetDuplicateHandlePointer();
  NtDelayExecution_f GetDelayExecution();
  NtQueryKey_f GetQueryKey();
  NtClose_f GetCloseHandle();
};

HANDLE NktDuplicateHandle(HANDLE this_process, HANDLE target_process, HANDLE src);
void SleepForABit();
NTSTATUS NktQueryKey(HANDLE KeyHandle, int KeyInformationClass, void *KeyInformation, ULONG Length, ULONG *ResultLength);
NTSTATUS NktClose(HANDLE handle);
