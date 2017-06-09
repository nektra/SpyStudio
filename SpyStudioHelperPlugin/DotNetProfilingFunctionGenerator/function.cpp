#include "function.h"
#include "SymbolicFormat.h"
#include <fstream>
#include <sstream>

#define CANT_HANDLE(x, y) std::cerr <<"WARNING: " x " doesn't know how to handle: "<< y <<std::endl
#define IGNORE_BEFORE_FUNCTIONS

Function::Param::Param(tinyxml2::XMLElement * const param):
    is_result(0),
    unused(0),
    hex(0){
  bool result_unset = 1;
  for (auto child = param->FirstAttribute(); child; child = child->Next()){
    if (!strcmp(child->Name(), "type"))
      this->type = child->Value();
    else if (!strcmp(child->Name(), "name"))
      this->name = child->Value();
    else if (!strcmp(child->Name(), "is_result")){
      this->is_result = child->BoolValue();
      result_unset = 0;
    }else if (!strcmp(child->Name(), "unused"))
      this->unused = child->BoolValue();
    else if (!strcmp(child->Name(), "hex"))
      this->hex = child->BoolValue();
    else{
      CANT_HANDLE(__FUNCTION__, child->Name());
    }
  }
  if (result_unset && this->type == "HRESULT")
    this->is_result = 1;
}

tinyxml2::XMLElement *Function::Param::to_xml(tinyxml2::XMLDocument &doc) const{
  auto ret = doc.NewElement("param");
  ret->SetAttribute("type", this->type.c_str());
  ret->SetAttribute("name", this->name.c_str());
  if (this->is_result)
    ret->SetAttribute("is_result", this->is_result);
  if (this->unused)
    ret->SetAttribute("unused", this->unused);
  return ret;
}

Function::Special::Special(tinyxml2::XMLElement *el){
  auto name = el->Attribute("name");
  if (name)
    this->name = name;
  for (auto child = el->FirstChildElement(); child; child = child->NextSiblingElement()){
    if (!strcmp(child->Name(), "param")){
      name = child->Attribute("name");
      if (name)
        this->params.push_back(name);
    }else{
      CANT_HANDLE(__FUNCTION__, child->Name());
    }
  }
}

tinyxml2::XMLElement *Function::Special::to_xml(tinyxml2::XMLDocument &doc){
  auto ret = doc.NewElement("special");
  ret->SetAttribute("name", this->name.c_str());
  for (auto &s : this->params){
    auto param = doc.NewElement("param");
    param->SetAttribute("name", s.c_str());
    ret->LinkEndChild(param);
  }
  return ret;
}

Function::Map Function::map;

Function::Map::Map(){
  this->element_handlers["params"] = &Function::handle_params;
  this->element_handlers["before"] = &Function::handle_before;
  this->element_handlers["after"] = &Function::handle_after;
  this->element_handlers["specials"] = &Function::handle_specials;
  this->element_handlers["insert"] = &Function::handle_insert;
}

Function::Function(tinyxml2::XMLElement * const el):
    prepost_status(PrePostStatus::Both),
    unused(0),
    no_member_definition(0){
  this->name = el->Attribute("name");
  this->unused = el->BoolAttribute("unused");
  this->no_member_definition = el->BoolAttribute("no_member_definition");

  const auto &element_handlers = this->map.element_handlers;

  for (auto child = el->FirstChildElement(); child; child = child->NextSiblingElement()){
    auto it = element_handlers.find(child->Name());
    if (it == element_handlers.end()){
      CANT_HANDLE(__FUNCTION__, child->Name());
      continue;
    }
    (this->*(it->second))(child);
  }
}

void Function::handle_params(tinyxml2::XMLElement * const param){
  for (auto child = param->FirstChildElement(); child; child = child->NextSiblingElement()){
    if (!strcmp(child->Name(), "param")){
      this->params.push_back(Param(child));
    }else{
      CANT_HANDLE(__FUNCTION__, child->Name());
    }
  }
}

void Function::handle_before(tinyxml2::XMLElement * const before){
  this->prepost_status = PrePostStatus::Before;

  auto value = before->Attribute("linker_id");
  if (value)
    this->linker_id = value;

  value = before->Attribute("id");
  if (value)
    this->call_identifier = value;
}

