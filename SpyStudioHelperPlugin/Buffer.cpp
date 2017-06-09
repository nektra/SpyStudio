#include "stdafx.h"
#include "Protocol.h"
#include "exception.h"
#include "CIPC.h"
#include "Buffer.h"
#include "CommonFunctions.h"
#include "base64.h"

BufferList::~BufferList()
{
  while (head)
  {
    Buffer *next = head->next;
    delete head;
    head = next;
  }
}

BufferList::Buffer *BufferList::Acquire()
{
  Buffer *ret;
  {
    CNktAutoFastMutex am(&mutex);
    if (!head)
    {
      ret = new (std::nothrow) Buffer;
      if (!ret)
        THROW_CIPC_OUTOFMEMORY;
    }
    else
    {
      ret = (BufferList::Buffer *)head;
      head = head->next;
      if (!head)
        tail = 0;
      node_count--;
    }
  }
  active_nodes++;
  ret->next = 0;
#ifdef _DEBUG
  memset(ret->data, 0, ret->SIZE);
#endif
  return ret;
}

void BufferList::Release(Buffer *buffer)
{
  CNktAutoFastMutex am(&mutex);
  if (node_count < THRESHOLD_NODE_COUNT)
  {
    if (!tail)
      head = tail = buffer;
    else
    {
      tail->next = buffer;
      tail = buffer;
    }
    node_count++;
  }
  else
  {
    delete buffer;
  }
  active_nodes--;
}

int BufferList::EstimateMemoryCompactness()
{
  if (!node_count)
    return 100;
  
  struct MemoryBlock
  {
    MEMORY_BASIC_INFORMATION mbi;
    unsigned object_count;
  };

  std::map<uintptr_t, MemoryBlock> map;

  for (Buffer *it = head; it; it = it->next)
  {
    MEMORY_BASIC_INFORMATION mbi;
    if (!VirtualQuery(it, &mbi, sizeof(mbi)))
      continue;
    std::map<uintptr_t, MemoryBlock>::iterator i = map.find((uintptr_t)mbi.AllocationBase);
    if (i!= map.end())
      i->second.object_count++;
    else
    {
      MemoryBlock block;
      block.mbi = mbi;
      block.object_count = 1;
      map[(uintptr_t)mbi.AllocationBase] = block;
    }
  }

  size_t total_size = 0;

  for (std::map<uintptr_t, MemoryBlock>::iterator i = map.begin(), e = map.end(); i != e; ++i)
    total_size += i->second.mbi.RegionSize;

  size_t n = sizeof(Buffer);
  double ret = double(node_count * n) / double(total_size) * 100.0;
  DBGPRINT("Estimated memory compactness: %f %%", ret);
  return int(ret);
}

AcquiredBuffer::AcquiredBuffer(BufferList &bl, CoalescentIPC &cipc):
  buffer(0),
  bl(bl),
  cipc(cipc),
  state(0),
  writing_position(0),
  discard(0),
  case_forced(0),
  finished(0)
{
  buffer = bl.Acquire();
}

AcquiredBuffer::~AcquiredBuffer()
{
  if (!discard && finished && (writing_position || queue.size))
  {
    Send();
    cipc.events_transterred++;
  }
  if (buffer)
    bl.Release(buffer);
}

char *AcquiredBuffer::NewStringBuffer(size_t n)
{
  char *ret = new (std::nothrow) char[n];
  if (!ret)
    THROW_CIPC_OUTOFMEMORY;
  return ret;
}

void AcquiredBuffer::Commit(int dummy)
{
  if (!queue.head)
    queue.head = queue.tail = buffer;
  else
  {
    queue.tail->next = buffer;
    queue.tail = buffer;
  }
  buffer->next = 0;
  queue.size++;
  buffer = bl.Acquire();
  writing_position = 0;
}

#ifdef DETAILED_LOG
#include <fstream>

std::ofstream detailed_log("detailed.log", std::ios::trunc);
#endif

#ifdef _DEBUG
char last_byte = '|';
#endif

void AcquiredBuffer::Send()
{
#ifdef DETAILED_LOG
  detailed_log <<"AcquiredBuffer::Send(): Now writing a new event.\n";
#endif
  while (queue.head)
  {
    cipc.WriteBuffer(queue.head->data, queue.head->SIZE);
    BufferList::Buffer *temp = queue.head;
    queue.head = queue.head->next;
    bl.Release(temp);
  }
  vassert(!!buffer);
  cipc.WriteBuffer(buffer->data, writing_position);
  vassert(last_byte == '|');
  cipc.IncrementEventCount();
  writing_position = 0;
#ifdef DETAILED_LOG
  detailed_log <<"AcquiredBuffer::Send(): Finished writing an event.\n";
#endif
}

