#include "SymbolicFormat.h"

SymbolicFormat::SymbolicFormat(const std::string &format){
  bool state = 0;
  std::string string;
  for (auto c : format){
    if (c == '%'){
      if (!state){
        if (string.size()){
          section s = {
            1,
            string,
          };
          this->sections.push_back(s);
          string.clear();
        }
      }else{
        section s;
        if (!string.size()){
          s.is_literal = 1;
          s.value = "%";
        }else{
          s.is_literal = 0;
          s.value = string;
          string.clear();
        }
        this->sections.push_back(s);
      }
      state = !state;
    }else
      string.push_back(c);
  }
  if (string.size()){
    section s = {
      !state,
      string,
    };
    this->sections.push_back(s);
  }
}

std::string SymbolicFormat::operator%(const std::map<std::string, std::string> &map) const{
  std::string ret;
  for (const auto &s : this->sections){
    if (s.is_literal)
      ret += s.value;
    else{
      auto it = map.find(s.value);
      if (it != map.end())
        ret += it->second;
    }
  }
  return ret;
}
