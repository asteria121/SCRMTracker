#include "pch.h"
#include "NetworkAdapters.h"


/// <summary>
/// Constructor of NetworkAdapters, to find adapter from pcap device list
/// </summary>
NetworkAdapters::NetworkAdapters()
{
	std::array<char, PCAP_ERRBUF_SIZE> errbuf;

	if (pcap_findalldevs_ex(PCAP_SRC_IF_STRING, nullptr, &deviceList, errbuf.data()) == -1)
	{
		throw std::runtime_error(std::format("Error in pcap_findalldevs: {}", errbuf.data()));
	}
}


/// <summary>
/// Desturctor of NetworkAdpaters
/// </summary>
NetworkAdapters::~NetworkAdapters()
{
	if (deviceList != nullptr)
	{
		pcap_freealldevs(deviceList);
		deviceList = nullptr;
	}
}


/// <summary>
/// Find pcap device by network adapter's ID
/// </summary>
/// <param name="strAdapterId">Network adapter's ID</param>
/// <returns>pcap device ptr if exist</returns>
const pcap_if_t* NetworkAdapters::GetNetworkAdapterById(const std::string& strAdapterId)
{
	for (pcap_if_t* device = deviceList; device != nullptr; device = device->next)
	{
		if (std::string(device->name).find(strAdapterId) != std::string::npos)
		{
			return device;
		}
	}

	return nullptr;
}