struct CharacterEncodingLookupTableElement
{
  char chars[2];
  char size;
};

static const CharacterEncodingLookupTableElement lookup[] = {
  { '\\', '0', 2 },
  { 1, 0, 1 },
  { 2, 0, 1 },
  { 3, 0, 1 },
  { 4, 0, 1 },
  { 5, 0, 1 },
  { 6, 0, 1 },
  { 7, 0, 1 },
  { 8, 0, 1 },
  { 9, 0, 1 },
  { 10, 0, 1 },
  { 11, 0, 1 },
  { 12, 0, 1 },
  { 13, 0, 1 },
  { 14, 0, 1 },
  { 15, 0, 1 },
  { 16, 0, 1 },
  { 17, 0, 1 },
  { 18, 0, 1 },
  { 19, 0, 1 },
  { 20, 0, 1 },
  { 21, 0, 1 },
  { 22, 0, 1 },
  { 23, 0, 1 },
  { 24, 0, 1 },
  { 25, 0, 1 },
  { 26, 0, 1 },
  { 27, 0, 1 },
  { 28, 0, 1 },
  { 29, 0, 1 },
  { 30, 0, 1 },
  { 31, 0, 1 },
  { 32, 0, 1 },
  { 33, 0, 1 },
  { 34, 0, 1 },
  { 35, 0, 1 },
  { 36, 0, 1 },
  { 37, 0, 1 },
  { 38, 0, 1 },
  { 39, 0, 1 },
  { '\\', '(', 2 },
  { '\\', ')', 2 },
  { 42, 0, 1 },
  { 43, 0, 1 },
  { 44, 0, 1 },
  { 45, 0, 1 },
  { 46, 0, 1 },
  { 47, 0, 1 },
  { 48, 0, 1 },
  { 49, 0, 1 },
  { 50, 0, 1 },
  { 51, 0, 1 },
  { 52, 0, 1 },
  { 53, 0, 1 },
  { 54, 0, 1 },
  { 55, 0, 1 },
  { 56, 0, 1 },
  { 57, 0, 1 },
  { '\\', ':', 2 },
  { 59, 0, 1 },
  { 60, 0, 1 },
  { 61, 0, 1 },
  { 62, 0, 1 },
  { 63, 0, 1 },
  { 64, 0, 1 },
  { 65, 0, 1 },
  { 66, 0, 1 },
  { 67, 0, 1 },
  { 68, 0, 1 },
  { 69, 0, 1 },
  { 70, 0, 1 },
  { 71, 0, 1 },
  { 72, 0, 1 },
  { 73, 0, 1 },
  { 74, 0, 1 },
  { 75, 0, 1 },
  { 76, 0, 1 },
  { 77, 0, 1 },
  { 78, 0, 1 },
  { 79, 0, 1 },
  { 80, 0, 1 },
  { 81, 0, 1 },
  { 82, 0, 1 },
  { 83, 0, 1 },
  { 84, 0, 1 },
  { 85, 0, 1 },
  { 86, 0, 1 },
  { 87, 0, 1 },
  { 88, 0, 1 },
  { 89, 0, 1 },
  { 90, 0, 1 },
  { 91, 0, 1 },
  { '\\', '\\', 2 },
  { 93, 0, 1 },
  { 94, 0, 1 },
  { 95, 0, 1 },
  { 96, 0, 1 },
  { 97, 0, 1 },
  { 98, 0, 1 },
  { 99, 0, 1 },
  { 100, 0, 1 },
  { 101, 0, 1 },
  { 102, 0, 1 },
  { 103, 0, 1 },
  { 104, 0, 1 },
  { 105, 0, 1 },
  { 106, 0, 1 },
  { 107, 0, 1 },
  { 108, 0, 1 },
  { 109, 0, 1 },
  { 110, 0, 1 },
  { 111, 0, 1 },
  { 112, 0, 1 },
  { 113, 0, 1 },
  { 114, 0, 1 },
  { 115, 0, 1 },
  { 116, 0, 1 },
  { 117, 0, 1 },
  { 118, 0, 1 },
  { 119, 0, 1 },
  { 120, 0, 1 },
  { 121, 0, 1 },
  { 122, 0, 1 },
  { 123, 0, 1 },
  { '\\', '|', 2 },
  { 125, 0, 1 },
  { 126, 0, 1 },
  { 127, 0, 1 }
};

