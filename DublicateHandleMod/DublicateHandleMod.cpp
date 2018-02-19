// DublicateHandleMod.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "DublicateHandleMod.h"
#include "windows.h"
#include <cwchar>
#include <iostream> //for cout and cin 
using namespace std;

/*DWORD PassHandles(LPCTSTR pipeName, HANDLE handleToDublicate[], DWORD numberOfHandles) {

	wchar_t name[256] = TEXT("\\\\.\\pipe\\");

	wcscat_s(name, pipeName); // just adds to the name end pipeName

	HANDLE namedPipe = CreateNamedPipe(name, PIPE_ACCESS_DUPLEX, PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE,
		1, 256, 256, 0, NULL);
	if (namedPipe == INVALID_HANDLE_VALUE) {
		DWORD err = GetLastError();
		cout << "Klaida CreateNamedPipe. Kodas: " << err << endl;
	}

	if (false == ConnectNamedPipe(namedPipe, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida ConnectPipe. Kodas: " << err << endl;
	}

	HANDLE currentProcces = GetCurrentProcess();
	if (currentProcces == NULL) {
		DWORD err = GetLastError();
		cout << "Klaida getcurrentprocess. Kodas: " << err << endl;
	}

	BYTE buffer[8];
	DWORD numberOfBytesRead;
	DWORD lpNumberOfBytesWritten;


	if (false == ReadFile(namedPipe, buffer, sizeof(buffer), &numberOfBytesRead, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida ReadFile. Kodas: " << err << endl;
	};

	DWORD *id;
	id = (DWORD*)buffer;

	cout << *id << endl;

	HANDLE targetProcess = OpenProcess(0x0040, false, *id);
	if (targetProcess == NULL) {
		DWORD err = GetLastError();
		cout << "Klaida open process. Kodas: " << err << endl;
	}

	HANDLE pointerToCopy;

	if (false == DuplicateHandle(currentProcces, event, targetProcess, &pointerToCopy, 0x100002, false, 0x00000002)) {
		DWORD err = GetLastError();
		cout << "Klaida dublicatehandle. Kodas" << err << endl;
	}

	cout << pointerToCopy << endl;

	int index = (int)pointerToCopy;

	if (false == WriteFile(namedPipe, &pointerToCopy, sizeof(buffer), &lpNumberOfBytesWritten, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida writeFile. Kodas" << err << endl;
	}// or DWORD *lpNumberOfBytesWritten and the pNumberOfBytesWritten;

	cout << "Signal" << endl;

	return index;
}*/

DWORD DublicateHandlesMod() {
	return 0;
}

DWORD PassHandles(HANDLE namedPipe, HANDLE handle, DWORD dwDesiredAccsses, DWORD dwOptions) {

	HANDLE currentProcces = GetCurrentProcess();
	if (currentProcces == NULL) {
		DWORD err = GetLastError();
		cout << "Klaida getcurrentprocess. Kodas: " << err << endl;
	}

	BYTE buffer[8];
	DWORD numberOfBytesRead;
	DWORD lpNumberOfBytesWritten;


	if (false == ReadFile(namedPipe, buffer, sizeof(buffer), &numberOfBytesRead, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida ReadFile. Kodas: " << err << endl;
	};

	DWORD *id;
	id = (DWORD*)buffer;

	cout << *id << endl;

	HANDLE targetProcess = OpenProcess(0x0040, false, *id);
	if (targetProcess == NULL) {
		DWORD err = GetLastError();
		cout << "Klaida open process. Kodas: " << err << endl;
	}

	HANDLE pointerToCopy;

	if (false == DuplicateHandle(currentProcces, handle, targetProcess, &pointerToCopy, dwDesiredAccsses, false, dwOptions)) {
		DWORD err = GetLastError();
		cout << "Klaida dublicatehandle. Kodas" << err << endl;
	}

	cout << pointerToCopy << endl;

	int index = (int)pointerToCopy;

	if (false == WriteFile(namedPipe, &pointerToCopy, sizeof(buffer), &lpNumberOfBytesWritten, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida writeFile. Kodas" << err << endl;
	}// or DWORD *lpNumberOfBytesWritten and the pNumberOfBytesWritten;

	return index;
}


DWORD* GetHandles( LPCTSTR pipeName ) {

	wchar_t name[256] = TEXT("\\\\.\\pipe\\");

	wcscat_s(name, pipeName);

	HANDLE namedPipe = CreateFileW(name , GENERIC_READ | GENERIC_WRITE, 0, NULL, 3, 0, NULL);
	if (namedPipe == INVALID_HANDLE_VALUE) {
		DWORD err = GetLastError();
		cout << "Klaida CreateFile. Kodas: " << err << endl;
	}

	DWORD id = GetCurrentProcessId();

	DWORD numberOfWritten;

	if (FALSE == WriteFile(namedPipe, (BYTE *)&id, 8, &numberOfWritten, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida WirteFile. Kodas: " << err << endl;
	}

	DWORD numberOfRead;
	HANDLE handle;

	if (FALSE == ReadFile(namedPipe, &handle, 8, &numberOfRead, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida WirteFile. Kodas: " << err << endl;
	}

	cout << id << endl;

	if (FALSE == SetEvent(handle)) {
		DWORD err = GetLastError();
		cout << "Klaida WirteFile. Kodas: " << err << endl;
	}

	cin.get();

	return 0;

	DWORD handlesIndices[3];

	return handlesIndices;
}

DWORD GetHandle(HANDLE pipeHandle) {

	DWORD id = GetCurrentProcessId();

	DWORD numberOfWritten;

	if (FALSE == WriteFile(pipeHandle, (BYTE *)&id, 8, &numberOfWritten, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida WirteFile. Kodas: " << err << endl;
	}

	DWORD numberOfRead;
	HANDLE handle;

	if (FALSE == ReadFile(pipeHandle, &handle, 8, &numberOfRead, NULL)) {
		DWORD err = GetLastError();
		cout << "Klaida WirteFile. Kodas: " << err << endl;
	}

	return (DWORD)handle;
}



