#pragma once
#include <cstdlib>

struct StringList
{
  enum NodeType
  {
    TYPE_STRING,
    TYPE_BSTR,
  };
  struct Node
  {
    Node *next;
    NodeType type;
  };
  struct NodeString
  {
    Node *next;
    NodeType type;
    size_t size;
    wchar_t str[1];
  };
  struct NodeBStr
  {
    Node *next;
    NodeType type;
    _bstr_t str;
  };
  Node *head,
       *tail;
  unsigned size;

  StringList(): head(0), tail(0), size(0) {}
  ~StringList()
  {
    while (head)
    {
      Node *temp = head;
      head = head->next;
      free(temp);
    }
  }
  void add(Node *n)
  {
    if (!tail)
      head = tail = n;
    else
    {
      tail->next = n;
      tail = n;
    }
    size++;
  }
  void add(NodeString *n)
  {
    add((Node *)n);
  }
  void add(NodeBStr *n)
  {
    add((Node *)n);
  }

  static NodeString *AllocateStringNode(size_t size, const wchar_t *str = 0)
  {
    NodeString *ret = (NodeString *)malloc(sizeof(NodeString) + size * sizeof(wchar_t));
    if (!ret)
      THROW_CIPC_OUTOFMEMORY;
    ret->next = 0;
    ret->type = TYPE_STRING;
    ret->size = size;
    if (str)
      memcpy(ret->str, str, size * sizeof(wchar_t));
    return ret;
  }
  static NodeString *AllocateStringNode(_bstr_t &bstr)
  {
    return AllocateStringNode(bstr.length(), (const wchar_t *)bstr);
  }
  static NodeString *AllocateStringNode(UNICODE_STRING *us)
  {
    return AllocateStringNode(us->Length / sizeof(wchar_t), us->Buffer);
  }
  template <typename T>
  static NodeString *AllocateIntegerNode(T n)
  {
    char buffer[100];
    size_t size;
    char *allocd = int_to_string<10>(n, DEC_DIGITS, 0, buffer, 100, size);
    NodeString *ret;
    ret = AllocateStringNode(size);
    if (!allocd)
    {
      for (size_t i = 0; i < size; i++)
        ret->str[i] = buffer[i];
    }
    else
    {
      for (size_t i = 0; i < size; i++)
        ret->str[i] = allocd[i];
      delete[] allocd;
    }
    return ret;
  }
  static NodeBStr *AllocateBStrNode(size_t size)
  {
    NodeBStr *ret = (NodeBStr *)malloc(sizeof(NodeBStr));
    if (!ret)
      THROW_CIPC_OUTOFMEMORY;
    ret->next = 0;
    ret->type = TYPE_BSTR;
    return ret;
  }
};
