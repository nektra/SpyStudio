#include "stdafx.h"
#include "Protocol.h"
#include "exception.h"
#include "CustomHookData.h"
#include "CIPC.h"
#include "DynamicApiManager.h"
#include <ctime>

const unsigned __int32 full32mask = 0xFFFFFFFFU;

//#define SEND_OUTPUT_TO_FILE
//#define REPORT_STATISTICS
//#define DISABLE_THREAD

#if defined SEND_OUTPUT_TO_FILE || defined REPORT_STATISTICS
#include <fstream>
#endif

#if 0
#include <fstream>
#include <shlobj.h>

#pragma comment(lib, "shell32.lib")
#endif

LazyHANDLE::LazyHANDLE(): initialized(0), handle(0){}

LazyHANDLE::~LazyHANDLE()
{
  if (initialized)
    NktClose(handle);
}

HANDLE LazyHANDLE::operator()(INktHookInfoPtr hi)
{
  if (initialized)
    return handle;
  handle = (HANDLE)hi->SendCustomMessage(CUSTOM_MESSAGE_GET_SERVER_HANDLE, 0, VARIANT_TRUE);
  if (!valid_handle(handle))
    throw CIPC_InializationException(ERROR_UNIDENTIFIED_ERROR, "Initialization of target_process failed.");
  initialized = 1;
  return handle;
}


Clock::Clock(){
    LARGE_INTEGER li;
	precision = (!QueryPerformanceFrequency(&li)) ? 0 : 1000.0/double(li.QuadPart);
}

double Clock::operator()() const{
	if (!precision)
		return (double)GetTickCount();
	LARGE_INTEGER li;
	QueryPerformanceCounter(&li);
	return double(li.QuadPart) * precision;
}

const HANDLE SELF_PROCESS_MAGIC_NUMBER = (HANDLE)-1;

boost::uint64_t get_long_pid(time_t time)
{
  HANDLE proc = self_process();
  FILETIME times[4];
  BOOL result = GetProcessTimes(
    proc,
    times,
    times + 1,
    times + 2,
    times + 3
  );
  CloseHandle(proc);
  boost::uint32_t time_val;
  if (!result)
    time_val = (boost::uint32_t)(time & full32mask);
  else
  {
    time_val = times->dwLowDateTime;
    time_val >>= 16;
    time_val |= ((boost::uint32_t)times->dwHighDateTime << 16) & full32mask;
  }
  boost::uint64_t ret = GetCurrentProcessId();
  ret <<= 32;
  ret |= time_val;
  return ret;
  
}

CoalescentIPC::CoalescentIPC(INktHookInfoPtr hi, const AipBuffer &aipbuffer):
  hi(hi)
{
  time_t time = ::time(nullptr);
  initialization_time = (double)time;
  initialization_performance_counter = clock();
  no_secondary_hooks = 1;
  installer_hook = 1;
  in_thinapp_process = 0;
  long_process_id = get_long_pid(time);
  active_hooks = 0;

  server_settings.Init((boost::uint32_t)hi->SendCustomMessage(CUSTOM_MESSAGE_REQUEST_SETTINGS, 0, VARIANT_TRUE));
  configured_buffer_size = MAX_BUFFER_SIZE;
  if (server_settings.omit_call_stack)
    configured_buffer_size /= 5;

  target_process(hi);

  synchronization_list = new SynchronizationList;
  ok = 0;
  file_map = INVALID_HANDLE_VALUE;
  if (!valid_handle(own_process = self_process()) && own_process != SELF_PROCESS_MAGIC_NUMBER)
  {
    last_error = GetLastError();
    error_string = "Initialization of own_process failed.";
    throw CIPC_InializationException(last_error, error_string);
  }
  mutex_initialized = 0;
  order_id = 0;
  AllocateSharedBuffer();

  kill_thread = 0;
#ifndef DISABLE_THREAD
  writer_thread = CreateThread(0, 0, thread, this, CREATE_SUSPENDED, &writer_thread_id);
  if (!valid_handle(writer_thread))
  {
    last_error = GetLastError();
    error_string = "Failed to create thread to write to shared buffer.";
    throw CIPC_InializationException(last_error, error_string);
  }
  hi->ThreadIgnore(writer_thread_id, VARIANT_TRUE);
#else
  writer_thread = 0;
#endif

  Initialize_system_modules(aipbuffer);

  ok = 1;
  buffers_transferred = 0;
  events_transterred = 0;
  total_time = 0;

  Initialize_in_thinapp_process();
}

