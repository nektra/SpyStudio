#ifndef SECONDARY_HOOKS_LAMBDA_N
#error Please define SECONDARY_HOOKS_LAMBDA_N before including this header.
#endif

#ifndef SECONDARY_HOOKS_LAMBDA_0
#define SECONDARY_HOOKS_LAMBDA_0 SECONDARY_HOOKS_LAMBDA_N
#define UNDEFINE_LAMBDA_0
#endif

SECONDARY_HOOKS_LAMBDA_0(NtOpenKey)
SECONDARY_HOOKS_LAMBDA_N(NtOpenKeyEx)
SECONDARY_HOOKS_LAMBDA_N(NtCreateKey)
SECONDARY_HOOKS_LAMBDA_N(NtQueryKey)
SECONDARY_HOOKS_LAMBDA_N(NtQueryValueKey)
SECONDARY_HOOKS_LAMBDA_N(NtQueryMultipleValueKey)
SECONDARY_HOOKS_LAMBDA_N(NtSetValueKey)
SECONDARY_HOOKS_LAMBDA_N(NtDeleteKey)
SECONDARY_HOOKS_LAMBDA_N(NtDeleteValueKey)
SECONDARY_HOOKS_LAMBDA_N(NtEnumerateKey)
SECONDARY_HOOKS_LAMBDA_N(NtEnumerateValueKey)
SECONDARY_HOOKS_LAMBDA_N(NtRenameKey)
SECONDARY_HOOKS_LAMBDA_N(NtCreateFile)
SECONDARY_HOOKS_LAMBDA_N(NtOpenFile)
SECONDARY_HOOKS_LAMBDA_N(NtDeleteFile)
SECONDARY_HOOKS_LAMBDA_N(NtQueryDirectoryFile)
SECONDARY_HOOKS_LAMBDA_N(NtQueryAttributesFile)

#ifdef UNDEFINE_LAMBDA_0
#undef SECONDARY_HOOKS_LAMBDA_0
#undef UNDEFINE_LAMBDA_0
#endif