void Function::handle_after(tinyxml2::XMLElement * const after){
  this->prepost_status = PrePostStatus::After;
  auto value = after->Attribute("linker_id");
  if (value)
    this->linker_id = value;

  value = after->Attribute("id");
  if (value)
    this->call_identifier = value;
}

void Function::handle_specials(tinyxml2::XMLElement * const specials){
  for (auto child = specials->FirstChildElement(); child; child = child->NextSiblingElement()){
    if (!strcmp(child->Name(), "special"))
      this->specials.push_back(std::shared_ptr<Special>(new Special(child)));
    else{
      CANT_HANDLE(__FUNCTION__, child->Name());
    }
  }
}

void Function::handle_insert(tinyxml2::XMLElement * const insert){
  this->insert = insert->GetText();
}

tinyxml2::XMLElement *Function::to_xml(tinyxml2::XMLDocument &doc) const{
  auto ret = doc.NewElement("function");
  ret->SetAttribute("name", this->name.c_str());
  if (this->unused)
    ret->SetAttribute("unused", this->unused);

  {
    auto params = doc.NewElement("params");
    ret->LinkEndChild(params);
    for (auto &param : this->params)
      params->LinkEndChild(param.to_xml(doc));
  }

  {
    tinyxml2::XMLElement *temp = nullptr;
    switch (this->prepost_status){
      case PrePostStatus::Both:
        break;
      case PrePostStatus::Before:
        temp = doc.NewElement("before");
        break;
      case PrePostStatus::After:
        temp = doc.NewElement("after");
        break;
    }
    if (!!temp){
      temp->SetAttribute("linker_id", this->linker_id.c_str());
      temp->SetAttribute("id", this->call_identifier.c_str());
      ret->LinkEndChild(temp);
    }
  }

  {
    auto specials = doc.NewElement("specials");
    ret->LinkEndChild(specials);
    for (auto &s : this->specials)
      specials->LinkEndChild(s->to_xml(doc));
  }

  return ret;
}

Functions::Functions(tinyxml2::XMLElement * const functions){
  for (auto el = functions->FirstChildElement(); el; el = el->NextSiblingElement()){
    if (!strcmp(el->Name(), "function")){
      std::shared_ptr<Function> f(new Function(el));
      this->list.push_back(f);
      //this->functions[f->get_name()] = f;
    }
  }
}

std::shared_ptr<Function> Functions::find_function(const std::string &name){
  for (auto &f : this->list)
    if (f->get_name() == name)
      return f;
  return std::shared_ptr<Function>();
}

void Functions::process_linker_ids(std::vector<std::string> &linker_ids){
  linker_ids.clear();

  for (auto &f : this->list){
    const auto &name = f->get_name();
    static const char Started[] = "Started";
    static const size_t Started_size = sizeof(Started)/sizeof(*Started) - 1;
    const size_t expected_position = name.size() - Started_size;

    if (name.rfind(Started) != expected_position)
      continue;

    std::string trimmed_name = name.substr(0, expected_position);
    std::string new_name = trimmed_name + "Finished";
    auto it = this->find_function(new_name);
    if (!it.get())
      continue;

    linker_ids.push_back(trimmed_name);
    f->set_prepost_status(PrePostStatus::Before);
    f->set_linker_id(trimmed_name);
    it->set_prepost_status(PrePostStatus::After);
    it->set_linker_id(trimmed_name);
  }
}

void Functions::process_function_ids(std::vector<std::string> &function_ids){
  function_ids.clear();
  for (auto &f : this->list)
    function_ids.push_back(f->get_name());
}

tinyxml2::XMLElement *Functions::to_xml(tinyxml2::XMLDocument &doc) const{
  auto ret = doc.NewElement("functions");
  for (auto &f : this->list)
    ret->LinkEndChild(f->to_xml(doc));
  return ret;
}

