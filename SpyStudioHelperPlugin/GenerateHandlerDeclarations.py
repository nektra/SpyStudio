list = ['HWND',
        'HMODULE',
        'HKEY',
        'HFILE',
        'IID',
        'CLASSNAME',
        'ADDRESS',
        'HTHREAD',
        'HPROCESS',
        'LPINTERNET_BUFFERS',
        'FILE_POBJECT_ATTRIBUTES',
        'PUNICODE_STRING',
        'PANSI_STRING',
        'NTSTATUS']
declarations = open('HandlerDeclarations.inl', 'w')
initializations = open('HandlerInitializations.inl', 'w')
definitions = open('HandlerDefinitions.txt', 'w')

s = '//This file was generated by GenerateHandlerDeclarations.py\n'\
    '//Do not edit it.\n'

declarations.write(s)
initializations.write(s)
definitions.write(s)

estring = ''
for i in list:
    declarations.write('bool Handle%s(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip);\n'%i)
    initializations.write(
'%sif (!strcmp(context, "%s"))\n\
{\n\
  param_handler = &CustomHookCallCES::Handle%s;\n\
}\n'%(estring, i, i))
    estring = 'else '
    definitions.write('bool CustomHookCallCES::Handle%s(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)\n{\n}\n\n'%i)
