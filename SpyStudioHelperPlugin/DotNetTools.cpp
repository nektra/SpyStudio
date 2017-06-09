#include "stdafx.h"
#include "DotNetTools.h"


ICorProfilerInfo *DotNetTools::_lpDotNetProfilerInfo;

void DotNetTools::SetDotNetProfilerInfo(ICorProfilerInfo *lpDotNetProfilerInfo)
{
  if(lpDotNetProfilerInfo != NULL)
  {
    lpDotNetProfilerInfo->AddRef();
  }
  else if(_lpDotNetProfilerInfo != NULL)
  {
    _lpDotNetProfilerInfo->Release();
  }
	_lpDotNetProfilerInfo = lpDotNetProfilerInfo;
}

_bstr_t DotNetTools::GetClassNameFromObjectId(ObjectID objId)
{
	_bstr_t className;
	ClassID classId;
	if(_lpDotNetProfilerInfo == NULL)
		return className;

	HRESULT hRes = _lpDotNetProfilerInfo->GetClassFromObject(objId, &classId);
	if(FAILED(hRes))
		return className;

	className = GetClassNameFromClassId(classId);
	
	return className;
}

_bstr_t DotNetTools::GetClassNameFromClassId(ClassID classId)
{
  ModuleID modId;
	mdTypeDef classMetaToken;
  IMetaDataImport *lpMetaDataImport;
	_bstr_t className;
	WCHAR lpClassName[1024];
	ULONG charsCopied;

    HRESULT hRes = _lpDotNetProfilerInfo->GetClassIDInfo(classId, &modId, &classMetaToken);
	if(FAILED(hRes))
		return className;
	hRes = _lpDotNetProfilerInfo->GetModuleMetaData(modId, ofRead, IID_IMetaDataImport, (IUnknown **) &lpMetaDataImport);
	if(FAILED(hRes))
		return className;

	lpMetaDataImport->GetTypeDefProps(classMetaToken, lpClassName, sizeof(lpClassName), &charsCopied, NULL, NULL);
    if(charsCopied < sizeof(lpClassName))
	{
		className = lpClassName;
	}
	return className;
}

bool DotNetTools::GetFunctionProperties(FunctionID fncId, _bstr_t &className, _bstr_t &procName)
{  
  IMetaDataImport *lpMetaDataImport;
  mdToken token;
  mdTypeDef classDef;
  HRESULT hRes = _lpDotNetProfilerInfo->GetTokenAndMetaDataFromFunction(fncId, IID_IMetaDataImport, (IUnknown**)&lpMetaDataImport, &token);
  if(FAILED(hRes))
    return false;

  WCHAR buf[1024];
  ULONG copiedChars;
  ULONG nChars = sizeof(buf) / sizeof(buf[0]);
  bool ret = false;

  hRes = lpMetaDataImport->GetMethodProps(token, &classDef, buf, nChars, &copiedChars, NULL, NULL, NULL, NULL, NULL);
  if(SUCCEEDED(hRes))
  {
    if(copiedChars < nChars)
    {
      procName = buf;
    
      hRes = lpMetaDataImport->GetTypeDefProps(classDef, buf, nChars, &copiedChars, NULL, NULL);
      if(copiedChars < nChars)
      {
        className = buf;
        ret = true;
      }
    }
  }
  
  lpMetaDataImport->Release();

  return ret;
}

bool DotNetTools::GetModuleInfo(ModuleID modId, SIZE_T &modAddress)
{
	_bstr_t className;
	if(_lpDotNetProfilerInfo == NULL)
		return false;

          //virtual HRESULT STDMETHODCALLTYPE GetModuleInfo( 
          //  /* [in] */ ModuleID moduleId,
          //  /* [out] */ LPCBYTE *ppBaseLoadAddress,
          //  /* [in] */ ULONG cchName,
          //  /* [out] */ ULONG *pcchName,
          //  /* [annotation][out] */ 
          //  _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
          //  /* [out] */ AssemblyID *pAssemblyId) = 0;

  HRESULT hRes = _lpDotNetProfilerInfo->GetModuleInfo(modId, (LPCBYTE*) &modAddress, 0, NULL, NULL, NULL);
	if(FAILED(hRes))
		return false;

	return true;
}

