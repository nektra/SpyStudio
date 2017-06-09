#pragma once
#include "exception.h"
#include "CommonFunctions.h"


class BufferList
{
public:
  struct Buffer
  {
    Buffer *next;
    static const size_t SIZE = 1 << 12; //4 KiB
    BYTE data[SIZE];
  };
private:
  static const unsigned THRESHOLD_NODE_COUNT = 1024;
  Buffer * volatile head;
  Buffer * volatile tail;
  unsigned volatile node_count;
  unsigned volatile active_nodes;
  unsigned volatile total_deallocations;
  enum
  {
    Normal,
    FullMemoryCycle,
  } state;
  CNktFastMutex mutex;
public:
  BufferList():
    head(0),
    tail(0),
    node_count(0),
    active_nodes(0),
    total_deallocations(0){}
  ~BufferList();
  Buffer *Acquire();
  void Release(Buffer *);
  //100: Fully compact. 0: "Fully sparse".
  int EstimateMemoryCompactness();
};

#include "int_to_string.inl"

#define DEC_DIGITS "0123456789"
#define hex_DIGITS "0123456789abcdef"
#define HEX_DIGITS "0123456789ABCDEF"

template <typename T>
_bstr_t int_to_bstr(T n)
{
  _bstr_t ret;
  size_t size;
  const size_t psize = 100;
  char preferred[psize];
  char *buffer = int_to_string(n, 10, DEC_DIGITS, 0, preferred, psize, size);
  if (!buffer)
    return preferred;
  ret = buffer;
  delete[] buffer;
  return ret;
}

template <typename T>
_bstr_t int_to_hex_bstr(T n)
{
  _bstr_t ret;
  size_t size;
  const size_t psize = 100;
  char preferred[psize];
  char *buffer = int_to_string<16>(n, HEX_DIGITS, 0, preferred, psize, size);
  if (!buffer)
    return preferred;
  ret = buffer;
  delete[] buffer;
  return ret;
}

class CoalescentIPC;

class AcquiredBuffer
{
  friend class CallEventSerializer;
  BufferList::Buffer *buffer;
  struct Queue
  {
    BufferList::Buffer *head,
                       *tail;
    unsigned size;
    Queue(): head(0), tail(0), size(0){}
  } queue;
  BufferList &bl;
  CoalescentIPC &cipc;
  size_t writing_position;
  //0: Not in middle of writing call event. 1: In middle.
  bool state;
  bool discard;
  bool finished;
  int case_forced;
  
  struct UTF8error
  {
    typedef enum
    {
      noError = 0,
      errInvalidData
    } ErrorCode;
  };
  static char *NewStringBuffer(size_t);
  static void FreeStringBuffer(char *buffer){
    delete[] buffer;
  }
  static void FreeStringBuffer(void *buffer){
    delete[] (char *)buffer;
  }
  UTF8error::ErrorCode EncodeToUTF8(const wchar_t *src, size_t src_len, int case_forced);
  char *EncodeString(char *, size_t, size_t &);
  void Commit(int = 0);
  void Send();
  void WriteBuffer(const void *buffer, size_t size);
  void *GetBuffer()
  {
    return buffer->data;
  }
  void WriteShortBuffer(const void *void_buffer, size_t size);
  void WriteSingleByte(BYTE byte)
  {
    assert(writing_position < buffer->SIZE);
    ((BYTE *)buffer->data)[writing_position++] = byte;
    if (writing_position == buffer->SIZE)
      Commit();
  }
  void WriteNullMarker()
  {
    WriteSingleByte(0);
  }
  void WriteSeparator();
  
  template <unsigned base, typename T>
  void AddIntegerBaseN(T n, const char *symbols, const char *prepend, size_t prepend_length, size_t minimum_size)
  {
    // preferred_buffer will almost always be used, avoiding unnecessary allocations.
    const size_t preferred_buffer_size = 100;
    char preferred_buffer[preferred_buffer_size];

    WriteSeparator();

    if (!!prepend && prepend_length)
      WriteBuffer((BYTE *)prepend, prepend_length);

    char *buffer;
    size_t size;
    try
    {
      buffer = int_to_string<base>(n, symbols, minimum_size, preferred_buffer, preferred_buffer_size, size);
    }
    catch (std::bad_alloc &)
    {
      THROW_CIPC_OUTOFMEMORY;
    }
    if (!buffer)
      WriteBuffer(preferred_buffer, size);
    else
    {
      WriteBuffer(buffer, size);
      delete[] buffer;
    }
  }
  void _AddString(const char *s, size_t n);
  void _AddString(const wchar_t *s, size_t n);
  void AddACP();

