#pragma once
#ifdef _M_X64
typedef __int64 smword_t;
typedef unsigned __int64 mword_t;
#else
typedef __int32 smword_t;
typedef unsigned __int32 mword_t;
#endif
//const mword_t MWORD_MAX = std::numeric_limits<mword_t>::max();

typedef smword_t HookId_t;

template <typename T>
class auto_array_ptr
{
public:
  typedef void (*deallocation_fp)(void *);
private:
  T *pointer;
  deallocation_fp func;
public:
  auto_array_ptr(T *p = 0): pointer(p), func(0){}
  auto_array_ptr(auto_array_ptr<T> &o): pointer(o.release()), func(o.func) {}
  ~auto_array_ptr()
  {
    reset();
  }
  void set_deallocation_function(deallocation_fp f)
  {
    func = f;
  }
  void reset_deallocation_function()
  {
    func = 0;
  }
  void set_free()
  {
    func = free;
  }
  const auto_array_ptr<T> &operator=(auto_array_ptr<T> &o)
  {
    reset(o.release());
    this->func = o.func;
    return *this;
  }
  T *release()
  {
    T *ret = pointer;
    pointer = 0;
    return ret;
  }
  void reset(T *p = 0)
  {
    if (!!func)
      func(pointer);
    else
      delete[] pointer;
    pointer = p;
  }
  operator bool() const
  {
    return !!pointer;
  }
  bool operator!() const
  {
    return !pointer;
  }
  T *get()
  {
    return pointer;
  }
  const T *get() const
  {
    return pointer;
  }
  T &operator*()
  {
    return *pointer;
  }
  const T &operator*() const
  {
    return *pointer;
  }
  T &operator[](size_t index)
  {
    return pointer[index];
  }
  const T &operator[](size_t index) const
  {
    return pointer[index];
  }
  template <typename Integer>
  T *operator+(Integer index)
  {
    return pointer + index;
  }
  template <typename Integer>
  const T *operator+(Integer index) const
  {
    return pointer + index;
  }
};

class CustomHookCallCES;
typedef void (CustomHookCallCES::*CustomHookResultHandler)(INktHookCallInfoPlugin &);
typedef bool (CustomHookCallCES::*CustomHookParamHandler)(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip);
