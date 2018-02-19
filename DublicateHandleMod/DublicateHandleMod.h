#pragma once

#ifdef DUBLICATEHANDLEMOD_EXPORTS  
#define DUBLICATEHANDLEMOD_API __declspec(dllexport)   
#else  
#define DUBLICATEHANDLEMOD_API __declspec(dllimport)   
#endif  


#include "stdafx.h"
#include "windows.h"
#include <cwchar>
#include <iostream> 
using namespace std;
extern "C" DUBLICATEHANDLEMOD_API DWORD PassHandles(HANDLE, HANDLE, DWORD, DWORD);
extern "C" DUBLICATEHANDLEMOD_API DWORD GetHandle(HANDLE); 
