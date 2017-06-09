#ifndef _TIMER_H
#define _TIMER_H

class HighDefinitionTimer
{
public:
	HighDefinitionTimer();
	~HighDefinitionTimer();
	double GetEllapsed();

private:
	static double _freq;
	__int64 _counterStart;
};


#endif //_TIMER_H
