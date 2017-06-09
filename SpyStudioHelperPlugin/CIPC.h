#pragma once

#include "CustomHookData.h"
#include "Buffer.h"
#pragma warning(push)
#pragma warning(disable: 4244)
#pragma warning(disable: 4267)
#include "aipbuffer.pb.h"
#pragma warning(pop)
#include "DynamicApiManager.h"
#include "DotNetProfilerMgr.h"
#include <vector>
#include <string>

#ifdef SendMessage
#undef SendMessage
#endif

class CoalescentIPC;

class CIPCInstanceHolder
{
  CoalescentIPC *p;
  CNktFastMutex mutex;
public:
  CIPCInstanceHolder(): p(0) {}
  bool IsInitialized()
  {
    return !!p;
  }
  void TryInit(INktHookInfoPtr hi, const AipBuffer &aipbuffer);
  void Uninit();
  operator CoalescentIPC &()
  {
    return *p;
  }
  CoalescentIPC *operator->()
  {
    return p;
  }
};

extern CIPCInstanceHolder global_cipc;

struct HookProperties
{
  bool precall_included,
       postcall_included,
       is_secondary_hook,
       has_secondary_hook;
  unsigned secondary_hook_index;
  HookId_t primary_hook_id;
  CustomHook *custom_hook;
  HookProperties():
    precall_included(1),
    postcall_included(1),
    is_secondary_hook(0),
    has_secondary_hook(0),
    custom_hook(0),
    secondary_hook_index(0)
  {}
};

#ifdef _DEBUG
class DebuggableMutex{
  CNktFastMutex mutex;
  const char *locked_at;
  DWORD tid;
public:
  DebuggableMutex(): locked_at(0){}
  void Lock(const char *location=0)
  {
    mutex.Lock();
    locked_at=location;
    tid=GetCurrentThreadId();
  }
  void Unlock()
  {
    mutex.Unlock();
  }
};

class AutoDebuggableMutex{
  DebuggableMutex &mutex;
public:
  AutoDebuggableMutex(DebuggableMutex &m, const char *location=0): mutex(m)
  {
    mutex.Lock(location);
  }
  ~AutoDebuggableMutex()
  {
    mutex.Unlock();
  }
};

#define STRINGIZE(x) #x
#define STRINGIZE_MACRO_VALUE(x) STRINGIZE(x)
#define LOCK_MUTEX(x) AutoDebuggableMutex adm__mutex(x, __FILE__ ":" STRINGIZE_MACRO_VALUE(__LINE__) ", in function " __FUNCTION__)
#else
#define DebuggableMutex CNktFastMutex
#define AutoDebuggableMutex CNktAutoFastMutex
#define LOCK_MUTEX(x) CNktAutoFastMutex adm__mutex(&(x))
#endif

struct SynchronizationNode
{
  AcquiredBuffer buffer;
  volatile bool ready;
  HANDLE creating_thread;
  SynchronizationNode *next;
  SynchronizationNode(BufferList &bl, CoalescentIPC &cipc);
  void Discard()
  {
    buffer.Discard();
  }
  void SetReady()
  {
    ready = 1;
  }
  ~SynchronizationNode()
  {
    CloseHandle(creating_thread);
  }
};

class CoalescentIPC;

class SerializerNodeInterface
{
  SynchronizationNode *node;
  CoalescentIPC *cipc;
public:
  SerializerNodeInterface(CoalescentIPC &cipc);
  ~SerializerNodeInterface();
  AcquiredBuffer &GetBuffer()
  {
    return node->buffer;
  }
};

class LazyHANDLE
{
  HANDLE handle;
  bool initialized;
public:
  LazyHANDLE();
  ~LazyHANDLE();
  //Guaranteed to succeed or throw.
  HANDLE operator()(INktHookInfoPtr hi);
};

class Clock{
	double precision;
public:
	Clock();
	double operator()() const;
};

class TimeMeasurement{
  double total;
  CNktFastMutex mutex;
  void WriteReport();
public:
  Clock c;
  TimeMeasurement();
  ~TimeMeasurement();
  void AddTime(double t);
};

