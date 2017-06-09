// This section is automatically generated!

static HRESULT __stdcall OnICorProfilerCallback_QueryInterface(ICorProfilerCallback3 *_this, REFIID riid, LPVOID * ppvObject)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_QueryInterface(_this, riid, ppvObject);
}

static HRESULT __stdcall OnICorProfilerCallback_Initialize(ICorProfilerCallback3 *_this, IUnknown * pICorProfilerInfoUnk)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_Initialize(_this, pICorProfilerInfoUnk);
}

static HRESULT __stdcall OnICorProfilerCallback_Shutdown(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_Shutdown(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_AppDomainCreationStarted(ICorProfilerCallback3 *_this, AppDomainID appDomainId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AppDomainCreationStarted(_this, appDomainId);
}

static HRESULT __stdcall OnICorProfilerCallback_AppDomainCreationFinished(ICorProfilerCallback3 *_this, AppDomainID appDomainId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AppDomainCreationFinished(_this, appDomainId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_AppDomainShutdownStarted(ICorProfilerCallback3 *_this, AppDomainID appDomainId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AppDomainShutdownStarted(_this, appDomainId);
}

static HRESULT __stdcall OnICorProfilerCallback_AppDomainShutdownFinished(ICorProfilerCallback3 *_this, AppDomainID appDomainId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AppDomainShutdownFinished(_this, appDomainId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_AssemblyLoadStarted(ICorProfilerCallback3 *_this, AssemblyID assemblyId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AssemblyLoadStarted(_this, assemblyId);
}

static HRESULT __stdcall OnICorProfilerCallback_AssemblyLoadFinished(ICorProfilerCallback3 *_this, AssemblyID assemblyId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AssemblyLoadFinished(_this, assemblyId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_AssemblyUnloadStarted(ICorProfilerCallback3 *_this, AssemblyID assemblyId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AssemblyUnloadStarted(_this, assemblyId);
}

static HRESULT __stdcall OnICorProfilerCallback_AssemblyUnloadFinished(ICorProfilerCallback3 *_this, AssemblyID assemblyId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_AssemblyUnloadFinished(_this, assemblyId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_ModuleLoadStarted(ICorProfilerCallback3 *_this, ModuleID moduleId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ModuleLoadStarted(_this, moduleId);
}

static HRESULT __stdcall OnICorProfilerCallback_ModuleLoadFinished(ICorProfilerCallback3 *_this, ModuleID moduleId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ModuleLoadFinished(_this, moduleId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_ModuleUnloadStarted(ICorProfilerCallback3 *_this, ModuleID moduleId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ModuleUnloadStarted(_this, moduleId);
}

static HRESULT __stdcall OnICorProfilerCallback_ModuleUnloadFinished(ICorProfilerCallback3 *_this, ModuleID moduleId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ModuleUnloadFinished(_this, moduleId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_ModuleAttachedToAssembly(ICorProfilerCallback3 *_this, ModuleID moduleId, AssemblyID AssemblyId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ModuleAttachedToAssembly(_this, moduleId, AssemblyId);
}

static HRESULT __stdcall OnICorProfilerCallback_ClassLoadStarted(ICorProfilerCallback3 *_this, ClassID classId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ClassLoadStarted(_this, classId);
}

static HRESULT __stdcall OnICorProfilerCallback_ClassLoadFinished(ICorProfilerCallback3 *_this, ClassID classId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ClassLoadFinished(_this, classId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_ClassUnloadStarted(ICorProfilerCallback3 *_this, ClassID classId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ClassUnloadStarted(_this, classId);
}

static HRESULT __stdcall OnICorProfilerCallback_ClassUnloadFinished(ICorProfilerCallback3 *_this, ClassID classId, HRESULT hrStatus)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ClassUnloadFinished(_this, classId, hrStatus);
}

static HRESULT __stdcall OnICorProfilerCallback_FunctionUnloadStarted(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_FunctionUnloadStarted(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_JITCompilationStarted(ICorProfilerCallback3 *_this, FunctionID functionId, BOOL fIsSafeToBlock)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_JITCompilationStarted(_this, functionId, fIsSafeToBlock);
}

static HRESULT __stdcall OnICorProfilerCallback_JITCompilationFinished(ICorProfilerCallback3 *_this, FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_JITCompilationFinished(_this, functionId, hrStatus, fIsSafeToBlock);
}

static HRESULT __stdcall OnICorProfilerCallback_JITCachedFunctionSearchStarted(ICorProfilerCallback3 *_this, FunctionID functionId, BOOL * pbUseCachedFunction)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_JITCachedFunctionSearchStarted(_this, functionId, pbUseCachedFunction);
}

static HRESULT __stdcall OnICorProfilerCallback_JITCachedFunctionSearchFinished(ICorProfilerCallback3 *_this, FunctionID functionId, COR_PRF_JIT_CACHE result)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_JITCachedFunctionSearchFinished(_this, functionId, result);
}

static HRESULT __stdcall OnICorProfilerCallback_JITFunctionPitched(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_JITFunctionPitched(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_JITInlining(ICorProfilerCallback3 *_this, FunctionID callerId, FunctionID calleeId, BOOL * pfShouldInline)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_JITInlining(_this, callerId, calleeId, pfShouldInline);
}

static HRESULT __stdcall OnICorProfilerCallback_ThreadCreated(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ThreadCreated(_this, threadId);
}

static HRESULT __stdcall OnICorProfilerCallback_ThreadDestroyed(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ThreadDestroyed(_this, threadId);
}

static HRESULT __stdcall OnICorProfilerCallback_ThreadAssignedToOSThread(ICorProfilerCallback3 *_this, ThreadID managedThreadId, DWORD osThreadId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ThreadAssignedToOSThread(_this, managedThreadId, osThreadId);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingClientInvocationStarted(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingClientInvocationStarted(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingClientSendingMessage(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingClientSendingMessage(_this, pCookie, fIsAsync);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingClientReceivingReply(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingClientReceivingReply(_this, pCookie, fIsAsync);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingClientInvocationFinished(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingClientInvocationFinished(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingServerReceivingMessage(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingServerReceivingMessage(_this, pCookie, fIsAsync);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingServerInvocationStarted(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingServerInvocationStarted(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingServerInvocationReturned(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingServerInvocationReturned(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RemotingServerSendingReply(ICorProfilerCallback3 *_this, LPGUID pCookie, BOOL fIsAsync)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RemotingServerSendingReply(_this, pCookie, fIsAsync);
}

static HRESULT __stdcall OnICorProfilerCallback_UnmanagedToManagedTransition(ICorProfilerCallback3 *_this, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_UnmanagedToManagedTransition(_this, functionId, reason);
}

static HRESULT __stdcall OnICorProfilerCallback_ManagedToUnmanagedTransition(ICorProfilerCallback3 *_this, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ManagedToUnmanagedTransition(_this, functionId, reason);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeSuspendStarted(ICorProfilerCallback3 *_this, COR_PRF_SUSPEND_REASON suspendReason)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeSuspendStarted(_this, suspendReason);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeSuspendFinished(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeSuspendFinished(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeSuspendAborted(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeSuspendAborted(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeResumeStarted(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeResumeStarted(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeResumeFinished(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeResumeFinished(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeThreadSuspended(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeThreadSuspended(_this, threadId);
}

static HRESULT __stdcall OnICorProfilerCallback_RuntimeThreadResumed(ICorProfilerCallback3 *_this, ThreadID threadId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RuntimeThreadResumed(_this, threadId);
}

static HRESULT __stdcall OnICorProfilerCallback_MovedReferences(ICorProfilerCallback3 *_this, ULONG cMovedObjectIDRanges, ObjectID * oldObjectIDRangeStart, ObjectID * newObjectIDRangeStart, ULONG * cObjectIDRangeLength)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_MovedReferences(_this, cMovedObjectIDRanges, oldObjectIDRangeStart, newObjectIDRangeStart, cObjectIDRangeLength);
}

static HRESULT __stdcall OnICorProfilerCallback_ObjectAllocated(ICorProfilerCallback3 *_this, ObjectID objectId, ClassID classId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ObjectAllocated(_this, objectId, classId);
}

static HRESULT __stdcall OnICorProfilerCallback_ObjectsAllocatedByClass(ICorProfilerCallback3 *_this, ULONG cClassCount, ClassID * classIds, ULONG * cObjects)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ObjectsAllocatedByClass(_this, cClassCount, classIds, cObjects);
}

static HRESULT __stdcall OnICorProfilerCallback_ObjectReferences(ICorProfilerCallback3 *_this, ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID * objectRefIds)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ObjectReferences(_this, objectId, classId, cObjectRefs, objectRefIds);
}

static HRESULT __stdcall OnICorProfilerCallback_RootReferences(ICorProfilerCallback3 *_this, ULONG cRootRefs, ObjectID * rootRefIds)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RootReferences(_this, cRootRefs, rootRefIds);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionThrown(ICorProfilerCallback3 *_this, ObjectID thrownObjectId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionThrown(_this, thrownObjectId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionSearchFunctionEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionSearchFunctionEnter(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionSearchFunctionLeave(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionSearchFunctionLeave(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionSearchFilterEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionSearchFilterEnter(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionSearchFilterLeave(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionSearchFilterLeave(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionSearchCatcherFound(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionSearchCatcherFound(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionOSHandlerEnter(ICorProfilerCallback3 *_this, UINT_PTR __unused)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionOSHandlerEnter(_this, __unused);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionOSHandlerLeave(ICorProfilerCallback3 *_this, UINT_PTR __unused)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionOSHandlerLeave(_this, __unused);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionUnwindFunctionEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionUnwindFunctionEnter(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionUnwindFunctionLeave(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionUnwindFunctionLeave(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionUnwindFinallyEnter(ICorProfilerCallback3 *_this, FunctionID functionId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionUnwindFinallyEnter(_this, functionId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionUnwindFinallyLeave(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionUnwindFinallyLeave(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionCatcherEnter(ICorProfilerCallback3 *_this, FunctionID functionId, ObjectID objectId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionCatcherEnter(_this, functionId, objectId);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionCatcherLeave(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionCatcherLeave(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_COMClassicVTableCreated(ICorProfilerCallback3 *_this, ClassID wrappedClassId, REFGUID implementedIID, void * pVTable, ULONG cSlots)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_COMClassicVTableCreated(_this, wrappedClassId, implementedIID, pVTable, cSlots);
}

static HRESULT __stdcall OnICorProfilerCallback_COMClassicVTableDestroyed(ICorProfilerCallback3 *_this, ClassID wrappedClassId, REFGUID implementedIID, void * pVTable)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_COMClassicVTableDestroyed(_this, wrappedClassId, implementedIID, pVTable);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionCLRCatcherFound(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionCLRCatcherFound(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ExceptionCLRCatcherExecute(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ExceptionCLRCatcherExecute(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ThreadNameChanged(ICorProfilerCallback3 *_this, ThreadID threadId, ULONG cchName, PWCHAR name)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ThreadNameChanged(_this, threadId, cchName, name);
}

static HRESULT __stdcall OnICorProfilerCallback_GarbageCollectionStarted(ICorProfilerCallback3 *_this, int cGenerations, BOOL * generationCollected, COR_PRF_GC_REASON reason)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_GarbageCollectionStarted(_this, cGenerations, generationCollected, reason);
}

static HRESULT __stdcall OnICorProfilerCallback_SurvivingReferences(ICorProfilerCallback3 *_this, ULONG cSurvivingObjectIDRanges, ObjectID * objectIDRangeStart, ULONG * cObjectIDRangeLength)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_SurvivingReferences(_this, cSurvivingObjectIDRanges, objectIDRangeStart, cObjectIDRangeLength);
}

static HRESULT __stdcall OnICorProfilerCallback_GarbageCollectionFinished(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_GarbageCollectionFinished(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_FinalizeableObjectQueued(ICorProfilerCallback3 *_this, DWORD finalizerFlags, ObjectID objectID)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_FinalizeableObjectQueued(_this, finalizerFlags, objectID);
}

static HRESULT __stdcall OnICorProfilerCallback_RootReferences2(ICorProfilerCallback3 *_this, ULONG cRootRefs, ObjectID * rootRefIds, COR_PRF_GC_ROOT_KIND * rootKinds, COR_PRF_GC_ROOT_FLAGS * rootFlags, UINT_PTR * rootIds)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_RootReferences2(_this, cRootRefs, rootRefIds, rootKinds, rootFlags, rootIds);
}

static HRESULT __stdcall OnICorProfilerCallback_HandleCreated(ICorProfilerCallback3 *_this, GCHandleID handleId, ObjectID initialObjectId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_HandleCreated(_this, handleId, initialObjectId);
}

static HRESULT __stdcall OnICorProfilerCallback_HandleDestroyed(ICorProfilerCallback3 *_this, GCHandleID handleId)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_HandleDestroyed(_this, handleId);
}

static HRESULT __stdcall OnICorProfilerCallback_InitializeForAttach(ICorProfilerCallback3 *_this, IUnknown * pCorProfilerInfoUnk, void * pvClientData, UINT cbClientData)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_InitializeForAttach(_this, pCorProfilerInfoUnk, pvClientData, cbClientData);
}

static HRESULT __stdcall OnICorProfilerCallback_ProfilerAttachComplete(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ProfilerAttachComplete(_this);
}

static HRESULT __stdcall OnICorProfilerCallback_ProfilerDetachSucceeded(ICorProfilerCallback3 *_this)
{
  CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(_this);
  return lpPtr->OnICorProfilerCallback_ProfilerDetachSucceeded(_this);
}

// End of automatically generated section.
