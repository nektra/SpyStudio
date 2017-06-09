#include <string>
#include <vector>
#include <map>

class SymbolicFormat{
  struct section{
    bool is_literal;
    std::string value;
  };
  std::vector<section> sections;
public:
  SymbolicFormat(const std::string &format);
  typedef std::map<std::string, std::string> map_t;
  std::string operator%(const map_t &) const;
};