LinkerIds::LinkerIds(tinyxml2::XMLElement * const linker_ids){
  for (auto el = linker_ids->FirstChildElement(); el; el = el->NextSiblingElement()){
    if (!strcmp(el->Name(), "linker_id")){
      auto name = el->Attribute("name");
      if (!name)
        continue;
      this->linker_ids.push_back(name);
    }
  }
}

tinyxml2::XMLElement *LinkerIds::to_xml(tinyxml2::XMLDocument &doc) const{
  auto ret = doc.NewElement("linker_ids");
  for (auto &id : this->linker_ids){
    auto node = doc.NewElement("linker_id");
    node->SetAttribute("name", id.c_str());
    ret->LinkEndChild(node);
  }
  return ret;
}

FunctionIds::FunctionIds(tinyxml2::XMLElement *function_ids){
  for (auto el = function_ids->FirstChildElement(); el; el = el->NextSiblingElement()){
    if (!strcmp(el->Name(), "function_id")){
      auto name = el->Attribute("name");
      if (!name)
        continue;
      this->function_ids.push_back(name);
    }
  }
}

tinyxml2::XMLElement *FunctionIds::to_xml(tinyxml2::XMLDocument &doc) const{
  auto ret = doc.NewElement("function_ids");
  for (auto &id : this->function_ids){
    auto node = doc.NewElement("function_id");
    node->SetAttribute("name", id.c_str());
    ret->LinkEndChild(node);
  }
  return ret;
}

std::string itoa(int i){
  std::stringstream s;
  s <<i;
  return s.str();
}

DotNetProfiling::DotNetProfiling(const char *path){
  tinyxml2::XMLDocument doc;
  doc.LoadFile(path);

  tinyxml2::XMLElement *main_node = nullptr;
  for (auto el = doc.FirstChildElement(); el; el = el->NextSiblingElement()){
    if (!strcmp(el->Name(), "DotNetProfiling")){
      main_node = el;
      break;
    }
  }
  if (!main_node)
    return;

  for (auto el = main_node->FirstChildElement(); el; el = el->NextSiblingElement()){
    if (!strcmp(el->Name(), "functions")){
      this->functions.reset(new Functions(el));
    }else if (!strcmp(el->Name(), "function_ids")){
      this->function_ids.reset(new FunctionIds(el));
    }else if (!strcmp(el->Name(), "linker_ids")){
      this->linker_ids.reset(new LinkerIds(el));
    }
  }
  if (!!this->functions.get()){
    if (!this->linker_ids.get()){
      std::vector<std::string> temp;
      this->functions->process_linker_ids(temp);
      this->linker_ids.reset(new LinkerIds(temp));
    }
    if (!this->function_ids.get()){
      std::vector<std::string> temp;
      this->functions->process_function_ids(temp);
      this->function_ids.reset(new FunctionIds(temp));
    }
  }
}

void DotNetProfiling::to_xml(tinyxml2::XMLDocument &doc) const{
  auto ret = doc.NewElement("DotNetProfiling");
  doc.LinkEndChild(ret);
  if (this->function_ids.get())
    ret->LinkEndChild(this->function_ids->to_xml(doc));
  if (this->linker_ids.get())
    ret->LinkEndChild(this->linker_ids->to_xml(doc));
  if (this->functions.get())
    ret->LinkEndChild(this->functions->to_xml(doc));
}

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                            CODE GENERATION                                 //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

static std::string load_format(const char *filename){
  std::ifstream file(filename);
  typedef std::istreambuf_iterator<char> it_t;
  return std::string(it_t(file), it_t());
}

void DotNetProfiling::generate_declarations(std::ostream &stream, const char *target_name){
  stream <<
    "#pragma once\n"
    "\n"
    "// This section is automatically generated!\n"
    "\n";
  if (this->function_ids.get()){
    this->function_ids->generate_declarations(stream);
    stream <<std::endl;
  }
  if (this->linker_ids.get()){
    this->linker_ids->generate_declarations(stream);
    stream <<std::endl;
  }
  if (this->functions.get())
    this->functions->generate_declarations(stream);
  stream <<"// End of automatically generated section.\n";
}