struct wcshash
{
  template <typename T>
  size_t operator()(const T *s, size_t size = 0) const
  {
    size_t ret = 0xF00BA8;
    const size_t factor = 
#ifdef _M_X64
      0x8BADF00DDEADBEEF;
#else
      0xDEADBEEF;
#endif
    if (!size)
    {
      for (; *s; s++)
      {
        ret *= factor;
        ret ^= tolower(*s);
      }
    }
    else
    {
      for (; size; size--, s++)
      {
        ret *= factor;
        ret ^= tolower(*s);
      }
    }
    return ret;
  }
};

struct ServerSettings
{
  bool omit_call_stack;
  bool monitor_dot_net_gc;
  bool monitor_dot_net_jit;
  bool monitor_dot_net_objects;
  bool monitor_dot_net_exceptions;
  ServerSettings(): omit_call_stack(0) {}
  void Init(boost::uint32_t);
};

class CoalescentIPC
{
  std::auto_ptr<DynamicApiManager> dynamic_api_manager;
public:
  friend class AcquiredBuffer;
  friend class SerializerNodeInterface;

  static const size_t MAX_BUFFER_SIZE = 1 << 22; //4 MiB
  size_t configured_buffer_size;
private:
  bool ok;
  CNktFastMutex mutex;
  bool mutex_initialized;
  CNktMutex global_mutex;
  HANDLE file_map,
         own_process,
         writer_thread;
  DWORD writer_thread_id;
  LazyHANDLE target_process;
  INktHookInfoPtr hi;
  void *buffer;
  DWORD last_error;
  const char *error_string;
  size_t initial_writing_position;
  size_t writing_position;
  //Statistics:
  double buffers_transferred;
  unsigned events_transterred;
  double total_time;
  CNktFastMutex time_mutex;

  BufferList buffer_list;
  typedef dictionary_t<HookId_t, HookProperties> properties_t;
  properties_t properties;
  dictionary_t<long, unsigned> CoCreateInstance_counters;
  double initialization_time,
    initialization_performance_counter;
  Clock clock;
  bool no_secondary_hooks;
  bool installer_hook;
  bool in_thinapp_process;
  boost::uint64_t order_id;
  boost::uint64_t long_process_id;
  volatile mword_t active_hooks;
  
  struct wcseq
  {
    bool operator()(const wchar_t *, const wchar_t *) const;
  };
  std::vector<std::wstring> system_modules_vector;
  hashset_t<const wchar_t *, wcshash, wcseq> system_modules;

  void InitializeMutex();
  void AllocateSharedBuffer();
  HANDLE TransferHandle(HANDLE);
  HANDLE TransferSharedBuffer()
  {
    return TransferHandle(file_map);
  }
  void TransferMutex();
  void FreeSharedBuffer();
  //Perform actual IPC.
  boost::uint32_t length_of_current_send;
  void SendMessage(bool synchronously);
  void IncrementEventCount();
  void WriteBuffer(const void *buffer, size_t size);
  void *GetBuffer()
  {
    return buffer;
  }

  /************************************************************************/
  //This next area handles synchronization and reordering of buffer writes.
  /************************************************************************/

  volatile bool kill_thread;
  static DWORD WINAPI thread(LPVOID);

  friend struct SynchronizationList;
  class SynchronizationList
  {
    DebuggableMutex mutex;
    SynchronizationNode *head,
      *tail;
  public:
    SynchronizationList();
    ~SynchronizationList();
    SynchronizationNode *PushBack(CoalescentIPC &cipc);
    bool IsEmpty()
    {
      LOCK_MUTEX(mutex);
      return !tail;
    }
    //Blocking. Holds control until list is non-empty.
    SynchronizationNode *PeekFront();
    //Blocking. Holds control until list is non-empty.
    SynchronizationNode *PopFront();
    SynchronizationNode *PopFrontUnlocked();
    //Blocking. Holds control until list is non-empty.
    void FreeFront()
    {
      SynchronizationNode *node = PopFront();
      node->Discard();
      delete node;
    }
    DebuggableMutex &GetMutex()
    {
      return mutex;
    }
    
