#include "pch.h"
#include "CaptureService.h"


/// <summary>
/// Check npcap library is available
/// </summary>
/// <returns>Npcap library availability</returns>
extern "C" __declspec(dllexport)
bool CheckNpcapAvailability()
{
	_TCHAR npcap_dir[512];
	UINT len;
	len = GetSystemDirectory(npcap_dir, 480);
	if (!len) {
		fprintf(stderr, "Error in GetSystemDirectory: %x", GetLastError());
		return false;
	}
	_tcscat_s(npcap_dir, 512, _T("\\Npcap"));
	if (SetDllDirectory(npcap_dir) == 0) {
		fprintf(stderr, "Error in SetDllDirectory: %x", GetLastError());
		return false;
	}
	return true;
}


/// <summary>
/// Global variable for CaptureService
/// </summary>
CaptureService* g_CaptureService = nullptr;
std::string g_ExceptionMessage = "";


/// <summary>
/// Initialize starcraft packet capture service, create thread at constructor(abstracted)
/// </summary>
/// <param name="strAdapterId">ID of network adapter</param>
/// <param name="callbackFunction">Callback function which is triggered by player join</param>
/// <returns>CaptureService initialization result</returns>
extern "C" __declspec(dllexport)
bool InitializeCaptureService(const char* strAdapterId, PlayerJoinCallback callbackFunction)
{
	try
	{
		g_CaptureService = new CaptureService(strAdapterId, callbackFunction);
		g_CaptureService->StartCaptureThread();

		return true;
	}
	catch (std::exception& e)
	{
		g_ExceptionMessage = e.what();
		if (g_CaptureService != nullptr)
		{
			delete g_CaptureService;
			g_CaptureService = nullptr;
		}

		return false;
	}
}


/// <summary>
/// Release starcraft packete capture service, suspend thread at destructor(abstracted)
/// </summary>
extern "C" __declspec(dllexport)
void ReleaseCaptureService()
{
	if (g_CaptureService != nullptr)
	{
		delete g_CaptureService;
		g_CaptureService = nullptr;
	}
}


/// <summary>
/// Get recent exception string, create managed memory to use at C#
/// </summary>
/// <returns>Recent exception string ptr</returns>
extern "C" __declspec(dllexport)
const char* GetLastException()
{
	// Free memory at C#
	const size_t len = g_ExceptionMessage.length() + 1;
	char* result = (char*)CoTaskMemAlloc(len);

	if (result)
	{
		strcpy_s(result, len, g_ExceptionMessage.c_str());
	}

	return result;
}