template <size_t N>
static std::string get_report_path(const char (&S)[N])
{
  char path[MAX_PATH + N + 1];
  {
    WCHAR wpath[MAX_PATH];
    wpath[0] = 0;
    if (SHGetFolderPath(0, CSIDL_DESKTOPDIRECTORY, 0, SHGFP_TYPE_CURRENT, wpath))
      return std::string(".") + "\\" + S;
    size_t i = 0;
    for (; wpath[i]; i++)
      path[i] = (char)wpath[i];
    path[i++] = '\\';
    for (size_t j = 0; S[j]; i++, j++)
      path[i] = S[j];
    path[i] = 0;
  }
  return path;
}

CoalescentIPC::~CoalescentIPC()
{
#ifndef DISABLE_THREAD
  if (valid_handle(writer_thread))
  {
    kill_thread = 1;
    if (!CallerThreadIsWriterThread())
      WaitForSingleObject(writer_thread, INFINITE);
    NktClose(writer_thread);
  }
#endif

  delete synchronization_list;
  synchronization_list = NULL;

  for (auto &p : properties)
    delete p.second.custom_hook;
  properties.clear();
  if (valid_handle(own_process))
    NktClose(own_process);
  FreeSharedBuffer();
  DBGPRINT("Events per buffer: %f", events_transterred / buffers_transferred);
}

void CoalescentIPC::Initialize_system_modules(const AipBuffer &aipbuffer){
  for (auto &mod : aipbuffer.systemmodules()){
    std::wstring temp;
    temp.resize(mod.size());
#ifdef _DEBUG
    {
      bool all = 1;
      for (size_t i = 0; i < mod.size() && all; i++)
        all = all && (unsigned char)(mod[i]) < 0x80;
      assert(all);
    }
#endif
    std::copy(mod.begin(), mod.end(), temp.begin());
    system_modules_vector.push_back(temp);
    this->system_modules.insert(system_modules_vector.back().c_str());
  }
}

void CoalescentIPC::Initialize_in_thinapp_process(){
  std::string ntdll = "nt0_dll.dll";
  std::string ntdll64 = "nt0_dll64.dll";
  bool found = 0;
  INktModulesEnumPtr mod_enum = hi->CurrentProcess()->Modules();
  for (int i = 0; i < mod_enum->Count; i++)
  {
    INktModulePtr mod = mod_enum->GetAt(i);
    _bstr_t bname = mod->Name;
    std::wstring name((const wchar_t *)bname, bname.length());
    std::transform(name.begin(), name.end(), name.begin(), tolower);
    if (ends_with(name, ntdll) || ends_with(name, ntdll64))
    {
      in_thinapp_process = 1;
      return;
    }
  }
}

INktModulePtr CoalescentIPC::FindModule(mword_t ip){
  return hi->CurrentProcess()->ModuleByAddress(ip, smFindContaining);
}

void CoalescentIPC::NotifyThreadDetached(DWORD tid)
{
  dot_net_profiler_mgr.NotifyThreadDetached(tid);
}

DWORD CoalescentIPC::GetDotNetMonitoringFlags()
{
  DWORD ret = 0;
  if (!server_settings.omit_call_stack)
    ret |= COR_PRF_ENABLE_STACK_SNAPSHOT;
  if (server_settings.monitor_dot_net_gc)
    ret |= COR_PRF_MONITOR_GC;
  if (server_settings.monitor_dot_net_jit)
    ret |= COR_PRF_MONITOR_JIT_COMPILATION;
  if (server_settings.monitor_dot_net_objects)
    ret |= COR_PRF_MONITOR_OBJECT_ALLOCATED|COR_PRF_ENABLE_OBJECT_ALLOCATED;
  if (server_settings.monitor_dot_net_exceptions)
    ret |= COR_PRF_MONITOR_EXCEPTIONS|COR_PRF_MONITOR_CLR_EXCEPTIONS;
  return ret;
}

bool CoalescentIPC::wcseq::operator()(const wchar_t *a, const wchar_t *b) const
{
  for (;tolower(*a) == tolower(*b); a++, b++)
    if (!*a)
      return 1;
  return 0;
}

