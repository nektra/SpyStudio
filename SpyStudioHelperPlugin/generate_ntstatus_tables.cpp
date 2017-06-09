#define NO_MAIN
#include "generate_hresult_tables.cpp"

int main(){
	generate("ntstatus");
	return 0;
}
