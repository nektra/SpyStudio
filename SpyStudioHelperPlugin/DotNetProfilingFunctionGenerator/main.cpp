#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include "../tinyxml2.h"
#include "function.h"

#define NAME "GeneratedDotNetCallbacks"

int main(){
  DotNetProfiling dnp("DotNetProfilingFunctions.xml");
  std::ofstream GeneratedDotNetCallbacks_h(NAME ".h");
  std::ofstream GeneratedDotNetCallbacks_cpp(NAME ".cpp");
  std::ofstream GeneratedMemberDeclarations_inl("GeneratedMemberDeclarations.inl");
  std::ofstream GeneratedStaticFunctions_inl("GeneratedStaticFunctions.inl");
  std::ofstream GeneratedArrayElements_inl("GeneratedArrayElements.inl");
  dnp.generate_declarations(GeneratedDotNetCallbacks_h, NAME);
  dnp.generate_definitions(GeneratedDotNetCallbacks_cpp, NAME);
  dnp.generate_member_declarations(GeneratedMemberDeclarations_inl);
  dnp.generate_static_functions(GeneratedStaticFunctions_inl);
  dnp.generate_array_elements(GeneratedArrayElements_inl);
  //tinyxml2::XMLDocument doc;
  //dnp.to_xml(doc);
  //doc.SaveFile("DotNetProfilingFunctions.new.xml");
}
