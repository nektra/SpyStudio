#pragma once
#include <limits>
#include "TlsData.h"

#define DEFINE_STANDARD_HANDLER(id, type)                                               \
EXTERN_C HRESULT WINAPI id(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,      \
                                     __in INktHookCallInfoPlugin *lpHookCallInfoPlugin) \
{                                                                                       \
  WRITE_CALL_EVENT_OF_TYPE(type);                                                       \
}

#ifdef _DEBUG
#define DEFINE_DEBUG_HANDLER___(id, type)                                               \
EXTERN_C HRESULT WINAPI id(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,      \
                                     __in INktHookCallInfoPlugin *lpHookCallInfoPlugin) \
{                                                                                       \
  Nektra::DebugBreak();                                                                 \
  WRITE_CALL_EVENT_OF_TYPE(type);                                                       \
}
#else
#define DEFINE_DEBUG_HANDLER___(id, type) DEFINE_STANDARD_HANDLER(id, type)
#endif
  //DBGPRINT("SpyStudioHelperPlugin::OnFunctionCall called. Function: %S", (BSTR)functionName);

#define DEFINE_SECONDARYHOOK_HANDLER(id)                                                            \
EXTERN_C HRESULT WINAPI On##id##_Secondary(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,  \
                                            __in INktHookCallInfoPlugin *lpHookCallInfoPlugin)      \
{                                                                                                   \
  global_secHookMgr.SetBit(SECONDARYHOOK_BIT_##id);                                                 \
  lpHookCallInfoPlugin->FilterSpyMgrEvent();                                                        \
  return S_OK;                                                                                      \
}

template <typename T>
inline T *get_pointer_or_null(INktParamPtr param)
{
  if (!valid_pointer(param))
    return 0;
  T *ret = (T *)param->GetPointerVal();
  return ret;
}

bool GetKeyNameFromHandle(__out std::wstring &dst, __in HKEY hKey, __in CTlsData *lpTlsData, bool in_thinapp_process);
bool GetFileNameFromHandle(__out std::wstring &dst, __in HANDLE handle, __in CTlsData *lpTlsData);
bool tls_functioncalled_init(TNktComPtr<CTlsData> &tlsDataContainer, INktHookCallInfoPlugin &__lpHookCallInfoPlugin);

inline bool valid_pointer(INktParamPtr ptr)
{
  return ptr && ptr->GetIsNullPointer() == VARIANT_FALSE;
}

const char *NTSTATUS_to_string(NTSTATUS s);
const char *HRESULT_to_string(NTSTATUS s);

template <typename T>
typename boost::enable_if_c<boost::is_unsigned<T>::value, T>::type sign_negation(T n)
{
  return ~n + 1;
}

template <typename T>
typename boost::enable_if_c<!boost::is_unsigned<T>::value, T>::type sign_negation(T n)
{
  return -n; 
}

inline bool valid_handle(HANDLE h)
{
  return !!h && h != INVALID_HANDLE_VALUE;
}

inline HANDLE handle_from_pid(DWORD pid)
{
  return OpenProcess(PROCESS_ALL_ACCESS, 0, pid);
}

inline HANDLE self_process()
{
  return GetCurrentProcess();
}

#ifdef _DEBUG
extern char last_byte;
#endif

template <typename T, typename T2>
static void WriteBuffer(
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
    in_buffer += bytes_to_write;
    writing_position += bytes_to_write;
    src_size -= bytes_to_write;
    if (writing_position != dst_size)
      continue;
    (_this->*Send)(param);
    out_buffer = (BYTE *)(_this->*GetBuffer)();
  }
  assert(writing_position < dst_size);
}

bool GetGlobalCurrentThreadHandle(HANDLE &dst, DWORD &win_error);
INktModulePtr GetModuleByAddress(INktProcessPtr process, SIZE_T address, bool exact = 0);
class CoalescentIPC;
bool GetModulePathByAddress(std::wstring &dst, INktProcessPtr process, SIZE_T address, bool exact, CoalescentIPC *);

