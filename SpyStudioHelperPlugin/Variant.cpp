#include "stdafx.h"
#include "Variant.h"
#include "TlsData.h"
#include <OleAuto.h>

/*
static HRESULT VARIANT_ValidateType(VARTYPE vt)
{
  VARTYPE vtExtra = vt & VT_EXTRA_TYPE;

  vt &= VT_TYPEMASK;

  if (!(vtExtra & (VT_VECTOR|VT_RESERVED)))
  {
    if (vt < VT_VOID || vt == VT_RECORD || vt == VT_CLSID)
    {
      if ((vtExtra & (VT_BYREF|VT_ARRAY)) && vt <= VT_NULL)
        return DISP_E_BADVARTYPE;
      if (vt != (VARTYPE)15)
        return S_OK;
    }
  }
  return DISP_E_BADVARTYPE;
}
*/

HRESULT NktVariantClear(VARIANT *pVarg)
{
  HRESULT hres;
  auto &type = V_VT(pVarg);

  hres = S_OK; //VARIANT_ValidateType(type);

  if (SUCCEEDED(hres))
  {
    if (!V_ISBYREF(pVarg))
    {
      if (V_ISARRAY(pVarg) || type == VT_SAFEARRAY)
        hres = SafeArrayDestroy(V_ARRAY(pVarg));
      else if (type == VT_BSTR)
      {
        SysFreeString(V_BSTR(pVarg));
      }
      else if (type == VT_RECORD)
      {
        auto pBr = pVarg->pRecInfo;
        if (pBr)
        {
          pBr->RecordClear(pVarg->pvRecord);
          pBr->RecordClear(pVarg->pRecInfo);
        }
      }
      else if ((type == VT_DISPATCH || type == VT_UNKNOWN) && V_UNKNOWN(pVarg))
        pVarg->punkVal->Release();
    }
    type = VT_EMPTY;
  }
  return hres;
}

NktVariant::NktVariant()
{
  VariantInit(&var);
}

NktVariant::~NktVariant()
{
  NktVariantClear(&var);
}

void NktVariant::operator=(long x)
{
  var.vt = VT_I4;
  var.lVal = x;
}

void NktVariant::operator=(long long x)
{
  var.vt = VT_I8;
  var.llVal = x;
}

void NktVariant::operator=(nullptr_t)
{
  var.vt = VT_BYREF;
  var.byref = nullptr;
}

void NktVariant::operator=(const _bstr_t &bstrSrc)
{
  var.vt = VT_BSTR;

  if (!bstrSrc)
    V_BSTR(&var) = NULL;
  else
  {
    BSTR bstr = static_cast<wchar_t*>(bstrSrc);
    V_BSTR(&var) = ::SysAllocStringByteLen(reinterpret_cast<char*>(bstr), ::SysStringByteLen(bstr));

    if (V_BSTR(&var) == NULL) 
      _com_issue_error(E_OUTOFMEMORY);
  }
}