CIPCInstanceHolder global_cipc;

void CIPCInstanceHolder::TryInit(INktHookInfoPtr hi, const AipBuffer &aipbuffer)
{
  if (!p)
  {
    p = new CoalescentIPC(hi, aipbuffer);
  }
}

void CIPCInstanceHolder::Uninit()
{
  CNktAutoFastMutex am(&mutex);
  delete p;
  p = 0;
}

#define BIT(x) (1<<(x))

void ServerSettings::Init(boost::uint32_t flag)
{
  unsigned idx = 0;
  bool *array[] =
  {
    &omit_call_stack,
    &monitor_dot_net_gc,
    &monitor_dot_net_jit,
    &monitor_dot_net_objects,
    &monitor_dot_net_exceptions,
  };
  for (auto p : array){
    auto mask = BIT(idx++);
    *p = check_flag(flag, mask);
  }
}

void CoalescentIPC::InitializeMutex()
{
  if (mutex_initialized)
    return;
  global_mutex.Create();
  TransferMutex();
  //hi->SendCustomMessage(CUSTOM_MESSAGE_SEND_MUTEX, (my_size_t)TransferMutex(), VARIANT_TRUE);
  hi->SendCustomMessage(CUSTOM_MESSAGE_SEND_FIRST_BUFFER, (my_size_t)TransferSharedBuffer(), VARIANT_TRUE);
  mutex_initialized = 1;
}

#pragma pack(push, 1)
struct SharedBuffer
{
  byte_t valid;
  boost::uint64_t long_pid;
  boost::uint64_t optional_mutex;
  boost::uint64_t order;
  boost::uint32_t length;
  boost::uint32_t event_count;
#define SUM_OF_OTHER_MEMBERS (1 + 3 * 8 + 2 * 4)
  byte_t buffer[CoalescentIPC::MAX_BUFFER_SIZE - SUM_OF_OTHER_MEMBERS];
#undef SUM_OF_OTHER_MEMBERS
};
#pragma pack(pop)

void CoalescentIPC::TransferMutex()
{
  SharedBuffer *sb = (SharedBuffer *)buffer;
  uintptr_t handle = (uintptr_t)TransferHandle(global_mutex.GetMutexHandle());
#pragma warning(push)
#pragma warning(error: 4244)
  sb->optional_mutex = handle;
#pragma warning(pop)
}

void CoalescentIPC::AllocateSharedBuffer()
{
#ifdef DETAILED_LOG
  detailed_log <<"CoalescentIPC::AllocateSharedBuffer()\n";
#endif
#ifdef _M_X64
  const DWORD max_size_high = (configured_buffer_size >> 32) & full32mask;
#else
  const DWORD max_size_high = 0;
#endif
  const DWORD max_size_low = configured_buffer_size & full32mask;
  file_map = CreateFileMappingW(INVALID_HANDLE_VALUE, 0, PAGE_READWRITE, max_size_high, max_size_low, 0);
  if (!file_map)
  {
    last_error = ::GetLastError();
    error_string = "CreateFileMappingW() failed.";
    throw CIPC_SharedBufferAllocationException(last_error, error_string);
  }
  buffer = MapViewOfFile(file_map, FILE_MAP_ALL_ACCESS, 0, 0, configured_buffer_size);
  if (!buffer)
  {
    last_error = ::GetLastError();
    error_string = "MapViewOfFile() failed.";
    NktClose(file_map);
    file_map = 0;
    throw CIPC_SharedBufferAllocationException(last_error, error_string);
  }

  SharedBuffer *sb = (SharedBuffer *)buffer;
  initial_writing_position = FIELD_OFFSET(SharedBuffer, buffer);
  memset(sb->buffer, 0, configured_buffer_size - initial_writing_position);
  writing_position = initial_writing_position;
  sb->valid = 1;
  sb->long_pid = this->long_process_id;
  sb->optional_mutex = 0;
  sb->order = order_id++;
  sb->length = 0;
  sb->event_count = 0;
}

#ifdef min
#undef min
#endif

