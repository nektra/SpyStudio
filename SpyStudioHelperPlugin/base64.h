#pragma once
#ifndef BASE64_CENCODE_H
#define BASE64_CENCODE_H

template <unsigned CHARS_PER_LINE = 72>
class base64_encoder
{
  enum base64_encodestep
  {
    step_A,
    step_B,
    step_C,
  };
  base64_encodestep step;
  unsigned char result;
  unsigned stepcount;

  static char base64_encode_value(unsigned char value_in)
  {
    static const char *encoding = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    if (value_in > 63)
      return '=';
    return encoding[(size_t)value_in];
  }
public:
  base64_encoder()
  {
    this->step = step_A;
    this->result = 0;
    this->stepcount = 0;
  }
  template <typename F>
  size_t input_block(const void *plaintext_in, size_t length_in, F &output_function)
  {
    auto plainchar = (const unsigned char *)plaintext_in;
    auto plaintextend = plainchar + length_in;
    auto initial = output_function();
    unsigned char result,
      fragment;
  
    result = this->result;
  
    switch (this->step)
    {
      while (1)
      {
        case step_A:
          if (plainchar == plaintextend)
          {
            this->result = result;
            this->step = step_A;
            return initial - output_function();
          }
          fragment = *plainchar++;
          result = (fragment & 0x0fc) >> 2;
          output_function(base64_encode_value(result));
          result = (fragment & 0x003) << 4;
        case step_B:
          if (plainchar == plaintextend)
          {
            this->result = result;
            this->step = step_B;
            return initial - output_function();
          }
          fragment = *plainchar++;
          result |= (fragment & 0x0f0) >> 4;
          output_function(base64_encode_value(result));
          result = (fragment & 0x00f) << 2;
        case step_C:
          if (plainchar == plaintextend)
          {
            this->result = result;
            this->step = step_C;
            return initial - output_function();
          }
          fragment = *plainchar++;
          result |= (fragment & 0x0c0) >> 6;
          output_function(base64_encode_value(result));
          result  = (fragment & 0x03f) >> 0;
          output_function(base64_encode_value(result));
        
          if (CHARS_PER_LINE > 0)
          {
            this->stepcount++;
            if (this->stepcount == CHARS_PER_LINE/4)
            {
              output_function('\n');
              this->stepcount = 0;
            }
          }
      }
    }
    /* control should not reach here */
    return initial - output_function();
  }
  template <typename F>
  size_t input_end(F &output_function)
  {
    auto initial = output_function();
  
    switch (this->step)
    {
      case step_B:
        output_function(base64_encode_value(this->result));
        output_function('=');
        output_function('=');
        break;
      case step_C:
        output_function(base64_encode_value(this->result));
        output_function('=');
        break;
      case step_A:
        break;
    }
    if (CHARS_PER_LINE > 0)
      output_function('\n');
  
    return initial - output_function();
  }
  template <typename F>
  static size_t encode(const void *buffer, size_t n, F &output_function)
  {
    size_t ret = 0;
    base64_encoder<CHARS_PER_LINE> encoder;
    ret += encoder.input_block(buffer, n, output_function);
    ret += encoder.input_end(output_function);
    return ret;
  }
};

#endif /* BASE64_CENCODE_H */
