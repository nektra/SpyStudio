#pragma once

#include <vector>
#include <list>
#include <utility>

class CustomHookParam;

template <typename T>
class Maybe
{
  bool set;
  T object;
public:
  Maybe<T>(): set(0){}
  bool IsSet() const
  {
    return set;
  }
  operator const T &() const
  {
    return object;
  }
  T &Get()
  {
    set = 1;
    return object;
  }
  const T &operator=(const T &o)
  {
    set=1;
    return object=o;
  }
};

typedef boost::shared_array<char> CustomHookString;

CustomHookString CopyString(const char *p, size_t n = NKT_SIZE_T_MAX);

class CustomHookCallCES;

typedef void (CustomHookCallCES::*CustomHookResultHandler)(INktHookCallInfoPlugin &);

#define DEFINE_GETTER(x, y) x get_##y() const { return y; }
#define DEFINE_BOOL_GETTER(x) DEFINE_GETTER(bool, x);
#define DEFINE_INTEGER_GETTER(x) DEFINE_GETTER(int, x);
#define DEFINE_STRING_GETTER(x) const char *get_##x() const { return x.get(); }

class CustomHook
{
  bool before;
  bool after;
  int priority;
  bool paramsBefore;
  bool paramsAfter;
  bool stackBefore;
  bool forceReturn;
  CustomHookString returnType;
  CustomHookResultHandler result_handler;
  CustomHookString function;
  CustomHookString displayName;
  CustomHookString functionString;
  CustomHookString group;
  std::vector<CustomHookParam *> params;
  std::vector<CustomHookString> skipCalls;
  CustomHook(const CustomHook &){}
  void ReadSkipCalls(const tinyxml2::XMLElement &element);
  void ReadReturnType(const tinyxml2::XMLElement &element);
public:
  CustomHook(const tinyxml2::XMLNode &node);
  ~CustomHook();
  DEFINE_BOOL_GETTER(before)
  DEFINE_BOOL_GETTER(after)
  DEFINE_INTEGER_GETTER(priority)
  DEFINE_BOOL_GETTER(paramsBefore)
  DEFINE_BOOL_GETTER(paramsAfter)
  DEFINE_BOOL_GETTER(stackBefore)
  DEFINE_BOOL_GETTER(forceReturn)
  DEFINE_STRING_GETTER(returnType)
  DEFINE_STRING_GETTER(function)
  DEFINE_STRING_GETTER(displayName)
  DEFINE_STRING_GETTER(functionString)
  DEFINE_STRING_GETTER(group)
  const char *get_skipCall(size_t i) const
  {
    return i < skipCalls.size() ? skipCalls[i].get() : 0;
  }
  DEFINE_GETTER(CustomHookResultHandler, result_handler);

  bool ShouldAddParameters(bool is_precall) const;
  DEFINE_GETTER(const std::vector<CustomHookParam *> &, params)
};

typedef std::pair<CustomHookString, CustomHookString> CustomHookMatch;
enum TrinaryValue
{
  TV_FALSE = 0,
  TV_TRUE = 1,
  TV_NONE = -1
};

class CustomHookParam
{
  bool before;
  CustomHookString context;
  CustomHookParamHandler param_handler;
  int index;
  bool pointer;
  TrinaryValue result;
  CustomHookString type;
  CustomHookString helpString;
  std::vector<CustomHookParam *> params;
  void ReadContext(const char *context);
public:
  CustomHookParam(const tinyxml2::XMLNode &node, unsigned recursion = 0);
  ~CustomHookParam();
  DEFINE_BOOL_GETTER(before)
  DEFINE_STRING_GETTER(context)
  DEFINE_INTEGER_GETTER(index)
  DEFINE_BOOL_GETTER(pointer)
  DEFINE_GETTER(TrinaryValue, result)
  DEFINE_STRING_GETTER(type)
  DEFINE_STRING_GETTER(helpString)
  DEFINE_GETTER(CustomHookParamHandler, param_handler)
  DEFINE_GETTER(const std::vector<CustomHookParam *> &, params)
};