template <typename T, typename T2>
static void WriteBuffer_B(
                        const size_t &dst_size,
                        size_t &writing_position,
                        const void *src,
                        size_t src_size,
                        T *_this,
                        void (T::*Send)(T2),
                        void *(T::*GetBuffer)(),
                        T2 param = T2())
{
  void *dst = (_this->*GetBuffer)();
  const BYTE *in_buffer = (const BYTE *)src;
  BYTE *out_buffer = (BYTE *)dst;
  while (src_size)
  {
    size_t bytes_to_write = std::min(dst_size - writing_position, src_size);
    assert(writing_position < dst_size);
    assert(writing_position + bytes_to_write <= dst_size);
    memcpy(
      out_buffer + writing_position,
      in_buffer,
      bytes_to_write
    );
#ifdef _DEBUG
    last_byte = out_buffer[writing_position + bytes_to_write - 1];
#endif
    in_buffer += bytes_to_write;
    writing_position += bytes_to_write;
    src_size -= bytes_to_write;
    if (writing_position == dst_size)
    {
      (_this->*Send)(param);
      out_buffer = (BYTE *)(_this->*GetBuffer)();
    }
  }
  assert(writing_position < dst_size);
}

void CoalescentIPC::IncrementEventCount()
{
  ((SharedBuffer *)buffer)->event_count++;
}

void CoalescentIPC::WriteBuffer(const void *void_buffer, size_t size)
{
  SharedBuffer *sb = (SharedBuffer *)buffer;
  if (!sb->valid)
  {
    length_of_current_send = sb->length;
    FreeSharedBuffer();
    AllocateSharedBuffer();
    SendMessage(0);
  }
  ::WriteBuffer_B(configured_buffer_size, writing_position, void_buffer, size, this, &CoalescentIPC::Send, &CoalescentIPC::GetBuffer, false);
  sb = (SharedBuffer *)buffer;
  sb->length = boost::uint32_t(writing_position - initial_writing_position);
}

#if defined SEND_OUTPUT_TO_FILE
int SEND_OUTPUT_TO_FILE_times = 0;
#endif

void CoalescentIPC::FreeSharedBuffer()
{
#ifdef DETAILED_LOG
  detailed_log <<"CoalescentIPC::FreeSharedBuffer()\n";
#endif
#ifdef SEND_OUTPUT_TO_FILE
  {
    char filename[100];
    sprintf_s(filename, "outgoing_buffer%04d.txt", SEND_OUTPUT_TO_FILE_times);
    SEND_OUTPUT_TO_FILE_times++;
    std::ofstream file(filename, std::ios::binary|std::ios::trunc);
    file.write((const char *)buffer, configured_buffer_size);
  }
#endif
  if (buffer)
    UnmapViewOfFile(buffer);
  if (file_map)
    NktClose(file_map);
}

HANDLE CoalescentIPC::TransferHandle(HANDLE h)
{
  HANDLE ret = NktDuplicateHandle(own_process, target_process(hi), h);
  bool failed = !ret;
  if (!ret)
  {
    error_string = "DuplicateHandle() failed.";
    throw CIPC_IPCException(-1, error_string);
  }
  return ret;
}

void CoalescentIPC::SendMessage(bool sync)
{
#ifdef REPORT_STATISTICS
  {
    std::ofstream file("statistics.txt", std::ios::app);
    file <<
      "SendMessage() entered!\n"
      "Buffers sent: "<<buffers_transferred<<"\n"
      "Events sent: "<<events_transterred<<std::endl;
  }
#endif
#ifdef DETAILED_LOG
  detailed_log <<"CoalescentIPC::SendMessage(): Sending buffer "
               <<(sync?"":"a")<<"synchronously.\n";
#endif
  double ratio = (double)length_of_current_send / configured_buffer_size ;
#ifndef TURN_IPC_AND_SYNC_OFF
  hi->SendCustomMessage(CUSTOM_MESSAGE_SEND_BUFFER, (my_size_t)TransferSharedBuffer(), sync ? VARIANT_TRUE : VARIANT_FALSE);
#endif
  buffers_transferred += ratio;
}

void CoalescentIPC::Send(bool synchronously)
{
  if (!ok)
    return;
  {
    SharedBuffer *sb = (SharedBuffer *)buffer;
    length_of_current_send = sb->length = boost::uint32_t(writing_position - initial_writing_position);
  }
  FreeSharedBuffer();
  try
  {
    AllocateSharedBuffer();
  }
  catch (CIPC_SharedBufferAllocationException &e)
  {
    ok = 0;
    throw e;
  }
  SendMessage(synchronously);
}