size_t EncodeCharacter(unsigned char dst[2], unsigned char src)
{
  if (src < 0x80)
  {
    CharacterEncodingLookupTableElement el = lookup[src];
    dst[0] = el.chars[0];
    dst[1] = el.chars[1];
    return el.size;
  }
  dst[0] = 0xC0 | (src >> 6);
  dst[1] = 0x80 | (src & 0x3F);
  return 2;
}

AcquiredBuffer::UTF8error::ErrorCode AcquiredBuffer::EncodeToUTF8(const wchar_t *src, size_t src_len, int force_case)
{
  static const BYTE UTF8_BOM[4] = {0x00, 0xC0, 0xE0, 0xF0};
  DWORD encoding_value;
  if (!src)
    return UTF8error::noError;
  for (size_t i = 0; i < src_len; i++)
  {
    encoding_value = (DWORD)(src[i]);
    if (src[i] >= 0xD800 && src[i] <= 0xDBFF)
    {
      if (src[i + 1] < 0xDC00 || src[i + 1] > 0xDFFF)
        return UTF8error::errInvalidData;
      encoding_value = (encoding_value - 0xD800UL) << 10;
      encoding_value += 0x10000;
      encoding_value = (DWORD)(src[i + 1] - 0xDC00);
      assert(encoding_value < 0x120000);
    }

    int k;
    if (encoding_value < 0x80)
    {
      if (case_forced > 0)
        encoding_value = toupper(encoding_value);
      else if (case_forced < 0)
        encoding_value = tolower(encoding_value);
      WriteSingleCharacter((unsigned char)encoding_value);
      continue;
    }
    else if (encoding_value < 0x800)
      k = 2;
    else if (encoding_value < 0x10000)
      k = 3;
    else
      k = 4;

    BYTE array[4];

    for (int j = k - 1; j; j--)
    {
      array[j] = (BYTE)((encoding_value | 0x80) & 0xBF);
      encoding_value >>= 6;
    }
    array[0] = (BYTE)(encoding_value | UTF8_BOM[k - 1]);
    for (int j = 0; j < k; j++)
      WriteSingleByte(array[j]);
  }
  return UTF8error::noError;
}

void AcquiredBuffer::WriteBuffer(const void *void_buffer, size_t size){
  ::WriteBuffer(buffer->SIZE, writing_position, void_buffer, size, this, &AcquiredBuffer::Commit, &AcquiredBuffer::GetBuffer);
}

void AcquiredBuffer::WriteShortBuffer(const void *void_buffer, size_t size)
{
  assert(writing_position < buffer->SIZE);
  const BYTE *in_buffer = (const BYTE *)void_buffer;
  for (; size; size--, in_buffer++)
    WriteSingleByte(*in_buffer);
}

void AcquiredBuffer::WriteSeparator()
{
  if (!state)
    state = 1;
  else
    WriteSingleByte(':');
}

void AcquiredBuffer::AddEndOfMessage()
{
  WriteSingleByte('|');
  state = 0;
  finished = 1;
}

void AcquiredBuffer::AddNULL()
{
  WriteSeparator();
  WriteNullMarker();
}

void AcquiredBuffer::AddNULL(int count)
{
  for(int i = 0; i < count; i++)
	  AddNULL();
}

void AcquiredBuffer::AddACP()
{
  UINT acp = GetACP();
  WriteSingleByte('(');
  char decimal_acp[100];
  char *allocated;
  size_t final_size;
  try
  {
    allocated = int_to_string<10>(acp, DEC_DIGITS, 0, decimal_acp, 100, final_size);
  }
  catch (std::bad_alloc &)
  {
    THROW_CIPC_OUTOFMEMORY;
  }
  if (!allocated)
    WriteBuffer(decimal_acp, final_size);
  else
  {
    WriteBuffer(allocated, final_size);
    delete[] allocated;
  }
  WriteSingleByte(')');
}

void AcquiredBuffer::AddAnsiString(const char *s)
{
  if (!s)
  {
    AddNULL();
    return;
  }
  AddAnsiString(s, strlen(s));
}

void AcquiredBuffer::AddAnsiString(const char *s, size_t n)
{
  if (!s)
  {
    AddNULL();
    return;
  }
  size_t size = MultiByteToWideChar(GetACP(), 0, s, (int)n, nullptr, 0);
  if (!size)
  {
    AddString("<<invalid data>>");
    return;
  }
  auto_array_ptr<wchar_t> buffer(new wchar_t[size]);
  size = MultiByteToWideChar(GetACP(), 0, s, (int)n, buffer.get(), (int)size);
  AddString(buffer.get(), size);
}

