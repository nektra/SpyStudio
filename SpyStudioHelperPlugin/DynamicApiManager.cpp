#include "stdafx.h"
#include "CommonFunctions.h"
#include "DynamicApiManager.h"
#include "CIPC.h"

enum FunctionOffsets
{
  NtDuplicateHandle_offset = 0,
  NtDelayExecution_offset,
  NtQueryKey_offset,
  NtClose_offset,
};

#if defined _M_IX86
#define PLATFORM_BITS 32
#elif defined _M_X64
#define PLATFORM_BITS 64
#else
#error
#endif

#define BUFFER_NAME CONCAT(buffer, PLATFORM_BITS)
#define OFFSETS_NAME CONCAT(offsets, PLATFORM_BITS)

DynamicApiManager::DynamicApiManager(const AipBuffer &aipbuffer)
{
  const std::string &code_buffer = aipbuffer.BUFFER_NAME();

  buffer_size = code_buffer.size();
  buffer.reset(new byte_t[buffer_size]);
  std::copy(code_buffer.begin(), code_buffer.end(), buffer.get());
  const google::protobuf::RepeatedField<google::protobuf::uint32> &code_offsets = aipbuffer.OFFSETS_NAME();
  offsets.reset(new size_t[code_offsets.size()]);
  std::copy(code_offsets.begin(), code_offsets.end(), offsets.get());
  executable_memory = 0;
}

NtDuplicateHandle_f DynamicApiManager::GetDuplicateHandlePointer()
{
  FinalInitialization();
  return (NtDuplicateHandle_f)(executable_memory + offsets[NtDuplicateHandle_offset]);
}

NtDelayExecution_f DynamicApiManager::GetDelayExecution()
{
  FinalInitialization();
  return (NtDelayExecution_f)(executable_memory + offsets[NtDelayExecution_offset]);
}

NtQueryKey_f DynamicApiManager::GetQueryKey()
{
  FinalInitialization();
  return (NtQueryKey_f)(executable_memory + offsets[NtQueryKey_offset]);
}

NtClose_f DynamicApiManager::GetCloseHandle()
{
  FinalInitialization();
  return (NtClose_f)(executable_memory + offsets[NtClose_offset]);
}

void DynamicApiManager::FinalInitialization()
{
  CNktAutoFastMutex am(&mutex);
  if (executable_memory)
    return;
  executable_memory = (byte_t *)global_cipc->RequestExecutableMemory(buffer_size);
  memcpy(executable_memory, buffer.get(), buffer_size);
}

#define DUPLICATE_SAME_ACCESS 0x00000002

HANDLE NktDuplicateHandle(HANDLE this_process, HANDLE target_process, HANDLE src)
{
  NtDuplicateHandle_f NtDuplicateHandle = global_cipc->GetDynApiMan().GetDuplicateHandlePointer();
  if (!NtDuplicateHandle)
    return 0;
  HANDLE ret;
  const ULONG flags = DUPLICATE_SAME_ACCESS;
  NTSTATUS result = NtDuplicateHandle(this_process, src, target_process, &ret, 0, 0, flags);
  if (!NT_SUCCESS(result))
    return 0;
  return ret;
}

void SleepForABit()
{
  NtDelayExecution_f NtDelayExecution = global_cipc->GetDynApiMan().GetDelayExecution();
  if (!NtDelayExecution)
    return;
  LARGE_INTEGER delay;
  delay.QuadPart = -10;
  NtDelayExecution(FALSE, &delay);
}

NTSTATUS NktQueryKey(HANDLE KeyHandle, int KeyInformationClass, void *KeyInformation, ULONG Length, ULONG *ResultLength)
{
  NtQueryKey_f NtQueryKey = global_cipc->GetDynApiMan().GetQueryKey();
  if (!NtQueryKey)
    return -1;
  return NtQueryKey(KeyHandle, KeyInformationClass, KeyInformation, Length, ResultLength);
}

NTSTATUS NktClose(HANDLE handle)
{
  NtClose_f NtClose = global_cipc->GetDynApiMan().GetCloseHandle();
  if (!NtClose)
    return -1;
  return NtClose(handle);
}
