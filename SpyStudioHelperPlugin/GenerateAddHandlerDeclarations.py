list = ['BOOL',
        'VOID',
        'INT',
        'UINT',
        'HEX',
        'HRESULT',
        'NTSTATUS']
declarations = open('AddHandlerDeclarations.inl', 'w')
initializations = open('AddHandlerInitializations.inl', 'w')
estring = ''

s = '//This file was generated by GenerateAddHandlerDeclarations.py\n'\
    '//Do not edit it.\n'

declarations.write(s)
initializations.write(s)

for i in list:
    declarations.write('void Add%s(INktHookCallInfoPlugin &hcip);\n'%i)
    initializations.write(
'%sif (!strcmp(s, "%s"))\n\
{\n\
  result_handler = &CustomHookCallCES::Add%s;\n\
}\n'%(estring, i, i))
    estring = 'else '