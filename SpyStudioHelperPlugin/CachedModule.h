#ifndef CACHEDMODULE_H
#define CACHEDMODULE_H

typedef mword_t address_t;

template <typename T>
class IntervalList;

class CachedModulesList
{
  IntervalList<std::wstring> *list;
  CNktFastMutex mutex;
public:
  CachedModulesList();
  ~CachedModulesList();
  void AddModule(address_t, size_t, const _bstr_t &);
  void AddModule(address_t, size_t, const std::wstring &);
  bool GetPath(std::wstring &dst, address_t);
  void *GetModule(address_t);
  const std::wstring &GetModuleInfo(address_t &base_address, address_t &size, void *);
};

#endif
