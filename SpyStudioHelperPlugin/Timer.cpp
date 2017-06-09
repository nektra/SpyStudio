#include "stdafx.h"
#include "Timer.h"

double HighDefinitionTimer::_freq = 0;

HighDefinitionTimer::HighDefinitionTimer()
{
	LARGE_INTEGER li;
	if(_freq == 0)
	{
		if(!QueryPerformanceFrequency(&li))
			OutputDebugString(_T("QueryPerformanceFrequency failed!\n"));

	    _freq = double(li.QuadPart)/1000.0;
	}

    QueryPerformanceCounter(&li);
    _counterStart = li.QuadPart;
}

HighDefinitionTimer::~HighDefinitionTimer()
{
}

double HighDefinitionTimer::GetEllapsed()
{
	LARGE_INTEGER li;
    QueryPerformanceCounter(&li);
    return double(li.QuadPart-_counterStart)/_freq;
}