// true if path matches the regex [a-zA-Z]*\:.*
// The input pointer is modified to point one past the colon.
template <typename T>
bool is_rooted(const T *&path)
{
  for (; *path; path++)
  {
    if (!isalpha(*path))
      return *(path++) == ':';
  }
  return 0;
}

// true if path matches the regex [a-zA-Z]*\:.*
// The input index is modified to point one past the colon.
template <typename T>
bool is_rooted(std::basic_string<T> &unit, const std::basic_string<T> &path)
{
  for (size_t i = 0; i < path.size(); i++)
  {
    if (!isalpha(path[i]))
    {
      if (path[i] == ':')
      {
        unit = path.substr(0, i + 1);
        return 1;
      }
      return 0;
    }
  }
  return 0;
}

template <typename T>
inline bool is_path_separator(T c)
{
  return c == '\\' || c == '/';
}

class InvalidPathException : public std::exception
{
public:
  const char *what()
  {
    return "The path is not well-formed.";
  }
};

template <typename T>
bool begins_with(const T *path, const char *string)
{
  for (; *path && *string; path++, string++)
    if (tolower(*path) != tolower((T)*string))
      return 0;
  return !*string;
}

template <typename T1, typename T2>
bool begins_with(const std::basic_string<T1> &path, size_t start, const T2 *string)
{
  for (size_t a = start; a < path.size() && *string; a++, string++)
    if (tolower(path[a]) != (T1)tolower(*string))
      return 0;
  return !*string;
}

template <typename T1, typename T2>
inline bool begins_with(const std::basic_string<T1> &path, const T2 *string)
{
  return begins_with(path, 0, string);
}

template <typename T1, typename T2>
struct SelectLargest
{};

template <>
struct SelectLargest<char, char>
{
  typedef char T;
};

template <>
struct SelectLargest<char, wchar_t>
{
  typedef wchar_t T;
};

template <>
struct SelectLargest<wchar_t, char>
{
  typedef wchar_t T;
};

template <>
struct SelectLargest<wchar_t, wchar_t>
{
  typedef wchar_t T;
};

template <typename T1, typename T2>
inline bool ends_with(const std::basic_string<T1> &s1, const std::basic_string<T2> &s2)
{
  typedef typename SelectLargest<T1, T2>::T T;
  typename std::basic_string<T1>::const_reverse_iterator i1 = s1.rbegin(),
    e1 = s1.rend();
  typename std::basic_string<T2>::const_reverse_iterator i2 = s2.rbegin(),
    e2 = s2.rend();
  for (;i1 != e1 && i2 != e2 && (T)*i1 == (T)*i2; ++i1, ++i2);
  return i2 == e2;
}

template <typename T1, typename T2>
inline bool ends_with(const std::basic_string<T1> &s1, const T2 *s2)
{
  return ends_with(s1, (std::basic_string<T2>)s2);
}

template <typename T>
size_t basic_strlen(const T *s)
{
  size_t ret = 0;
  while (s[ret])
    ret++;
  return ret;
}

template <typename T>
std::basic_string<T> replace(const std::basic_string<T> &s1, const T *s2, const T *s3)
{
  size_t n = basic_strlen(s2);
  for (size_t a = 0; a < s1.size(); a++)
  {
    if (begins_with(s1, a, s2))
    {
      std::basic_string<T> ret = s1;
      ret.replace(ret.begin() + a, ret.begin() + a + n, std::basic_string<T>(s3));
      return ret;
    }
  }
  return s1;
}


template <typename T>
void add_path_to_vector(std::vector<std::basic_string<T> > &dst, const std::basic_string<T> &path)
{
  add_path_to_vector(dst, path.c_str());
}