void DotNetProfiling::generate_member_declarations(std::ostream &stream){
  stream <<
    "// This section is automatically generated!\n"
    "\n";
  if (this->functions.get())
    this->functions->generate_member_declarations(stream);
  stream <<"\n"
    "// End of automatically generated section.\n";
}

void DotNetProfiling::generate_static_functions(std::ostream &stream){
  stream <<
    "// This section is automatically generated!\n"
    "\n";
  if (this->functions.get())
    this->functions->generate_static_functions(stream);
  stream <<"// End of automatically generated section.\n";
}

void DotNetProfiling::generate_array_elements(std::ostream &stream){
  stream <<
    "// This section is automatically generated!\n"
    "\n";
  if (this->functions.get())
    this->functions->generate_array_elements(stream);
  stream <<"\n"
    "// End of automatically generated section.\n";
}

void DotNetProfiling::generate_definitions(std::ostream &stream, const char *target_name){
  stream <<
    "// This section is automatically generated!\n\n"
    "\n"
    "#include \"stdafx.h\"\n"
    "#include \""<<target_name<<".h\"\n"
    "#include \"DotNetProfiler.h\"\n"
    "#include \"DotNetProfilerMgr.h\"\n"
    "#include \"CIPC.h\"\n"
    "#include \"Buffer.h\"\n"
    "\n";
  if (this->function_ids.get()){
    this->function_ids->generate_definitions(stream);
    stream <<std::endl;
  }
  if (this->linker_ids.get()){
    this->linker_ids->generate_definitions(stream);
    stream <<std::endl;
  }
  if (this->functions.get())
    this->functions->generate_definitions(stream);
  stream <<"// End of automatically generated section.\n";
}

std::string generate_enum_lines(const SymbolicFormat &format, const std::vector<std::string> &lines){
  SymbolicFormat::map_t map;
  int i = 0;
  std::string ret;
  for (auto &s : lines){
    map["name"] = s;
    map["value"] = itoa(i++);
    ret += format % map;
  }
  map["name"] = "_Count";
  map["value"] = itoa(i);
  ret += format % map;
  return ret;
}

void LinkerIds::generate_declarations(std::ostream &stream){
  SymbolicFormat::map_t map;
  static SymbolicFormat enum_format = load_format("formats/Enum.txt");
  static SymbolicFormat line_format = load_format("formats/EnumLine.txt");

  map["name"] = "LinkerId";
  map["enums"] = generate_enum_lines(line_format, this->linker_ids);
  stream << (enum_format % map);
}

void LinkerIds::generate_definitions(std::ostream &stream){
}

void FunctionIds::generate_declarations(std::ostream &stream){
  SymbolicFormat::map_t map;
  static SymbolicFormat enum_format = load_format("formats/Enum.txt");
  static SymbolicFormat line_format = load_format("formats/EnumLine.txt");

  map["name"] = "FunctionId";
  map["enums"] = generate_enum_lines(line_format, this->function_ids);
  stream << (enum_format % map);
}

void FunctionIds::generate_definitions(std::ostream &stream){
}

void Function::generate_declarations(std::ostream &stream){
}