    class iterator
    {
      friend class SynchronizationList;
      SynchronizationNode *node;
      iterator(SynchronizationNode *node): node(node){}
    public:
      iterator operator++()
      {
        node = node->next;
        return *this;
      }
      iterator operator++(int)
      {
        iterator ret(*this);
        node = node->next;
        return ret;
      }
      SynchronizationNode *operator*() const
      {
        return node;
      }
      bool operator==(const iterator &i) const
      {
        return node == i.node;
      }
      bool operator!=(const iterator &i) const
      {
        return !(*this == i);
      }
    };

    iterator begin()
    {
      return iterator(this->head);
    }
    iterator end()
    {
      return iterator(0);
    }

  };
  SynchronizationList* synchronization_list;

  //Call only from thread()!
  SynchronizationNode *ObtainNodes();
  //Call only from thread()!
  void WriteNodes(SynchronizationNode *);

  /************************************************************************/
  //It ends here.
  /************************************************************************/

  void Initialize_system_modules(const AipBuffer &aipbuffer);
  void Initialize_in_thinapp_process();
public:
  CoalescentIPC(INktHookInfoPtr hi, const AipBuffer &aipbuffer);
  ~CoalescentIPC();

  TimeMeasurement tm;
  ServerSettings server_settings;
  DotNetProfilerMgr dot_net_profiler_mgr;

  bool Good()
  {
    return ok;
  }
  operator bool()
  {
    return ok;
  }
  DWORD GetError(const char **error_string = 0)
  {
    if (!!error_string)
      *error_string = this->error_string;
    return last_error;
  }
  //Sends and releases the current buffer, allocates a new buffer, and resets writing_position.
  void Send(bool synchronously = false);

  BufferList &GetBufferList()
  {
    return buffer_list;
  }

  void AddHookPropertyAndActivateHook(HookId_t hook_id, const HookProperties &prop);

  HookProperties &GetHookProperty(HookId_t hook_id)
  {
    return properties[hook_id];
  }
  HookProperties *TryGetHookProperty(HookId_t hook_id)
  {
    properties_t::iterator i = properties.find(hook_id);
    return i == properties.end() ? 0 : &i->second;
  }
  bool HasSecondaryHook(HookId_t primary_hook_id);
  bool IsSystemModule(const wchar_t *name) const;

  //returns the new value
  unsigned IncrementCoCreateInstanceCounter(long thread_id);
  unsigned DecrementCoCreateInstanceCounter(long thread_id);
  unsigned GetCoCreateInstanceCounter(long thread_id);

  bool GetInstallerHookBit()
  {
    return installer_hook;
  }
  void ResetInstallerHookBit()
  {
    installer_hook = 0;
  }

  double MillisecondsSinceInitialization(){
    return clock() - initialization_performance_counter;
  }

  //Hopefully this will return a high resolution, system-wide timestamp.
  double GlobalTimestamp()
  {
    return initialization_time + clock() - initialization_performance_counter;
  }

  void SendThinAppCreateProcessMessage(unsigned long pid);

  void AddTime(double t)
  {
    CNktAutoFastMutex am(&time_mutex);
    total_time += t;
  }
  bool InThinAppProcess() const
  {
    return in_thinapp_process;
  }

  DynamicApiManager &GetDynApiMan()
  {
    if (!dynamic_api_manager.get())
      throw CIPC_DynamicApiException();
    return *dynamic_api_manager;
  }
  void InitializeDynamicApis(const AipBuffer &aipbuffer)
  {
    if (dynamic_api_manager.get())
      return;
    dynamic_api_manager.reset(new DynamicApiManager(aipbuffer));
    StartThread();
  }
  void *RequestExecutableMemory(size_t size);
  void StartThread()
  {
    ResumeThread(writer_thread);
  }
  INktModulePtr FindModule(mword_t ip);
  void NotifyThreadDetached(DWORD);
  DWORD GetDotNetMonitoringFlags();
  //true if any hooks are still active
  bool DeactivateAHook(){
    return !!InterlockedDecrement(&active_hooks);
  }
  bool CallerThreadIsWriterThread(){
    return GetCurrentThreadId() == writer_thread_id;
  }
};
