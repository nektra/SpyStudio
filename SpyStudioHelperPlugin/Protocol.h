#pragma once

#include <exception>
#include <cassert>
#include <map>
#include <Windows.h>
#include "Main.h"
#include "TypeDeclarations.h"
#include "CommonFunctions.h"
#include "MessageCodes.h"

//#define DETAILED_LOG

class CallEventSerializer;

HRESULT perform_writes(CallEventSerializer &ces, INktHookInfo &hi, INktHookCallInfoPlugin &hcip);
