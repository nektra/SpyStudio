// This section is automatically generated!


#include "stdafx.h"
#include "GeneratedDotNetCallbacks.h"
#include "DotNetProfiler.h"
#include "DotNetProfilerMgr.h"
#include "CIPC.h"
#include "Buffer.h"



HRESULT CDotNetProfiler::OnICorProfilerCallback_Shutdown(ICorProfilerCallback3 *_this)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AppDomainCreationStarted(ICorProfilerCallback3 *_this, AppDomainID appDomainId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AppDomainCreationFinished(ICorProfilerCallback3 *_this, AppDomainID appDomainId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::AppDomainCreationFinished, 0.0, "AppDomainCreation", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_AppDomainID(buffer, "appDomainId", appDomainId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AppDomainShutdownStarted(ICorProfilerCallback3 *_this, AppDomainID appDomainId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AppDomainShutdownFinished(ICorProfilerCallback3 *_this, AppDomainID appDomainId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::AppDomainShutdownFinished, 0.0, "AppDomainShutdown", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_AppDomainID(buffer, "appDomainId", appDomainId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AssemblyLoadStarted(ICorProfilerCallback3 *_this, AssemblyID assemblyId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AssemblyLoadFinished(ICorProfilerCallback3 *_this, AssemblyID assemblyId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::AssemblyLoadFinished, 0.0, "AssemblyLoad", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_AssemblyID(buffer, "assemblyId", assemblyId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AssemblyUnloadStarted(ICorProfilerCallback3 *_this, AssemblyID assemblyId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_AssemblyUnloadFinished(ICorProfilerCallback3 *_this, AssemblyID assemblyId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::AssemblyUnloadFinished, 0.0, "AssemblyUnload", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_AssemblyID(buffer, "assemblyId", assemblyId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ModuleLoadStarted(ICorProfilerCallback3 *_this, ModuleID moduleId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ModuleLoadFinished(ICorProfilerCallback3 *_this, ModuleID moduleId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ModuleLoadFinished, 0.0, "ModuleLoad", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_ModuleID(buffer, "moduleId", moduleId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ModuleUnloadStarted(ICorProfilerCallback3 *_this, ModuleID moduleId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ModuleUnloadFinished(ICorProfilerCallback3 *_this, ModuleID moduleId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ModuleUnloadFinished, 0.0, "ModuleUnload", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_ModuleID(buffer, "moduleId", moduleId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ModuleAttachedToAssembly(ICorProfilerCallback3 *_this, ModuleID moduleId, AssemblyID AssemblyId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ModuleAttachedToAssembly, 0.0, "ModuleAttachedToAssembly", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ModuleID(buffer, "moduleId", moduleId);
    mgr.add_param_AssemblyID(buffer, "AssemblyId", AssemblyId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ClassLoadStarted(ICorProfilerCallback3 *_this, ClassID classId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ClassLoadFinished(ICorProfilerCallback3 *_this, ClassID classId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ClassLoadFinished, 0.0, "ClassLoad", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_ClassID(buffer, "classId", classId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ClassUnloadStarted(ICorProfilerCallback3 *_this, ClassID classId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ClassUnloadFinished(ICorProfilerCallback3 *_this, ClassID classId, HRESULT hrStatus)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ClassUnloadFinished, 0.0, "ClassUnload", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_ClassID(buffer, "classId", classId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_FunctionUnloadStarted(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::FunctionUnloadStarted, 0.0, "FunctionUnloadStarted", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_JITCompilationStarted(ICorProfilerCallback3 *_this, FunctionID functionId, BOOL fIsSafeToBlock)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_JITCompilationFinished(ICorProfilerCallback3 *_this, FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::JITCompilationFinished, 0.0, "JITCompilation", 4, 1);
    mgr.add_result(buffer, hrStatus);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    mgr.add_param_BOOL(buffer, "fIsSafeToBlock", fIsSafeToBlock);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_JITCachedFunctionSearchStarted(ICorProfilerCallback3 *_this, FunctionID functionId, BOOL * pbUseCachedFunction)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_JITCachedFunctionSearchFinished(ICorProfilerCallback3 *_this, FunctionID functionId, COR_PRF_JIT_CACHE result)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::JITCachedFunctionSearchFinished, 0.0, "JITCachedFunctionSearch", 4, 1);
    mgr.add_result(buffer, result);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_JITFunctionPitched(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::JITFunctionPitched, 0.0, "JITFunctionPitched", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_JITInlining(ICorProfilerCallback3 *_this, FunctionID callerId, FunctionID calleeId, BOOL * pfShouldInline)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::JITInlining, 0.0, "JITInlining", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "callerId", callerId);
    mgr.add_param_FunctionID(buffer, "calleeId", calleeId);
    buffer.AddEndOfMessage();
    JITInlining_INSERT;
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ThreadCreated(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ThreadCreated, 0.0, "ThreadCreated", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ThreadID(buffer, "threadId", threadId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ThreadDestroyed(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ThreadDestroyed, 0.0, "ThreadDestroyed", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ThreadID(buffer, "threadId", threadId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ThreadAssignedToOSThread(ICorProfilerCallback3 *_this, ThreadID managedThreadId, DWORD osThreadId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ThreadAssignedToOSThread, 0.0, "ThreadAssignedToOSThread", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ThreadID(buffer, "managedThreadId", managedThreadId);
    mgr.add_param_DWORD(buffer, "osThreadId", osThreadId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingClientInvocationStarted(ICorProfilerCallback3 *_this)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingClientSendingMessage(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingClientSendingMessage, 0.0, "RemotingClientSendingMessage", 4, 1);
    buffer.AddString("params");
    mgr.add_param_LPGUID(buffer, "pCookie", pCookie);
    mgr.add_param_BOOL(buffer, "fIsAsync", fIsAsync);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingClientReceivingReply(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingClientReceivingReply, 0.0, "RemotingClientReceivingReply", 4, 1);
    buffer.AddString("params");
    mgr.add_param_LPGUID(buffer, "pCookie", pCookie);
    mgr.add_param_BOOL(buffer, "fIsAsync", fIsAsync);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingClientInvocationFinished(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingClientInvocationFinished, 0.0, "RemotingClientInvocation", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingServerReceivingMessage(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingServerReceivingMessage, 0.0, "RemotingServerReceivingMessage", 4, 1);
    buffer.AddString("params");
    mgr.add_param_LPGUID(buffer, "pCookie", pCookie);
    mgr.add_param_BOOL(buffer, "fIsAsync", fIsAsync);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingServerInvocationStarted(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingServerInvocationStarted, 0.0, "RemotingServerInvocationStarted", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingServerInvocationReturned(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingServerInvocationReturned, 0.0, "RemotingServerInvocationReturned", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RemotingServerSendingReply(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RemotingServerSendingReply, 0.0, "RemotingServerSendingReply", 4, 1);
    buffer.AddString("params");
    mgr.add_param_LPGUID(buffer, "pCookie", pCookie);
    mgr.add_param_BOOL(buffer, "fIsAsync", fIsAsync);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_UnmanagedToManagedTransition(ICorProfilerCallback3 *_this, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::UnmanagedToManagedTransition, 0.0, "UnmanagedToManagedTransition", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    mgr.add_param_COR_PRF_TRANSITION_REASON(buffer, "reason", reason);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ManagedToUnmanagedTransition(ICorProfilerCallback3 *_this, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ManagedToUnmanagedTransition, 0.0, "ManagedToUnmanagedTransition", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    mgr.add_param_COR_PRF_TRANSITION_REASON(buffer, "reason", reason);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeSuspendStarted(ICorProfilerCallback3 *_this, COR_PRF_SUSPEND_REASON suspendReason)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeSuspendFinished(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RuntimeSuspendFinished, 0.0, "RuntimeSuspend", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeSuspendAborted(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RuntimeSuspendAborted, 0.0, "RuntimeSuspendAborted", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeResumeStarted(ICorProfilerCallback3 *_this)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeResumeFinished(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RuntimeResumeFinished, 0.0, "RuntimeResume", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeThreadSuspended(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RuntimeThreadSuspended, 0.0, "RuntimeThreadSuspended", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ThreadID(buffer, "threadId", threadId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RuntimeThreadResumed(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::RuntimeThreadResumed, 0.0, "RuntimeThreadResumed", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ThreadID(buffer, "threadId", threadId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_MovedReferences(ICorProfilerCallback3 *_this, ULONG cMovedObjectIDRanges, ObjectID * oldObjectIDRangeStart, ObjectID * newObjectIDRangeStart, ULONG * cObjectIDRangeLength)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::MovedReferences, 0.0, "MovedReferences", 4, 1);
    mgr.add_param_movedObjects(buffer, "movedObjects", cMovedObjectIDRanges, oldObjectIDRangeStart, newObjectIDRangeStart, cObjectIDRangeLength);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ObjectAllocated(ICorProfilerCallback3 *_this, ObjectID objectId, ClassID classId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ObjectAllocated, 0.0, "ObjectAllocated", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ObjectID(buffer, "objectId", objectId);
    mgr.add_param_ClassID(buffer, "classId", classId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ObjectsAllocatedByClass(ICorProfilerCallback3 *_this, ULONG cClassCount, ClassID * classIds, ULONG * cObjects)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ObjectsAllocatedByClass, 0.0, "ObjectsAllocatedByClass", 4, 1);
    mgr.add_param_objectCounts(buffer, "objectCounts", cClassCount, classIds, cObjects);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ObjectReferences(ICorProfilerCallback3 *_this, ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID * objectRefIds)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RootReferences(ICorProfilerCallback3 *_this, ULONG cRootRefs, ObjectID * rootRefIds)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionThrown(ICorProfilerCallback3 *_this, ObjectID thrownObjectId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionThrown, 0.0, "ExceptionThrown", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ObjectID(buffer, "thrownObjectId", thrownObjectId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionSearchFunctionEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionSearchFunctionEnter, 0.0, "ExceptionSearchFunctionEnter", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionSearchFunctionLeave(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionSearchFunctionLeave, 0.0, "ExceptionSearchFunctionLeave", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionSearchFilterEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionSearchFilterEnter, 0.0, "ExceptionSearchFilterEnter", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionSearchFilterLeave(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionSearchFilterLeave, 0.0, "ExceptionSearchFilterLeave", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionSearchCatcherFound(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionSearchCatcherFound, 0.0, "ExceptionSearchCatcherFound", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionOSHandlerEnter(ICorProfilerCallback3 *_this, UINT_PTR __unused)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionOSHandlerEnter, 0.0, "ExceptionOSHandlerEnter", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionOSHandlerLeave(ICorProfilerCallback3 *_this, UINT_PTR __unused)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionOSHandlerLeave, 0.0, "ExceptionOSHandlerLeave", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionUnwindFunctionEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionUnwindFunctionEnter, 0.0, "ExceptionUnwindFunctionEnter", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionUnwindFunctionLeave(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionUnwindFunctionLeave, 0.0, "ExceptionUnwindFunctionLeave", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionUnwindFinallyEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionUnwindFinallyEnter, 0.0, "ExceptionUnwindFinallyEnter", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionUnwindFinallyLeave(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionUnwindFinallyLeave, 0.0, "ExceptionUnwindFinallyLeave", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionCatcherEnter(ICorProfilerCallback3 *_this, FunctionID functionId, ObjectID objectId)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionCatcherEnter, 0.0, "ExceptionCatcherEnter", 4, 1);
    buffer.AddString("params");
    mgr.add_param_FunctionID(buffer, "functionId", functionId);
    mgr.add_param_ObjectID(buffer, "objectId", objectId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionCatcherLeave(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionCatcherLeave, 0.0, "ExceptionCatcherLeave", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_COMClassicVTableCreated(ICorProfilerCallback3 *_this, ClassID wrappedClassId, REFGUID implementedIID, void * pVTable, ULONG cSlots)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::COMClassicVTableCreated, 0.0, "COMClassicVTableCreated", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ClassID(buffer, "wrappedClassId", wrappedClassId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_COMClassicVTableDestroyed(ICorProfilerCallback3 *_this, ClassID wrappedClassId, REFGUID implementedIID, void * pVTable)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::COMClassicVTableDestroyed, 0.0, "COMClassicVTableDestroyed", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ClassID(buffer, "wrappedClassId", wrappedClassId);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionCLRCatcherFound(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionCLRCatcherFound, 0.0, "ExceptionCLRCatcherFound", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ExceptionCLRCatcherExecute(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ExceptionCLRCatcherExecute, 0.0, "ExceptionCLRCatcherExecute", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ThreadNameChanged(ICorProfilerCallback3 *_this, ThreadID threadId, ULONG cchName, PWCHAR name)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ThreadNameChanged, 0.0, "ThreadNameChanged", 4, 1);
    buffer.AddString("params");
    mgr.add_param_ThreadID(buffer, "threadId", threadId);
    mgr.add_param_ULONG(buffer, "cchName", cchName);
    mgr.add_param_PWCHAR(buffer, "name", name);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_GarbageCollectionStarted(ICorProfilerCallback3 *_this, int cGenerations, BOOL * generationCollected, COR_PRF_GC_REASON reason)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_SurvivingReferences(ICorProfilerCallback3 *_this, ULONG cSurvivingObjectIDRanges, ObjectID * objectIDRangeStart, ULONG * cObjectIDRangeLength)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::SurvivingReferences, 0.0, "SurvivingReferences", 4, 1);
    mgr.add_param_survivingObjects(buffer, "survivingObjects", cSurvivingObjectIDRanges, objectIDRangeStart, cObjectIDRangeLength);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_GarbageCollectionFinished(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::GarbageCollectionFinished, 0.0, "GarbageCollection", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_FinalizeableObjectQueued(ICorProfilerCallback3 *_this, DWORD finalizerFlags, ObjectID objectID)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::FinalizeableObjectQueued, 0.0, "FinalizeableObjectQueued", 4, 1);
    buffer.AddString("params");
    mgr.add_param_DWORD(buffer, "finalizerFlags", finalizerFlags);
    mgr.add_param_ObjectID(buffer, "objectID", objectID);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_RootReferences2(ICorProfilerCallback3 *_this, ULONG cRootRefs, ObjectID * rootRefIds, COR_PRF_GC_ROOT_KIND * rootKinds, COR_PRF_GC_ROOT_FLAGS * rootFlags, UINT_PTR * rootIds)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_HandleCreated(ICorProfilerCallback3 *_this, GCHandleID handleId, ObjectID initialObjectId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_HandleDestroyed(ICorProfilerCallback3 *_this, GCHandleID handleId)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_InitializeForAttach(ICorProfilerCallback3 *_this, IUnknown * pCorProfilerInfoUnk, void * pvClientData, UINT cbClientData)
{
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ProfilerAttachComplete(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ProfilerAttachComplete, 0.0, "ProfilerAttachComplete", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_ProfilerDetachSucceeded(ICorProfilerCallback3 *_this)
{
  {
    auto &mgr = global_cipc->dot_net_profiler_mgr;
    //auto cookie = mgr.get_cookie(0, (LinkerId)0, generate_cookie_hash());
    SerializerNodeInterface sni(global_cipc);
    auto &buffer = sni.GetBuffer();
    mgr.standard_prelude(buffer, (unsigned)FunctionId::ProfilerDetachSucceeded, 0.0, "ProfilerDetachSucceeded", 4, 1);
    buffer.AddEndOfMessage();
    
  }
  return S_OK;
}

// End of automatically generated section.
