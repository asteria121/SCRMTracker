#pragma once


constexpr auto PID_OFFSET = 0x13;
constexpr auto BATTLETAG_OFFSET = 0x1B;
constexpr auto USERNAME_OFFSET = 0xE3;
constexpr auto ROOM_PLAYER_SLOT_COUNT = 12;


#pragma pack(push, 1)
struct PlayerInfo
{
	uint8_t		pid;
	LPCSTR		battleTag;
	LPCSTR		userName;
	uint32_t	ipAddress;
};
#pragma pack(pop)


// Callback function from C#
typedef void (*PlayerJoinCallback)(const PlayerInfo* info);

class CaptureService
{
public:
	CaptureService(const std::string& strAdapterName, PlayerJoinCallback callbackFunction);
	~CaptureService();

	void StartCaptureThread();
	void StopCaptureThread();

private:
	pcap_t* hPcap = nullptr;
	std::mutex pcapMtx;
	std::thread captureThread;
	std::atomic<bool> isRunning{ false };

	static void NpcapParseCallback(u_char* param, const struct pcap_pkthdr* header, const u_char* pkt_data);
};