template <typename T>
void add_path_to_vector(std::vector<std::basic_string<T> > &dst, const T *path)
{
  while (is_path_separator(*path))
    path++;
  if (!*path)
    return;
  bool last_was_separator = 0;
  typedef std::basic_string<T> string_t;
  string_t accumulator;
  for (; *path; path++)
  {
    if (last_was_separator)
    {
      if (is_path_separator(*path))
        continue;
      if (begins_with(path, ".\\") || begins_with(path, "./"))
        continue;
      if (begins_with(path, "..\\") || begins_with(path, "../"))
      {
        if (!dst.size())
          throw InvalidPathException();
        dst.pop_back();
        path++;
        continue;
      }
    }
    else if (is_path_separator(*path))
    {
      dst.push_back(accumulator);
      accumulator.clear();
      last_was_separator = 1;
      continue;
    }
    accumulator.push_back(*path);
    last_was_separator = 0;
  }
  if (accumulator.size())
    dst.push_back(accumulator);
}

template <typename T>
std::basic_string<T> canonicalize_path(const T *path, size_t n, const std::basic_string<T> &working_directory, bool final_slash = 0)
{
  std::basic_string<T> temp(path, n);
  return canonicalize_path(temp, working_directory, final_slash);
}

/*
template <typename T>
bool is_drive_root(const std::basic_string<T> &path)
{
  if (path.size() < 3)
    return 0;
  if (!(isalpha(path[0]) && path[1] == ':' && path[2] == '\\'))
    return 0;
  for (size_t i = 3; i < path.size(); i++)
    if (path[i] != '\\')
      return 0;
  return 1;
}
*/

template <typename T>
bool handle_explicit_working_directory(std::basic_string<T> &path, std::basic_string<T> &new_working_directory)
{
  size_t first_quote = path.find('\"');
  if (first_quote == path.npos)
    return 0;
  size_t second_quote = path.find('\"', first_quote + 1);
  if (second_quote == path.npos)
    return 0;
  new_working_directory = path.substr(0, first_quote);
  first_quote++;
  path = path.substr(first_quote, second_quote - first_quote);
  return 1;
}

template <typename T>
std::basic_string<T> construct_string(const char *s)
{
  std::basic_string<T> ret;
  ret.reserve(strlen(s));
  for (; *s; s++)
    ret.push_back(*s);
  return ret;
}

template <typename T>
std::basic_string<T> canonicalize_path(std::basic_string<T> path, std::basic_string<T> working_directory, bool final_slash = 0)
{
  typedef std::basic_string<T> string_t;
  std::wstring ret;
  ret.reserve(512);

  bool is_filesystem_path = 1;

  //WARNING: The order of these blocks matters.
  if (begins_with(path, "\\??\\pipe") || begins_with(path, "\\??\\unc"))
  {
    path = path.substr(3);
    is_filesystem_path = 0;
  }
  else if (begins_with(path, "\\??\\") || begins_with(path, "\\\\?\\"))
    path = path.substr(4);
  else if (begins_with(path, "\\\\"))
    return construct_string<T>("\\Device\\Mup") + path.substr(1);
  else if (begins_with(path, "\\Device\\"))
    return path;

  if (!path.size())
    return path;
  
#ifdef _DEBUG
  if (begins_with(path, "\\??\\"))
    __debugbreak();
#endif

  if (is_filesystem_path)
    path = path_to_long_path(path);
  string_t new_working_directory;
  if (handle_explicit_working_directory(path, new_working_directory) && new_working_directory.size())
    working_directory = new_working_directory;
#if defined _M_X64
  //temp = replace(temp, L"\\windows\\system32", L"\\windows\\SysWOW64");
#endif

  std::vector<string_t> subpaths;
  subpaths.reserve(128);
  string_t working_directory_unit;
  string_t unit;
  bool wd_is_rooted = is_rooted(working_directory_unit, working_directory);
  bool path_is_rooted = is_rooted(unit, path);

  if (is_filesystem_path)
  {
    if (path_is_rooted)
    {
      subpaths.push_back(unit);
      path = path.substr(unit.size());
    }
    else if (path.size() && is_path_separator(path[0]) && wd_is_rooted)
      subpaths.push_back(working_directory_unit);
    else
      add_path_to_vector(subpaths, working_directory);
  }

  add_path_to_vector(subpaths, path);

  if (is_filesystem_path && subpaths.size())
  {
    ret.append(subpaths.front());
    if (subpaths.size() == 1)
      final_slash = 1;
  }

  for (size_t a = is_filesystem_path ? 1 : 0; a < subpaths.size(); a++)
  {
    ret.push_back(L'\\');
    ret.append(subpaths[a]);
  }

#ifdef _DEBUG
  if (is_filesystem_path && ret.size() && ret[0] == '\\')
    __debugbreak();
#endif

  if (final_slash)
    ret.push_back('\\');
  /*
  else if (!is_drive_root(ret))
  {
    while (ret.size() && ret[ret.size() - 1] == '\\')
      ret.resize(ret.size() - 1);
  }
  */

  return ret;
}

