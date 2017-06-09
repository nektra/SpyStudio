#include "stdafx.h"
#include "CustomHookData.h"
#include "SerializerInheritors.h"
#include <typeinfo>
#ifdef _DEBUG
#include <sstream>
#endif

CustomHookString CopyString(const char *p, size_t n)
{
  if (n == NKT_SIZE_T_MAX)
    n = strlen(p);
  CustomHookString ret(new (std::nothrow) char [n + 1]);
  if (!ret)
    return ret;
  memcpy(ret.get(), p, n);
  ret[n] = 0;
  return ret;
}

using namespace tinyxml2;

#define SET_FROM_ATTRIBUTE(name, element, type)              \
{                                                            \
  const XMLAttribute *attr = (element).FindAttribute(#name); \
  if (!!attr)                                                \
    (name) = attr->type();                                   \
}

#define SET_STRING_FROM_ATTRIBUTE(name, element) \
{                                                \
  const char *name = 0;                          \
  SET_FROM_ATTRIBUTE(name, element, Value);      \
  if (!!name)                                    \
    this->name = CopyString(name);               \
}

#define ON(x) if (!strcmp(child->Value(), #x))

CustomHook::CustomHook(const XMLNode &node)
{
  //Default initialization
  before = 0;
  after = 1;
  paramsBefore = 1;
  paramsAfter = 1;
  stackBefore = 1;
  forceReturn = 0;

  //-----------------------------------------------------------

  const XMLElement &hook = (const XMLElement &)node;
  SET_FROM_ATTRIBUTE(before, hook, BoolValue);
  {
    Maybe<bool> onlyBefore;
    SET_FROM_ATTRIBUTE(onlyBefore, hook, BoolValue);
    if (onlyBefore.IsSet())
      after = !(before = (bool)onlyBefore);
  }
  SET_FROM_ATTRIBUTE(priority, hook, IntValue);
  SET_FROM_ATTRIBUTE(paramsBefore, hook, BoolValue);
  SET_FROM_ATTRIBUTE(paramsAfter, hook, BoolValue);
  SET_FROM_ATTRIBUTE(stackBefore, hook, BoolValue);
  params.reserve(8);
  skipCalls.reserve(8);
  for (auto child = hook.FirstChildElement(); !!child; child = child->NextSiblingElement())
  {
    const XMLElement &element = *child;
    ON(return)
      ReadReturnType(element);
    else ON(function)
    {
      this->function = CopyString(element.GetText());
      this->displayName = CopyString(element.Attribute("displayName"));
    }
    else ON(group)
      this->group = CopyString(element.GetText());
    else ON(functionString)
      this->functionString = CopyString(element.GetText());
    else ON(param)
    {
      this->params.push_back(new CustomHookParam(element));
    }
    else ON(skipCalls)
      ReadSkipCalls(element);
    else
      assert(0);
  }
  if (skipCalls.size())
    stackBefore = 1;
}

CustomHook::~CustomHook()
{
  for (size_t i = 0; i < params.size(); i++)
    delete params[i];
}

void CustomHook::ReadSkipCalls(const XMLElement &element)
{
  for (auto child = element.FirstChildElement(); !!child; child = child->NextSiblingElement())
  {
    const XMLElement &element = (const XMLElement &)*child;
    ON (callerFrame)
    {
      CustomHookString str = CopyString(element.GetText());
      if (str)
        skipCalls.push_back(CustomHookString(str));
    }
    else
      assert(0);
  }
}

void CustomHook::ReadReturnType(const tinyxml2::XMLElement &element)
{
#define HANDLER_CASE(x)                          \
  if (!strcmp(s, #x))                            \
  {                                              \
    result_handler = &CustomHookCallCES::Add##x; \
  }

  returnType = CopyString(element.GetText());
  if (!this->returnType)
    return;
  const char *s = returnType.get();
#include "AddHandlerInitializations.inl"
  else
    result_handler = 0;

  forceReturn = element.BoolAttribute("force");
#undef HANDLER_CASE
}

bool CustomHook::ShouldAddParameters(bool is_precall) const
{
  bool any_param = 0;
  for (size_t i = 0; i < params.size() && !any_param; i++)
    any_param |= params[i]->get_before();
  return !is_precall && paramsAfter || is_precall && (!after || paramsBefore || any_param);
}

void CustomHookParam::ReadContext(const char *context)
{
  if (!context)
    return;
#include "HandlerInitializations.inl"
  else
    param_handler = 0;
}

CustomHookParam::CustomHookParam(const tinyxml2::XMLNode &node, unsigned recursion)
{
  before = 1;
  index = -1;
  result = TV_NONE;
  param_handler = 0;
  pointer = 0;
  const XMLElement &el = (const XMLElement &)node;
  SET_FROM_ATTRIBUTE(index, el, IntValue);
  SET_FROM_ATTRIBUTE(pointer, el, BoolValue);
  {
    const XMLAttribute *attr = el.FindAttribute("result");
    if (!!attr)
      result = (TrinaryValue)attr->BoolValue();
  }
  SET_STRING_FROM_ATTRIBUTE(context, el);
  ReadContext(context.get());
  SET_STRING_FROM_ATTRIBUTE(type, el);
  SET_STRING_FROM_ATTRIBUTE(helpString, el);
  params.reserve(8);
  for (auto child = el.FirstChildElement(); !!child; child = child->NextSiblingElement())
  {
    const XMLElement &element = *child;
    ON(param)
      this->params.push_back(new CustomHookParam(element, recursion + 1));
    else ON(match); //Do nothing.
    else
      assert(0);
  }
}

CustomHookParam::~CustomHookParam()
{
  for (size_t i = 0; i < params.size(); i++)
    delete params[i];
}
