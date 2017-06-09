#include <iostream>
#include <iomanip>
#include <fstream>
#include <vector>
#include <string>
#include <algorithm>

struct NTSTATUS_map{
	std::string name;
	unsigned value;
	NTSTATUS_map(const std::string &name, unsigned value): name(name), value(value){}
	bool operator<(const NTSTATUS_map &b) const{
		return this->value < b.value;
	}
};

struct NTSTATUS_block{
	unsigned first_id,
		first_index,
		block_length;
	std::vector<std::string> strings;
	bool can_be_appended(unsigned id){
		return id == this->first_id + this->block_length;
	}
	void append(const NTSTATUS_map &map){
		this->block_length++;
		this->strings.push_back(map.name);
	}
};

void generate(const std::string &name){
	std::string lower_case = name;
	std::string upper_case = name;
	for (size_t i = 0; i < upper_case.size(); i++){
		lower_case[i] = tolower(lower_case[i]);
		upper_case[i] = toupper(upper_case[i]);
	}
	
	std::ifstream file((lower_case + ".txt").c_str());
	std::vector<NTSTATUS_map> maps;
	while (file.good()){
		std::string ntstatus_string;
		unsigned ntstatus_id;
		file >> ntstatus_string >> std::hex >> ntstatus_id;
		if (!ntstatus_string.size())
			break;
		maps.push_back(NTSTATUS_map(ntstatus_string, ntstatus_id));
	}
	if (!maps.size())
		return;
	std::sort(maps.begin(), maps.end());
	std::vector<NTSTATUS_block> blocks;
	blocks.push_back(NTSTATUS_block());
	blocks.front().first_id = 0;
	blocks.front().first_index = 0;
	blocks.front().block_length = 0;
	for (size_t i = 0; i < maps.size(); i++){
		if (!blocks.back().can_be_appended(maps[i].value)){
			blocks.push_back(NTSTATUS_block());
			blocks.back().first_id = maps[i].value;
			blocks.back().first_index = i;
			blocks.back().block_length = 0;
		}
		blocks.back().append(maps[i]);
	}
	
	std::ofstream output((lower_case + "_table.inl").c_str());
	output << "static const char *" << upper_case << "_strings[] =\n"
	          "{\n";
	for (size_t i = 0; i < blocks.size(); i++){
		for (size_t j = 0; j < blocks[i].strings.size(); j++){
			output << "\t\"" << blocks[i].strings[j] << "\",\n";
		}
	}
	
	output << "};\n"
	          "\n"
	          "static interval intervals[] = {\n";
	
	for (size_t i = 0; i < blocks.size(); i++){
		output << "\t{ 0x" <<std::hex<<std::setw(8)<<std::setfill('0') << blocks[i].first_id <<", "
		       <<std::dec << blocks[i].block_length << ", " << blocks[i].first_index << " },\n";
	}
	output << "};\n";
}

#ifndef NO_MAIN
int main(){
	generate("hresult");
	return 0;
}
#endif
