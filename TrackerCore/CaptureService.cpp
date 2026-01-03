#include "pch.h"
#include "IP.h"
#include "CaptureService.h"
#include "NetworkAdapters.h"


PlayerJoinCallback g_JoinCallback = nullptr;


/// <summary>
/// Constructor of CaptureService, start pcap_loop thread for packet capture
/// </summary>
/// <param name="strAdapterId">ID of network adapter</param>
/// <param name="callbackFunction">Callback function which is triggered by player join</param>
CaptureService::CaptureService(const std::string& strAdapterId, PlayerJoinCallback callbackFunction)
{
	std::array<char, PCAP_ERRBUF_SIZE> errbuf;
	u_int netmask;
	struct bpf_program fcode;

	if (callbackFunction == nullptr)
	{
		throw std::runtime_error(std::format("PlayerJoinCallback 함수 지정해야합니다.", strAdapterId));
	}
	
	// Get network adapter with adapter name
	// Should have to check network adapter name with Dll export function
	NetworkAdapters& instance = NetworkAdapters::GetInstance();
	const pcap_if_t* device = instance.GetNetworkAdapterById(strAdapterId);
	if (device == nullptr)
	{
		throw std::runtime_error(std::format("NPCAP 네트워크 어댑터 ID \"{}\" 찾을 수 없습니다.", strAdapterId));
	}

	// Open the adapter
	hPcap = pcap_open(device->name, 65536, PCAP_OPENFLAG_PROMISCUOUS, 1000, NULL, errbuf.data());
	if (hPcap == nullptr)
	{
		throw std::runtime_error(std::format("NPCAP 네트워크 어댑터 ID \"{}\" 열 수 없습니다.", device->name));
	}

	int linkType = pcap_datalink(hPcap);
	if (linkType != DLT_EN10MB && linkType != DLT_IEEE802_11)
	{
		throw std::runtime_error("지원되지 않는 네트워크 어댑터입니다.");
	}

	if (device->addresses != nullptr)
		netmask = ((struct sockaddr_in*)(device->addresses->netmask))->sin_addr.S_un.S_addr;
	else
		netmask = 0xFFFFFF;

	// compile filter with udp, 6112 port, packet size >= 400, inbound
	auto pMacAddress = GetMACAddressByDevice(device);
	if (!pMacAddress.has_value())
	{
		throw std::runtime_error(std::format("NPCAP 네트워크 어댑터 ID \"{}\" MAC 주소 찾을 수 없습니다.", strAdapterId));
	}

	std::string packet_filter = std::format(
		"udp port 6112 and greater 400 and ether dst {:02X}:{:02X}:{:02X}:{:02X}:{:02X}:{:02X}",
		(*pMacAddress)[0], (*pMacAddress)[1], (*pMacAddress)[2], (*pMacAddress)[3], (*pMacAddress)[4], (*pMacAddress)[5]
	);

	if (pcap_compile(hPcap, &fcode, packet_filter.c_str(), 1, netmask) < 0)
	{
		throw std::runtime_error("NPCAP 패킷 필터 컴파일 실패했습니다.");
	}

	if (pcap_setfilter(hPcap, &fcode) < 0)
	{
		pcap_freecode(&fcode);
		throw std::runtime_error("NPCAP 패킷 필터 설정 실패했습니다.");
	}

	pcap_freecode(&fcode);
	g_JoinCallback = callbackFunction;
}


/// <summary>
/// Destructor of CaptureService, suspend pcap_loop thread for packet capture
/// </summary>
CaptureService::~CaptureService()
{
	StopCaptureThread();
}


/// <summary>
/// Create thread for packet capture
/// </summary>
void CaptureService::StartCaptureThread()
{
	if (isRunning == true || hPcap == nullptr) return;

	std::lock_guard<std::mutex> lock(pcapMtx);

	isRunning = true;
	captureThread = std::thread([this]()
	{
		pcap_loop(hPcap, 0, NpcapParseCallback, nullptr);
		isRunning = false;
	});
}


/// <summary>
/// Suspend thread for packet capture
/// </summary>
void CaptureService::StopCaptureThread()
{
	if (isRunning == true)
	{
		{
			std::lock_guard<std::mutex> lock(pcapMtx);
			if (hPcap != nullptr)
				pcap_breakloop(hPcap);
		}

		if (captureThread.joinable())
			captureThread.join();

		isRunning = false;
	}

	{
		std::lock_guard<std::mutex> lock(pcapMtx);
		if (hPcap != nullptr)
		{
			pcap_close(hPcap);
			hPcap = nullptr;
		}
	}
}


/// <summary>
/// pcap_loop callback function, check for player join
/// </summary>
/// <param name="param"></param>
/// <param name="header"></param>
/// <param name="pkt_data"></param>
void CaptureService::NpcapParseCallback(u_char* param, const struct pcap_pkthdr* header, const u_char* pkt_data)
{
	// Store player information by current room's PID
	static std::array<std::string, ROOM_PLAYER_SLOT_COUNT> currentPlayersTag;
	static std::array<std::string, ROOM_PLAYER_SLOT_COUNT> currentPlayersName;
	static std::array<uint32_t, ROOM_PLAYER_SLOT_COUNT> currentPlayersIP;

	// Parse UDP packet
	const ip_header* ipHdr = reinterpret_cast<const ip_header*>(pkt_data + sizeof(ethernet_header));
	const u_int ipLen = (ipHdr->ver_ihl & 0xF) * 4;

	const udp_header* udpHdr = reinterpret_cast<const udp_header*>((u_char*)ipHdr + ipLen);
	const char* data = reinterpret_cast<const char*>(udpHdr) + sizeof(udp_header);

	// Check player join signature
	if (memcmp(data, "\x08\x01\x12\xa6\x03\x00\x00\x00\x00", 9) == 0)
	{
		char pid = data[PID_OFFSET];
		std::string strBattleTag = data + BATTLETAG_OFFSET;
		std::string strUserName = data + USERNAME_OFFSET;

		if ((0 <= pid && pid < 8) || (-128 <= pid && pid < -124))
		{
			// Set spectator's pid 8~11 to store at array
			if (pid < 0)
				pid += 128 + 8;

			// Prevent duplicate join event
			if (currentPlayersTag[pid] != strBattleTag || currentPlayersName[pid] != strUserName)
			{
				uint32_t ipAddress = 0;
				memcpy_s(&ipAddress, sizeof(ipAddress), reinterpret_cast<const void*>(&ipHdr->saddr), sizeof(ipHdr->saddr));

				currentPlayersTag[pid] = strBattleTag;
				currentPlayersName[pid] = strUserName;
				currentPlayersIP[pid] = ipAddress;

				// Send player information with registered C# frontend's callback function
				PlayerInfo info;
				info.pid = pid;
				info.battleTag = currentPlayersTag[pid].c_str();
				info.userName = currentPlayersName[pid].c_str();
				info.ipAddress = currentPlayersIP[pid];

				if (g_JoinCallback != nullptr)
					g_JoinCallback(&info);
			}
		}
	}
}