std::wstring get_current_directory();

//Compares symbols of the form "foo.dll!Bar" with case insensitivity only
//before the exclamation mark.
template <typename T>
int symbol_strcmp(const T *a, const T *b, size_t n = std::numeric_limits<size_t>::max())
{
  size_t i = 0;
  for (; i < n && (*a || *b); a++, b++, i++)
  {
    char c = tolower(*a),
      d = tolower(*b);
    if (c != d)
      return c - d;
    if (c == '!')
    {
      a++;
      b++;
      break;
    }
  }
  for (; i < n && *a || *b; a++, b++, i++)
    if (*a != *b)
      return *a - *b;
  return 0;
}

inline int symbol_strcmp(_bstr_t &a, const wchar_t *b)
{
  size_t n = a.length();
  int cmp = symbol_strcmp((const wchar_t *)a, b, n);
  if (cmp)
    //Strings are unequal.
    return cmp;
  //Strings are equal up to n.
  //If b[n], then b is longer, otherwise the strings are exactly equal.
  return b[n];
}

template <typename T1, typename T2>
inline bool check_flag(T1 x, T2 y)
{
  return (x & (T1)y) == y;
}

template <typename T1>
inline bool check_bit(T1 x, unsigned n)
{
  return !!(x & (T1)(1 << n));
}

std::wstring path_to_long_path_volatile(std::wstring &);
inline std::wstring path_to_long_path(const std::wstring &path)
{
  std::wstring temp = path;
  return path_to_long_path_volatile(temp);
}

inline std::wstring path_to_long_path(const _bstr_t &s){
  std::wstring temp((const wchar_t *)s, s.length());
  return path_to_long_path_volatile(temp);
}

template <typename T>
void strip_to_filename(std::basic_string<T> &path)
{
  static const char slashes[] = { '\\', '/' };
  for (wchar_t c : slashes)
  {
    auto slash = path.rfind(c);
    if (slash == path.npos)
      continue;
    path = path.substr(slash + 1);
    return;
  }
}

HRESULT __stdcall NktVariantClear(VARIANT *);

template <class Iterator, typename Value, typename Lambda>
Iterator my_lower_bound(Iterator begin, Iterator end, const Value &val, const Lambda &f)
{
  auto count = end - begin;
  while (count > 0)
  {
    auto count2 = count / 2;
    Iterator mid = begin + count2;
    if (f(*mid, val))
    {
      begin = ++mid;
      count -= count2 + 1;
    }
    else
      count = count2;
  }
  return begin;
}

template <typename ArrayT, size_t N, typename CodeT, typename LambdaT>
const char *code_to_string(CodeT code, ArrayT (&intervals)[N], const char **strings, LambdaT lambda)
{
#define WITHIN (p->begin <= x && x < p->begin + p->size)

  auto x = (boost::make_unsigned<CodeT>::type)code;
  interval *p = my_lower_bound(
    intervals,
    intervals + N,
    x,
    lambda
  );
  if (p == intervals + N)
    p--;
  if (!WITHIN){
    p--;
    if (!WITHIN)
      return nullptr;
  }
  return strings[x - p->begin + p->index];
}

template <typename T>
typename 
  boost::make_unsigned<T>::type cast_to_unsigned(const T &x)
{
  return (boost::make_unsigned<T>::type)x;
}

template <typename T>
T max_value(const T &x)
{
  return std::numeric_limits<T>::max();
}
