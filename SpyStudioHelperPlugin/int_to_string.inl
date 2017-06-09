template <unsigned base, typename T>
inline char *int_to_string_zero_helper(
                                T n,
                                const char *symbols,
                                size_t minimum_size,
                                char *preferred_buffer,
                                const size_t &preferred_buffer_size,
                                size_t &final_size
                               )
{
  bool freeit = 0;
  char *buffer;

  final_size = 1;

  if (final_size < minimum_size)
    final_size = minimum_size;
  if (final_size < preferred_buffer_size)
    buffer = preferred_buffer;
  else
  {
    buffer = new (std::nothrow) char[final_size + 1];
    if (!buffer)
    {
      THROW_CIPC_OUTOFMEMORY;
    }
    freeit = 1;
  }
  memset(buffer, symbols[0], final_size);
  buffer[final_size] = 0;

  return !freeit ? 0 : buffer;
}

template <unsigned base, typename T>
inline typename boost::enable_if_c<boost::is_unsigned<T>::value, int>::type int_to_string_non_zero_helper2(
     T &n,
     const char *symbols,
     size_t minimum_size,
     char *preferred_buffer,
     const size_t &preferred_buffer_size,
     size_t &final_size,
     size_t &length,
     bool &is_negative
    )
{
  return 0;
}

template <unsigned base, typename T>
inline typename boost::enable_if_c<!boost::is_unsigned<T>::value, int>::type int_to_string_non_zero_helper2(
     T &n,
     const char *symbols,
     size_t minimum_size,
     char *preferred_buffer,
     const size_t &preferred_buffer_size,
     size_t &final_size,
     size_t &length,
     bool &is_negative
    )
{
  if (n < (T)0)
  {
    is_negative = 1;
    length++;
    n = sign_negation(n);
  }
  return 0;
}

template <unsigned base, typename T>
inline char *int_to_string_non_zero_helper(
                                           T n,
                                           const char *symbols,
                                           size_t minimum_size,
                                           char *preferred_buffer,
                                           const size_t &preferred_buffer_size,
                                           size_t &final_size
                                          )
{
  size_t length = 0;
  bool is_negative = 0;
  int_to_string_non_zero_helper2<base>(n, symbols, minimum_size, preferred_buffer, preferred_buffer_size, final_size, length, is_negative);
  bool freeit = 0;
  char *buffer;

  // Calculate length.
  for (T copy = n; copy; copy /= base)
    length++;
  if (length < minimum_size)
    length = minimum_size;
  final_size = length;
  if (final_size < preferred_buffer_size)
    buffer = preferred_buffer;
  else
  {
    buffer = new (std::nothrow) char[final_size + 1];
    if (!buffer)
    {
      THROW_CIPC_OUTOFMEMORY;
    }
    freeit = 1;
  }
  buffer[length] = 0;
  if (is_negative)
    buffer[0] = '-';
  size_t iterations = length - (size_t)is_negative;
  for (size_t i = 0; i < iterations; i++)
  {
    buffer[length - 1 - i] = symbols[n % base];
    n /= base;
  }
  return !freeit ? 0 : buffer;
}

template <unsigned base, typename T>
char *int_to_string(
                    T n,
                    const char *symbols,
                    size_t minimum_size,
                    char *preferred_buffer,
                    const size_t &preferred_buffer_size,
                    size_t &final_size
                   )
{
  final_size = 0;

  if (!n)
    return int_to_string_zero_helper<base>(n, symbols, minimum_size, preferred_buffer, preferred_buffer_size, final_size);
  return int_to_string_non_zero_helper<base>(n, symbols, minimum_size, preferred_buffer, preferred_buffer_size, final_size);
}