void AcquiredBuffer::_AddString(const char *s, size_t n)
{
  class UnsizedStringIterator
  {
  protected:
    const char *s;
    AcquiredBuffer *ab;
  public:
    UnsizedStringIterator(const char *s, AcquiredBuffer *ab): s(s), ab(ab){}
    void operator()()
    {
      ab->WriteSingleCharacter(*s);
    }
    virtual operator bool()
    {
      return !!*++s;
    }
  };
  class SizedStringIterator : public UnsizedStringIterator
  {
    size_t n;
  public:
    SizedStringIterator(const char *s, size_t n, AcquiredBuffer *ab): UnsizedStringIterator(s, ab), n(n){}
    virtual operator bool()
    {
      s++;
      return !!--n;
    }
  };

  if (!s || !n)
    return;
  UnsizedStringIterator usi(s, this);
  SizedStringIterator ssi(s, n, this);
  UnsizedStringIterator &u = n == NKT_SIZE_T_MAX ? usi : ssi;
  do
    u();
  while (u);
}

void AcquiredBuffer::AddString(const char *s)
{
  WriteSeparator();
  _AddString(s, NKT_SIZE_T_MAX);
}

void AcquiredBuffer::AddString(const char *s, size_t n)
{
  WriteSeparator();
  _AddString(s, n);
}

void AcquiredBuffer::AddString(const wchar_t *s)
{
  AddString(s, wcslen(s));
}

void AcquiredBuffer::AddString(const wchar_t *s, size_t n)
{
  WriteSeparator();
  if (!n)
    return;
  _AddString(s, n);
}

void AcquiredBuffer::_AddString(const wchar_t *s, size_t n)
{
  UTF8error::ErrorCode error = EncodeToUTF8(s, n, case_forced);

  if (error == UTF8error::errInvalidData)
  {
    throw CIPC_DataErrorException(
      ERROR_INVALID_DATA,
      "AcquiredBuffer::_AddString(): Failed to encode to UTF-8 because the source UTF-16 data is invalid."
    );
  }
}

void AcquiredBuffer::AddDouble(double n)
{
  char string[100];
  int characters = sprintf_s(string, "%f", n);
  WriteSeparator();
  WriteBuffer(string, characters);
}

void AcquiredBuffer::AddGUID(const GUID &guid)
{
  WriteSeparator();

  char temp[100];
  char *alloc_buffer,
    *buffer;
  size_t size;

  WriteSingleByte('{');

  alloc_buffer = int_to_string<16>(guid.Data1, HEX_DIGITS, 8, temp, (size_t)100, size);
  buffer = !alloc_buffer ? temp : alloc_buffer;
  WriteBuffer(buffer, size);
  if (!!alloc_buffer)
    delete[] alloc_buffer;

  WriteSingleByte('-');

  alloc_buffer = int_to_string<16>(guid.Data2, HEX_DIGITS, 4, temp, (size_t)100, size);
  buffer = !alloc_buffer ? temp : alloc_buffer;
  WriteBuffer(buffer, size);
  if (!!alloc_buffer)
    delete[] alloc_buffer;

  WriteSingleByte('-');

  alloc_buffer = int_to_string<16>(guid.Data3, HEX_DIGITS, 4, temp, (size_t)100, size);
  buffer = !alloc_buffer ? temp : alloc_buffer;
  WriteBuffer(buffer, size);
  if (!!alloc_buffer)
    delete[] alloc_buffer;

  WriteSingleByte('-');

  for (size_t i = 0; i < sizeof(guid.Data4); i++)
  {
    alloc_buffer = int_to_string<16>(guid.Data4[i], HEX_DIGITS, 2, temp, (size_t)100, size);
    buffer = !alloc_buffer ? temp : alloc_buffer;
    WriteBuffer(buffer, size);
    if (!!alloc_buffer)
      delete[] alloc_buffer;
    if (i == 1)
      WriteSingleByte('-');
  }

  WriteSingleByte('}');
}

void AcquiredBuffer::WriteSingleCharacter(char c)
{
  unsigned char encoded[2];
  size_t encoded_size = EncodeCharacter(encoded, (unsigned char)c);
  if (encoded_size == 1)
    WriteSingleByte((BYTE)encoded[0]);
  else
  {
    assert(encoded_size == 2);
    WriteSingleByte((BYTE)encoded[0]);
    WriteSingleByte((BYTE)encoded[1]);
  }
}

void AcquiredBuffer::AddBufferAsBase64String(const void *buffer, size_t size)
{
  //friend struct F;
  struct F{
    AcquiredBuffer *_this;
    F(AcquiredBuffer *_this): _this(_this){}
    void operator()(char c)
    {
      _this->WriteSingleByte(c);
    }
    size_t operator()()
    {
      return 0;
    }
  } f(this);
  base64_encoder<0>::encode(buffer, size, f);
}
