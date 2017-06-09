#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include <cassert>
#include <limits>
#include <memory>
#include <string>

void WindowsCES::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  result = hcip.Result()->GetSizeTVal();
  abuffer->AddInteger(result);
}

template <bool IsAnsi>
bool CreateWindowExCES<IsAnsi>::IgnoreCall(INktHookCallInfoPlugin &hcip)
{
  INktStackTracePtr trace = hcip.StackTrace();
  return MH_CheckCallFrom(trace, L"user32.dll!CreateWindowExA") != FALSE;
}

void WindowsCES::AddClassNameFromClassAtom(INktParamPtr &param, int cast)
{
  if (valid_pointer(param))
  {
    long class_atom = param->GetIntResourceString();
    if (class_atom > 0)
      abuffer->AddHexInteger(class_atom);
    else if (cast < 0)
      abuffer->AddString(param->ReadString());
    else if (cast)
      abuffer->AddAnsiString(get_pointer_or_null<const char>(param));
    else
      abuffer->AddString(get_pointer_or_null<const wchar_t>(param));
  }
  else
    abuffer->AddEmptyString();
}

const int CAST_TO_CHAR = 1;
const int CAST_TO_WCHAR = 0;

void WindowsCES::AddParentClassName(INktParamPtr param)
{
  HWND parent_handle = (HWND)param->GetSizeTVal();
  if (parent_handle == HWND_MESSAGE)
  {
    abuffer->AddString("HWND_MESSAGE");
    return;
  }

  size_t characters;
  auto classname = BetterGetClassNameW(characters, parent_handle);
  if (!characters)
    abuffer->AddInteger((intptr_t)parent_handle);
  else
    abuffer->AddString(classname.get(), characters);
}

auto_array_ptr<wchar_t> BetterGetClassNameW(size_t &size, HWND handle)
{
  size = 100;
  auto_array_ptr<wchar_t> buffer;
  int res;
  do
  {
    buffer.reset(new (std::nothrow) wchar_t[size]);
    if (!buffer)
      THROW_CIPC_OUTOFMEMORY;
    res = GetClassNameW(handle, buffer.get(), (int)size);
    if (res)
    {
      size = res;
      continue;
    }
    //Ideally, I'd use err to decide what to do, but I have no idea what it can return
    //for GetClassName().
    DWORD err = GetLastError();
    if (err != ERROR_INSUFFICIENT_BUFFER)
    {
      size = 0;
      return 0;
    }
    buffer.reset();
    size *= 2;
  }
  while (!res);
  return buffer;
}

template <bool IsAnsi>
void CreateWindowExCES<IsAnsi>::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param;

  if (is_precall)
  {
    AddClassNameFromClassAtom(params->GetAt(1), IsAnsi ? CAST_TO_CHAR : CAST_TO_WCHAR);
    return;
  }

  param = params->GetAt(2);
  if (valid_pointer(param))
  {
    if (IsAnsi)
      abuffer->AddAnsiString(get_pointer_or_null<const char>(param));
    else
      abuffer->AddString(param->ReadString());
  }
  else
    abuffer->AddEmptyString();

  param = params->GetAt(10);
  this->AddOptionalModule(hcip, param->GetSizeTVal());

  size_t buffer_size = 0;
  auto_array_ptr<wchar_t> buffer;
  if (Success())
    buffer = BetterGetClassNameW(buffer_size, GetHandleValue());
  if (buffer_size != 0)
  {
    abuffer->AddString(buffer.get(), buffer_size);
  }
  else
  {
    param = params->GetAt(1);
    if (valid_pointer(param))
      AddClassNameFromClassAtom(param, IsAnsi ? CAST_TO_CHAR : CAST_TO_WCHAR);
    else
      abuffer->AddEmptyString();
  }

  param = params->GetAt(0);
  abuffer->AddIntegerForceUnsigned(param->GetULongVal());
  
  param = params->GetAt(3);
  abuffer->AddIntegerForceUnsigned(param->GetULongVal());

  AddParentClassName(params->GetAt(8));
}

struct DLG_TEMPLATE
{
  DWORD      style;
  DWORD      exStyle;
  DWORD      helpId;
  WORD       nbItems;
  short      x;
  short      y;
  short      cx;
  short      cy;
  LPCWSTR    menuName;
  LPCWSTR    className;
  LPCWSTR    caption;
  WORD       pointSize;
  WORD       weight;
  BOOL       italic;
  LPCWSTR    faceName;
  BOOL       dialogEx;
};

