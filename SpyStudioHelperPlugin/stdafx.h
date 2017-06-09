#include <Windows.h>

#include "..\Deviare2\Source\Common\EngBaseObj.h"
#include "..\Deviare2\Source\Common\MemoryManager.h"
#include "..\Deviare2\Source\Common\Debug.h"
#include "..\Deviare2\Source\Common\AutoPtr.h"
#include "..\Deviare2\Source\Common\ArrayList.h"
#include "..\Deviare2\Source\Common\ComPtr.h"
#include "..\Deviare2\Source\Common\StringLiteW.h"
#include "..\Deviare2\Source\Common\DynamicAPIs.h"
#include "..\Deviare2\Source\Common\NtInternals.h"
#include "tinyxml2.h"

#include <vector>
#include <string>
#include <limits>

#ifdef max
#undef max
#endif

#ifdef min
#undef min
#endif

//Note: I used Boost 1.52.0. I recommend a version equal or higher.
#include <boost/unordered_map.hpp>
#include <boost/unordered_set.hpp>
#include <boost/cstdint.hpp>
#include <boost/type_traits/make_unsigned.hpp>
#include <boost/shared_array.hpp>

#include "nterror.h"

// Disable performance warning when converting BOOL to bool.
#pragma warning (disable : 4800)

//Note: You may redefine this macro, but do note that the type in question must behave "like" std::map.
//In particular:
//  1. dictionary_t<T1, T2> must be a type of associative array from T1 to T2.
//  2. T2 &operator[](const T1 &) should be overloaded. If the dictionary doesn't contain the item, the
//     item should be constructed with the default constructor, then added and returned.
//  3. find() should return an iterator to an element equal to the operand, or an iterator equal to
//     end() if no such element exists.
//  4. Unlike with std::map, iterating a dictionary_t needs not return the elements in order.
//     Furthermore, it's preferable if a strict weak ordering is not required of T1.
#define dictionary_t boost::unordered_map

#define hashset_t boost::unordered_set

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS
#include <atlbase.h>
#include <atlstr.h>
#include <winternl.h>

#if defined _M_IX86
  #import "../Deviare2/bin/DeviareCOM.dll" named_guids, raw_dispinterfaces, auto_rename
#elif defined _M_X64
  #import "../Deviare2/bin/DeviareCOM64.dll" named_guids, raw_dispinterfaces, auto_rename
#else
  #error Unsupported platform
#endif

#include <cor.h>
#include <corprof.h>

using namespace Deviare2;

#if defined _M_IX86

typedef long my_ssize_t;
typedef unsigned long my_size_t;
#define MACHINE_BITNESS 32

#elif defined _M_X64

typedef __int64 my_ssize_t;
typedef unsigned __int64 my_size_t;
#define MACHINE_BITNESS 64

#endif

using boost::uint8_t;
using boost::uint32_t;

typedef uint8_t byte_t;

#define WRITE_CALL_EVENT_OF_TYPE(type) \
  return perform_writes(type(global_cipc),*lpHookInfo,*lpHookCallInfoPlugin)

#include "TypeDeclarations.h"

#ifdef USE_STACKWALKER
#include "StackWalker.h"

#define BEGIN_STACKWALKER_TRY __try {
#define STACKWALKER_CATCH } __except (exception_handler(GetExceptionInformation(), GetExceptionCode())) { abort(); }
#else

#define BEGIN_STACKWALKER_TRY
#define STACKWALKER_CATCH

#endif

#ifdef _DEBUG
#define vassert(x) if (!(x)) __debugbreak()
#define DBGPRINT(...) Nektra::DebugPrintLnA(__VA_ARGS__)
#else
#define vassert(x)
#define DBGPRINT(...)
#endif

#define _CONCAT(x, y) x##y
#define CONCAT(x, y) _CONCAT(x, y)
#define APPEND_BITNESS(x) CONCAT(x, MACHINE_BITNESS)
#define _LITERAL_INSTEAD_OF_MACRO(x) x
#define LITERAL_INSTEAD_OF_MACRO(x) _LITERAL_INSTEAD_OF_MACRO(x)