void CoalescentIPC::AddHookPropertyAndActivateHook(HookId_t hook_id, const HookProperties &prop)
{
  properties_t::iterator i = properties.find(hook_id);
  assert(i == properties.end());
  properties[hook_id] = prop;
  if (prop.is_secondary_hook)
    no_secondary_hooks = 0;
  InterlockedIncrement(&active_hooks);
}

bool CoalescentIPC::HasSecondaryHook(HookId_t primary_hook_id)
{
  for (properties_t::iterator i = properties.begin(), e = properties.end(); i != e; ++i)
  {
    HookProperties &hp = i->second;
    if (hp.is_secondary_hook && hp.primary_hook_id == primary_hook_id)
      return 1;
  }
  return 0;
}

bool CoalescentIPC::IsSystemModule(const wchar_t *name) const
{
  return system_modules.find(name) != system_modules.end();
}

unsigned CoalescentIPC::IncrementCoCreateInstanceCounter(long thread_id)
{
  dictionary_t<long, unsigned>::iterator it = CoCreateInstance_counters.find(thread_id);
  if (it == CoCreateInstance_counters.end())
    return CoCreateInstance_counters[thread_id] = 1;
  return ++it->second;
}

unsigned CoalescentIPC::DecrementCoCreateInstanceCounter(long thread_id)
{
  dictionary_t<long, unsigned>::iterator it = CoCreateInstance_counters.find(thread_id);
  if (it == CoCreateInstance_counters.end())
  {
    assert(0);
    return CoCreateInstance_counters[thread_id] = 0;
  }
  assert(it->second);
  return --it->second;
}

unsigned CoalescentIPC::GetCoCreateInstanceCounter(long thread_id)
{
  dictionary_t<long, unsigned>::iterator it = CoCreateInstance_counters.find(thread_id);
  if (it == CoCreateInstance_counters.end())
    return CoCreateInstance_counters[thread_id] = 0;
  return it->second;
}

void CoalescentIPC::SendThinAppCreateProcessMessage(unsigned long pid)
{
  hi->SendCustomMessage(CUSTOM_MESSAGE_THINAPP_CREATE_PROCESS, pid, VARIANT_TRUE);
}

DWORD WINAPI CoalescentIPC::thread(LPVOID param)
{
#ifndef DISABLE_THREAD
  CoalescentIPC *_this = (CoalescentIPC *)param;
  while (1)
  {
    if (_this->synchronization_list->IsEmpty())
    {
      if (_this->kill_thread)
        break;
      SleepForABit();
      continue;
    }
    SynchronizationNode *head = _this->ObtainNodes();
    _this->WriteNodes(head);
  }
#endif
  return 0;
}

void *CoalescentIPC::RequestExecutableMemory(size_t size)
{
  return (void *)hi->SendCustomMessage(CUSTOM_MESSAGE_REQUEST_EXEC_MEMORY, size, VARIANT_TRUE);
}

bool is_alive(HANDLE thread)
{
  DWORD res = WaitForSingleObject(thread, 0);
  return res != WAIT_OBJECT_0;
}

CoalescentIPC::SynchronizationList::SynchronizationList()
{
  head = 0;
  tail = 0;
}

CoalescentIPC::SynchronizationList::~SynchronizationList()
{
  for (iterator i = begin(), e = end(); i != e;)
    delete *(i++);
}

SynchronizationNode *CoalescentIPC::SynchronizationList::PushBack(CoalescentIPC &cipc)
{
  SynchronizationNode *new_node = new (std::nothrow) SynchronizationNode(cipc.buffer_list, cipc);
  if (!new_node)
    THROW_CIPC_OUTOFMEMORY;
  {
    LOCK_MUTEX(mutex);
    if (!head)
      return tail = head = new_node;
    tail->next = new_node;
    tail = new_node;
  }
  return new_node;
}

SynchronizationNode *CoalescentIPC::SynchronizationList::PeekFront()
{
  SynchronizationNode *ret;
  while (1)
  {
    {
      LOCK_MUTEX(mutex);
      ret = head;
      if (!!ret)
        break;
    }
#ifndef DISABLE_THREAD
    SleepForABit();
#endif
  }
  return ret;
}