inline DWORD get_dword(const WORD *&p){
	DWORD ret = *(const DWORD *)p;
	p += 2;
	return ret;
}

static LPCSTR DIALOG_ParseTemplate32(DLG_TEMPLATE *result, void *dlg_template)
{
  auto p = (const WORD *)dlg_template;
  WORD signature;
  WORD dlgver;

  dlgver = *p++;
  signature = *p++;

  if (dlgver == 1 && signature == 0xffff)  /* DIALOGEX resource */
  {
    result->dialogEx = TRUE;
    result->helpId   = get_dword(p);
    result->exStyle  = get_dword(p);
    result->style  = get_dword(p);
  }
  else
  {
    p -= 2;
    result->style = get_dword(p);
	result->dialogEx = FALSE;
    result->helpId   = 0;
    result->exStyle  = get_dword(p);
  }
  result->nbItems = *p++;
  result->x     = *p++;
  result->y     = *p++;
  result->cx    = *p++;
  result->cy    = *p++;

  /* Get the menu name */

  switch(*p)
  {
    case 0x0000:
      result->menuName = NULL;
      p++;
      break;
    case 0xffff:
      result->menuName = (LPCWSTR)(UINT_PTR)*++p;
      p++;
      break;
    default:
      result->menuName = (LPCWSTR)p;
      p += wcslen( result->menuName ) + 1;
      break;
  }

  /* Get the class name */

  switch(*p)
  {
    case 0x0000:
      result->className = WC_DIALOG;
      p++;
      break;
    case 0xffff:
      result->className = (LPCWSTR)(UINT_PTR)*++p;
      p++;
      break;
    default:
      result->className = (LPCWSTR)p;
      p += wcslen( result->className ) + 1;
      break;
  }

  /* Get the window caption */

  result->caption = (LPCWSTR)p;
  p += wcslen( result->caption ) + 1;

  /* Get the font name */

  result->pointSize = 0;
  result->faceName = NULL;
  result->weight = FW_DONTCARE;
  result->italic = FALSE;

  if (result->style & DS_SETFONT)
  {
    result->pointSize = *p;
    p++;

    /* If pointSize is 0x7fff, it means that we need to use the font
     * in NONCLIENTMETRICSW.lfMessageFont, and NOT read the weight,
     * italic, and facename from the dialog dlg_template.
     */
    if (result->pointSize == 0x7fff)
    {
      /* We could call SystemParametersInfo here, but then we'd have
       * to convert from pixel size to point size (which can be
       * imprecise).
       */
    }
    else
    {
      if (result->dialogEx)
      {
        result->weight = *p; p++;
        result->italic = LOBYTE(*p); p++;
      }
      result->faceName = (LPCWSTR)p;
      p += wcslen( result->faceName ) + 1;

    }
  }

  /* First control is on dword boundary */
  return (LPCSTR)((((UINT_PTR)p) + 3) & ~3);
}



void CreateDialogIndirectCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  if (is_precall)
    return;

  auto hinstance = params->GetAt(0)->GetSizeTVal();

  DLG_TEMPLATE t;
  if (Success())
  {
    auto p = (void *)params->GetAt(1)->GetPointerVal();
    DIALOG_ParseTemplate32(&t, p);

    abuffer->AddString(t.caption);
  }
  else
    abuffer->AddEmptyString();
  this->AddOptionalModule(hcip, hinstance);
  if (Success())
  {
    if (t.className == WC_DIALOG)
      abuffer->AddString("#32770");
    else
      abuffer->AddString(t.className);
    abuffer->AddIntegerForceUnsigned(t.exStyle);
    abuffer->AddIntegerForceUnsigned(t.style);
  }
  else
    abuffer->AddEmptyString(3);
  AddParentClassName(params->GetAt(2));
}

void DialogBoxIndirectCES::AddResultInner(INktHookCallInfoPlugin &hcip){
  DialogBoxIndirectCES_result = (INT_PTR)hcip.Result()->GetSizeTVal();
  abuffer->AddInteger(DialogBoxIndirectCES_result);
}
