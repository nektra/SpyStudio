#include "stdafx.h"
#include "TypeDeclarations.h"
#include "CachedModule.h"
#include <vector>
#include <algorithm>

//#define CODE_COVERAGE
//#define UNIT_TESTING

#ifdef UNIT_TESTING
#include <cassert>
#endif

#ifdef CODE_COVERAGE
bool code_covered[13] = { 0 };

#define COVERED(x) code_covered[x] = 1
#else
#define COVERED(x)
#endif

template <typename Iterator, typename Value, typename Lambda>
Iterator my_lower_bound(Iterator begin, Iterator end, const Value &val, const Lambda &f)
{
  Iterator::difference_type count = end - begin;
  while (count > 0)
  {
    Iterator::difference_type count2 = count / 2;
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

template <typename Iterator, typename Value, typename Lambda>
Iterator my_upper_bound(Iterator begin, Iterator end, const Value &val, const Lambda &f)
{
  Iterator::difference_type count = end - begin;
  while (count > 0)
  {
    Iterator::difference_type count2 = count / 2;
    Iterator mid = begin + count2;
    if (!f(val, *mid))
    {
      begin = ++mid;
      count -= count2 + 1;
    }
    else
      count = count2;
  }
  return begin;
}

template <typename T2>
class IntervalList;

template <typename T>
class Interval
{
  address_t start,
    end;
public:
  T extra_data;
  Interval(address_t start, size_t size): start(start), end(start + size){}
  Interval(address_t start, size_t size, const T &ed): start(start), end(start + size), extra_data(ed){}
  address_t GetStart() const
  {
    return start;
  }
  address_t GetEnd() const
  {
    return end;
  }
};

BOOL AttachCurrentProcessToDebugger();

#define RECORD_HISTORY

template <typename T>
class IntervalList{
  typedef std::vector<Interval<T> *> vector_t;
  typedef typename vector_t::iterator forward;
  typedef typename vector_t::reverse_iterator reverse;
  vector_t data;
#ifdef RECORD_HISTORY
  vector_t history;
#endif

  forward find_first_end_higher(address_t addr){
    COVERED(0);
    forward i(data.begin()),
      e(data.end());
    struct Functor{
      bool operator()(Interval<T> *a, address_t b) const{
        return a->GetEnd() <= b;
      }
      bool operator()(address_t b, Interval<T> *a) const{
        return a->GetEnd() >= b;
      }
    };
    return my_lower_bound(i, e, addr, Functor());
  }
  forward find_last_start_lower(address_t addr){
    COVERED(1);
    COVERED(3);
    if (!data.size())
    {
      COVERED(2);
      return data.end();
    }
    struct Functor
    {
      bool operator()(Interval<T> *a, address_t b) const
      {
        return a->GetStart() >= b;
      }
      bool operator()(address_t b, Interval<T> *a) const
      {
        return a->GetStart() <= b;
      }
    };

    struct MyReverseIterator
    {
      typedef int difference_type;
      vector_t *v;
      int index;
      MyReverseIterator(vector_t &v, int i): v(&v), index(i){}
      const MyReverseIterator &operator++()
      {
        --index;
        return *this;
      }
      int operator-(const MyReverseIterator &b) const
      {
        return b.index - index;
      }
      MyReverseIterator operator+(int n) const
      {
        MyReverseIterator ret = *this;
        ret.index -= n;
        return ret;
      }
      Interval<T> *operator*() const
      {
        return (*v)[index];
      }
    };

    MyReverseIterator i(data, (int)data.size() - 1);
    MyReverseIterator e(data, -1);

    MyReverseIterator it = my_lower_bound(i, e, addr, Functor());
    return data.begin() + (it.index + 1);
  }
  struct Deleter{
    void operator()(Interval<T> *p) const{
      COVERED(4);
      delete p;
    }
  };
public:
  ~IntervalList(){
    COVERED(5);
    std::for_each(data.begin(), data.end(), Deleter());
#ifdef RECORD_HISTORY
    std::for_each(history.begin(), history.end(), Deleter());
#endif
  }
  void Add(Interval<T> *p){
#ifdef RECORD_HISTORY
    history.push_back(new Interval<T>(*p));
#endif
    COVERED(6);
    forward first = find_first_end_higher(p->GetStart());
    forward last = find_last_start_lower(p->GetEnd());
    if (last - first < 1)
    {
      COVERED(7);
      data.insert(first, p);
    }
    else
    {
      COVERED(8);
#ifdef _DEBUG
      if (first - last > 1)
        AttachCurrentProcessToDebugger();
#endif

      delete *first;
      *(first++) = p;
      
      std::for_each(first, last, Deleter());
      data.erase(first, last);
    }
  }
  Interval<T> *Find(address_t addr){
    forward i(data.begin()),
      e(data.end());
    struct Functor{
      bool operator()(Interval<T> *a, address_t b) const{
        return a->GetStart() < b;
      }
      bool operator()(address_t b, Interval<T> *a) const{
        return a->GetStart() > b;
      }
    };
    forward i2 = my_upper_bound(i, e, addr, Functor());
    if (i2 == data.begin())
    {
      COVERED(9);
      return 0;
    }
    COVERED(10);
    i2--;
    if (addr >= (*i2)->GetEnd())
    {
      COVERED(11);
      return 0;
    }
    COVERED(12);
    return *i2;
  }
#ifdef UNIT_TESTING
  template <size_t N>
  bool Contains(int (&array)[N]){
    if (data.size() != N)
      return 0;
    for (size_t i = 0; i < N; i++)
      if (data[i]->extra_data != array[i])
        return 0;
    return 1;
  }
#endif
};

CachedModulesList::CachedModulesList()
{
  list = new IntervalList<std::wstring>;
}

CachedModulesList::~CachedModulesList()
{
  delete list;
}

void CachedModulesList::AddModule(address_t address, size_t length, const _bstr_t &path)
{
  Interval<std::wstring> *interval = new Interval<std::wstring>(address, length);
  interval->extra_data.assign((const wchar_t *)path, path.length());

  CNktAutoFastMutex am(&mutex);
  list->Add(interval);
}

void CachedModulesList::AddModule(address_t address, size_t length, const std::wstring &path)
{
  Interval<std::wstring> *interval = new Interval<std::wstring>(address, length, path);
  CNktAutoFastMutex am(&mutex);
  list->Add(interval);
}

bool CachedModulesList::GetPath(std::wstring &dst, address_t address)
{
  CNktAutoFastMutex am(&mutex);
  Interval<std::wstring> *interval = list->Find(address);
  if (!interval)
    return 0;
  dst = interval->extra_data;
  return 1;
}

void *CachedModulesList::GetModule(address_t address)
{
  CNktAutoFastMutex am(&mutex);
  Interval<std::wstring> *interval = list->Find(address);
  return interval;
}

const std::wstring &CachedModulesList::GetModuleInfo(address_t &base_address, address_t &size, void *p)
{
  //Locking the mutex is not necessary.
  Interval<std::wstring> *interval = (Interval<std::wstring> *)p;
  base_address = interval->GetStart();
  size = interval->GetEnd() - base_address;
  return interval->extra_data;
}
