#pragma once

#include <iostream>
#include <string>
#include <vector>
#include <map>
#include <memory>
#include "../tinyxml2.h"

enum class PrePostStatus{
  Before = 0,
  After,
  Both,
};

class Function{
  std::string name;
  bool unused,
    no_member_definition;
  struct Param{
    std::string type;
    std::string name;
    bool is_result;
    bool unused;
    bool hex;
    Param(): is_result(0), unused(0), hex(0){}
    Param(tinyxml2::XMLElement *el);
    tinyxml2::XMLElement *to_xml(tinyxml2::XMLDocument &doc) const;
  };
  std::vector<Param> params;
  PrePostStatus prepost_status;
  static struct Map{
    std::map<std::string, void (Function::*)(tinyxml2::XMLElement *)> element_handlers;
    Map();
  } map;
  std::string linker_id;
  std::string call_identifier;
  struct Special{
    std::string name;
    std::vector<std::string> params;
    Special(tinyxml2::XMLElement *el);
    tinyxml2::XMLElement *to_xml(tinyxml2::XMLDocument &doc);
  };
  std::vector<std::shared_ptr<Special> > specials;
  std::string insert;

  void handle_params(tinyxml2::XMLElement *);
  void handle_before(tinyxml2::XMLElement *);
  void handle_after(tinyxml2::XMLElement *);
  void handle_specials(tinyxml2::XMLElement *);
  void handle_insert(tinyxml2::XMLElement *);
public:
  Function(){}
  Function(tinyxml2::XMLElement *el);
  const std::string &get_name() const{
    return this->name;
  }
  void set_prepost_status(PrePostStatus status){
    this->prepost_status = status;
  }
  void set_linker_id(const std::string &id){
    this->linker_id = id;
  }
  tinyxml2::XMLElement *to_xml(tinyxml2::XMLDocument &doc) const;
  bool get_no_member_definition() const{
    return this->no_member_definition;
  }
  void generate_declarations(std::ostream &);
  void generate_definitions(std::ostream &);
  void generate_member_declarations(std::ostream &);
  void generate_static_functions(std::ostream &);
  void generate_array_elements(std::ostream &);
};

class Functions{
  std::vector<std::shared_ptr<Function> > list;
  //std::vector<boost::shared_ptr<Function> > functions;
  std::shared_ptr<Function> find_function(const std::string &name);
public:
  Functions(tinyxml2::XMLElement *);
  void process_linker_ids(std::vector<std::string> &linker_ids);
  void process_function_ids(std::vector<std::string> &function_ids);
  tinyxml2::XMLElement *to_xml(tinyxml2::XMLDocument &doc) const;
  void generate_declarations(std::ostream &);
  void generate_definitions(std::ostream &);
  void generate_member_declarations(std::ostream &);
  void generate_static_functions(std::ostream &);
  void generate_array_elements(std::ostream &);
};

class LinkerIds{
  std::vector<std::string> linker_ids;
public:
  LinkerIds(tinyxml2::XMLElement *);
  LinkerIds(const std::vector<std::string> &linker_ids): linker_ids(linker_ids){}
  tinyxml2::XMLElement *to_xml(tinyxml2::XMLDocument &doc) const;
  void generate_declarations(std::ostream &);
  void generate_definitions(std::ostream &);
};

class FunctionIds{
  std::vector<std::string> function_ids;
public:
  FunctionIds(tinyxml2::XMLElement *);
  FunctionIds(const std::vector<std::string> &function_ids): function_ids(function_ids){}
  tinyxml2::XMLElement *to_xml(tinyxml2::XMLDocument &doc) const;
  void generate_declarations(std::ostream &);
  void generate_definitions(std::ostream &);
};

class DotNetProfiling{
  std::shared_ptr<LinkerIds> linker_ids;
  std::shared_ptr<FunctionIds> function_ids;
  std::shared_ptr<Functions> functions;
public:
  DotNetProfiling(const char *path);
  void to_xml(tinyxml2::XMLDocument &doc) const;
  void generate_declarations(std::ostream &, const char *target_name);
  void generate_definitions(std::ostream &, const char *target_name);
  void generate_member_declarations(std::ostream &);
  void generate_static_functions(std::ostream &);
  void generate_array_elements(std::ostream &);
};