SynchronizationNode *CoalescentIPC::SynchronizationList::PopFront()
{
  SynchronizationNode *ret;
  while (1)
  {
    {
      LOCK_MUTEX(mutex);
      ret = head;
      if (!!ret)
      {
        if (head == tail)
        {
          tail = head = 0;
          return ret;
        }
        head = head->next;
        break;
      }
    }
#ifndef DISABLE_THREAD
    SleepForABit();
#endif
  }
  return ret;
}

SynchronizationNode *CoalescentIPC::SynchronizationList::PopFrontUnlocked()
{
  SynchronizationNode *ret;
  while (1)
  {
    ret = head;
    if (!!ret)
    {
      if (head == tail)
      {
        tail = head = 0;
        return ret;
      }
      head = head->next;
      break;
    }
#ifndef DISABLE_THREAD
    SleepForABit();
#endif
  }
  return ret;
}

SynchronizationNode *CoalescentIPC::ObtainNodes()
{
  SynchronizationNode *ret = 0,
    *tail = 0;
  //Jumpy code ahead. Take caution.
  while (1)
  {
    if (synchronization_list->IsEmpty() && !!ret)
      //If there are no more pending buffers and we have work to do
      //then don't bother waiting. Just write what we have, for now.
      break;
    SynchronizationNode *node = synchronization_list->PeekFront();
    {
      vassert((mword_t)node != 0xDDDDDDDD);
      LOCK_MUTEX(synchronization_list->GetMutex());
      if (!node->ready)
      {
        if (!!ret)
          //The next buffer is not yet ready, but we want to write what we have
          //as soon as possible, so leave for now.
          break;
        if (!is_alive(node->creating_thread))
          //The thread died, so eliminate its work load.
          //(src1)
          goto remove_front;
        //At this point, the thread is alive and not ready, but we have nothing
        //to do, so we keep waiting.
        //(src2)
      }
      else
      {
        //A buffer is ready...
        node = synchronization_list->PopFront();
        //...so add it to the list...
        if (!ret)
          ret = tail = node;
        else
        {
          tail->next = node;
          tail = tail->next;
        }
        tail->next = 0;
        //...and look for more work (without waiting).
        continue;
      }
    }
    //Note: A well-intentioned yet misguided soul may think that they can
    //      simplify the code by moving the next few lines up into the true
    //      block for !node->ready, but they'd be wrong. Note how at this point
    //      we're outside of the block that locks synchronization_list.
    
    //If we reach this point, it must be from src2. The head is not ready
    //and our work list is empty.
    assert(!ret);
#ifndef DISABLE_THREAD
    SleepForABit();
#endif
    continue;
remove_front:
    //From src1.
    synchronization_list->FreeFront();
  }
  return ret;
}

void CoalescentIPC::WriteNodes(SynchronizationNode *head)
{
#ifndef TURN_IPC_AND_SYNC_OFF
  CNktAutoFastMutex m(&mutex);
  InitializeMutex();
  CNktAutoMutex am(&global_mutex);
  if (!am.IsLockHeld())
    throw CIPC_SynchonizationException(ERROR_TIMEOUT, "CoalescentIPC::WriteNodes(): Failed to lock mutex.");
#endif
  while (head)
  {
    SynchronizationNode *next = head->next;
    vassert((mword_t)head->creating_thread != 0xDDDDDDDD);
    delete head;
    head = next;
  }
}

SynchronizationNode::SynchronizationNode(BufferList &bl, CoalescentIPC &cipc):
  next(0),
  buffer(bl, cipc),
  ready(0)
{
  DWORD error;
  if (!GetGlobalCurrentThreadHandle(creating_thread, error))
    throw CIPC_Exception(error, "An error occurred while getting handle for the current thread.");
}

SerializerNodeInterface::SerializerNodeInterface(CoalescentIPC &cipc)
{
  node = cipc.synchronization_list->PushBack(cipc);
  this->cipc = &cipc;
}

SerializerNodeInterface::~SerializerNodeInterface()
{
  node->SetReady();
#ifdef DISABLE_THREAD
  LOCK_MUTEX(cipc->synchronization_list->mutex);
  if (cipc->synchronization_list->IsEmpty())
    return;
  SynchronizationNode *head = cipc->ObtainNodes();
  cipc->WriteNodes(head);
#endif
}