  void ForceCase2(int c)
  {
    this->case_forced = c;
  }

  void WriteSingleCharacter(char c);

public:
  AcquiredBuffer(BufferList &bl, CoalescentIPC &cipc);
  ~AcquiredBuffer();

  //Basic functions:
  //Note: These functions may throw instances of CIPC_Exception.
  void AddNULL();
  void AddNULL(int count);
  void AddAnsiString(const char *);
  void AddAnsiString(const char *, size_t);
  void AddStringMaybeAnsi(const char *s)
  {
    AddAnsiString(s);
  }
  void AddStringMaybeAnsi(const wchar_t *s)
  {
    AddString(s);
  }
  void AddString(const char *);
  void AddString(const char *, size_t);
  void AddString(const wchar_t *);
  void AddString(UNICODE_STRING *us)
  {
    if (!us)
      AddNULL();
    else
      AddString(us->Buffer, us->Length / sizeof(wchar_t));
  }
  void AddString(ANSI_STRING *us)
  {
    if (!us)
      AddNULL();
    else
      AddAnsiString(us->Buffer, us->Length / sizeof(char));
  }
  void AddString(const wchar_t *, size_t);
  void AddString(const _bstr_t &str)
  {
    AddString((const wchar_t *)str, str.length());
  }
  void AddString(const std::wstring &str)
  {
    AddString(str.c_str(), str.size());
  }
  void AddDualString(const wchar_t *str0, const std::wstring &str1)
  {
    WriteSeparator();
    _AddString(str0, wcslen(str0));
    _AddString(str1.c_str(), str1.size());
  }
  void AddIID(const _bstr_t &str)
  {
    WriteSeparator();
    WriteSingleByte('{');
    _AddString((const wchar_t *)str, str.length());
    WriteSingleByte('}');
  }
  void AddGUID(const GUID &guid);
  void AddEmptyString(unsigned how_many = 1)
  {
    for (; how_many; how_many--)
      WriteSeparator();
  }
  void AddDouble(double n);
  template <typename T>
  void AddIntegerWithPrefix(T n, char *prefix, size_t prefix_size = NKT_SIZE_T_MAX)
  {
    if (prefix_size == NKT_SIZE_T_MAX)
      for (prefix_size = 0; prefix[prefix_size]; prefix_size++);
    AddIntegerBaseN(n, 10, DEC_DIGITS, prefix, prefix_size, 0);
  }
  template <typename T>
  void AddInteger(T n)
  {
    AddIntegerBaseN<10>(n, DEC_DIGITS, 0, 0, 0);
  }
  template <>
  void AddInteger(bool n)
  {
    AddIntegerBaseN<10>((int)n, DEC_DIGITS, 0, 0, 0);
  }
  template <typename T>
  void AddIntegerForceUnsigned(T n)
  {
    AddIntegerBaseN<10, boost::make_unsigned<T>::type>(n, DEC_DIGITS, 0, 0, 0);
  }
  template <typename T>
  void AddHexInteger(T n)
  {
    AddIntegerBaseN<16, boost::make_unsigned<T>::type>(n, HEX_DIGITS, "0x", 2, 0);
  }
  template <typename T>
  void AddHexIntegerWithoutBasePrefix(T n)
  {
    AddIntegerBaseN<16, boost::make_unsigned<T>::type>(n, HEX_DIGITS, 0, 0, 0);
  }
  void AddBufferAsBase64String(const void *buffer, size_t size);
  void Discard()
  {
    discard = 1;
  }
  void AddEndOfMessage();

  friend class CaseRestorer;
  //A little bit of overengineering never hurt anyone.
  class CaseRestorer
  {
    int Case;
    AcquiredBuffer *buffer;
  public:
    CaseRestorer(AcquiredBuffer &ab, int c): Case(c), buffer(&ab) {}
    CaseRestorer(CaseRestorer &cr): Case(cr.Case), buffer(cr.buffer)
    {
      cr.buffer = 0;
    }
    ~CaseRestorer()
    {
      if (!!buffer)
        buffer->ForceCase2(Case);
    }
  };

  CaseRestorer ForceUpperCase()
  {
    return ForceCase(1);
  }
  CaseRestorer ForceLowerCase()
  {
    return ForceCase(-1);
  }
  CaseRestorer ForceNoCase()
  {
    return ForceCase(0);
  }
  CaseRestorer ForceCase(int c)
  {
    int ret = this->case_forced;
    this->case_forced = c;
    return CaseRestorer(*this, ret);
  }
#define SET_CASE(pointer, c) AcquiredBuffer::CaseRestorer case_restorer_temp = pointer->Force##c()
};