void Function::generate_definitions(std::ostream &stream){
  static SymbolicFormat function_format = load_format("formats/Function.txt");
  static SymbolicFormat unused_function_format = load_format("formats/UnusedFunction.txt");
  static SymbolicFormat param_format = load_format("formats/Parameter.txt");

  std::map<std::string, std::string> map;
  map["name"] = this->name;
  {
    std::string params;
    for (auto &param : this->params){
      map["param_type"] = param.type;
      map["param_name"] = param.name;
      params += param_format % map;
    }
    map["params"] = params;
  }
  if (this->unused || this->prepost_status == PrePostStatus::Before)
    stream << (unused_function_format % map);
  else{
    map["function_id"] = "(unsigned)FunctionId::" + this->name;
    map["elapsed_time"] = "0.0";
    map["function_name"] = this->prepost_status == PrePostStatus::Both ? this->name : this->linker_id;
#ifndef IGNORE_BEFORE_FUNCTIONS
    switch (this->prepost_status){
      case PrePostStatus::Both:
        map["event_kind"] = "4";
        map["call_status"] = "0";
        break;
      case PrePostStatus::Before:
        map["event_kind"] = "1";
        map["call_status"] = "-1";
        break;
      case PrePostStatus::After:
        map["event_kind"] = "0";
        map["call_status"] = "1";
        break;
    }
#else
    map["event_kind"] = "4";
    map["call_status"] = "0";
#endif
#ifndef IGNORE_BEFORE_FUNCTIONS
    if (this->prepost_status != PrePostStatus::Both){
      map["linker_id"] = "LinkerId::" + this->linker_id;
      map["call_identifier"] = this->call_identifier;
    }else
#endif
      map["linker_id"] = "(LinkerId)0";
#ifndef IGNORE_BEFORE_FUNCTIONS
    map["add_stack"] = this->prepost_status != PrePostStatus::After ? "1" : "0";
#else
    map["add_stack"] = "1";
#endif
    {
      Param *result = nullptr;
      for (auto &p : this->params){
        if (p.is_result){
          result = &p;
          break;
        }
      }
      if (!!result){
        map["add_result"] = "    mgr.add_result(buffer, " + result->name + ");\n";
      }
    }
    {
      std::string add_params;
      bool any = 0;
      for (auto &p : this->params){
        if (p.unused || p.is_result)
          continue;
        add_params += "    mgr.add_param_" + p.type + "(buffer, \"" + p.name + "\", " + p.name + ");\n";
        any = 1;
      }
      map["add_params"] = (any ? "    buffer.AddString(\"params\");\n" : "") + add_params;
    }
    {
      std::string special;
      for (auto &s : this->specials){
        special += "    mgr.add_param_" + s->name + "(buffer, \"" + s->name + "\"";
        for (auto &p : s->params){
          special += ", ";
          special += p;
        }
        special += ");\n";
      }
      map["special"] = special;
    }
    map["insert"] = this->insert;
    stream << (function_format % map);
  }
}

void Function::generate_member_declarations(std::ostream &stream){
  static SymbolicFormat member_declaration = load_format("formats/MemberDeclaration.txt");
  static SymbolicFormat param_format = load_format("formats/Parameter.txt");

  std::map<std::string, std::string> map;
  map["name"] = this->name;
  {
    std::string params;
    for (auto &param : this->params){
      map["param_type"] = param.type;
      map["param_name"] = param.name;
      params += param_format % map;
    }
    map["params"] = params;
  }
  stream << (member_declaration % map);
}

void Function::generate_static_functions(std::ostream &stream){
  static SymbolicFormat static_function = load_format("formats/StaticFunction.txt");
  static SymbolicFormat param_format = load_format("formats/Parameter.txt");

  std::map<std::string, std::string> map;
  map["name"] = this->name;
  {
    std::string params;
    std::string call_params;
    for (auto &param : this->params){
      map["param_type"] = param.type;
      map["param_name"] = param.name;
      params += param_format % map;
      call_params += ", " + param.name;
    }
    map["params"] = params;
    map["call_params"] = call_params;
  }
  stream << (static_function % map);
}

void Function::generate_array_elements(std::ostream &stream){
  static SymbolicFormat array_element = load_format("formats/ArrayElement.txt");

  std::map<std::string, std::string> map;
  map["name"] = this->name;
  std::stringstream temp;
  temp << this->params.size();
  temp >> map["count"];
  stream << (array_element % map);
}

void Functions::generate_declarations(std::ostream &stream){
}

void Functions::generate_definitions(std::ostream &stream){
  for (auto &f : this->list){
    if (f->get_no_member_definition())
      continue;
    f->generate_definitions(stream);
    stream <<std::endl;
  }
}

void Functions::generate_member_declarations(std::ostream &stream){
  for (auto &f : this->list){
    f->generate_member_declarations(stream);
    stream <<std::endl;
  }
}

void Functions::generate_static_functions(std::ostream &stream){
  for (auto &f : this->list){
    f->generate_static_functions(stream);
    stream <<std::endl;
  }
}

void Functions::generate_array_elements(std::ostream &stream){
  for (auto &f : this->list){
    f->generate_array_elements(stream);
    stream <<std::endl;
  }
}
