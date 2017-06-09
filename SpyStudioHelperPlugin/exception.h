#pragma once

class CIPC_Exception: public std::exception
{
  DWORD last_error;
  const char *error_string;
public:
  CIPC_Exception(DWORD error, const char *error_string): last_error(error), error_string(error_string) {}
  virtual ~CIPC_Exception() {}
  DWORD GetLastError(const char **e = 0)
  {
    if (!!e)
      *e = error_string;
    return last_error;
  }
};

#define DEFINE_CIPC_EXCEPTION(name, what_string)                                    \
class name: public CIPC_Exception{                                                  \
public:                                                                             \
  name(): CIPC_Exception(ERROR_UNIDENTIFIED_ERROR, "") {}                           \
  name(DWORD error, const char *error_string):CIPC_Exception(error, error_string){} \
  const char *what() const                                                          \
  {                                                                                 \
    return what_string;                                                             \
  }                                                                                 \
}

DEFINE_CIPC_EXCEPTION(CIPC_InializationException, "An object initialization failed.");
DEFINE_CIPC_EXCEPTION(CIPC_SharedBufferAllocationException, "Shared buffer allocation failed.");
DEFINE_CIPC_EXCEPTION(CIPC_DataErrorException, "The input data is invalid.");
DEFINE_CIPC_EXCEPTION(CIPC_OutOfMemoryException, "Out of memory.");
DEFINE_CIPC_EXCEPTION(CIPC_IPCException, "IPC failed.");
DEFINE_CIPC_EXCEPTION(CIPC_SynchonizationException, "Failed to lock synchronization object.");
DEFINE_CIPC_EXCEPTION(CIPC_DynamicApiException, "The dynamic API manager wasn't initialized.");

#undef DEFINE_CIPC_EXCEPTION

#define THROW_SPECIAL_CIPC_OUTOFMEMORY(s) throw CIPC_OutOfMemoryException(ERROR_NOT_ENOUGH_MEMORY, (s))
#define THROW_CIPC_OUTOFMEMORY THROW_SPECIAL_CIPC_OUTOFMEMORY("Not enough memory.")
