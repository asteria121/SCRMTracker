#include "pch.h"
#include "IP.h"


/// <summary>
/// Parse MAC address from network adapter
/// </summary>
/// <param name="d">pcap device ptr</param>
/// <returns>6 Bytes MAC address array</returns>
std::optional<std::array<u_char, MAC_ADDR_LEN>> GetMACAddressByDevice(const pcap_if_t* d)
{
	DWORD dwSize = sizeof(MIB_IFTABLE);
	std::vector<BYTE> ifTableBuffer(dwSize);

	auto pIfTable = reinterpret_cast<MIB_IFTABLE*>(ifTableBuffer.data());
	DWORD dwRetVal = GetIfTable(pIfTable, &dwSize, FALSE);

	if (dwRetVal == ERROR_INSUFFICIENT_BUFFER)
	{
		ifTableBuffer.resize(dwSize);
		pIfTable = reinterpret_cast<MIB_IFTABLE*>(ifTableBuffer.data());
		dwRetVal = GetIfTable(pIfTable, &dwSize, FALSE);
	}

	if (dwRetVal != NO_ERROR)
	{
		return std::nullopt;
	}

	const int wideNameSize = MultiByteToWideChar(CP_UTF8, 0, d->name, -1, nullptr, 0);
	if (wideNameSize == 0)
	{
		return std::nullopt;
	}

	// Convert to wide string
	std::wstring wszWideName(wideNameSize, L'\0');
	MultiByteToWideChar(CP_UTF8, 0, d->name, -1, wszWideName.data(), wideNameSize);
	wszWideName.resize(wideNameSize - 1); // Remove null terminator

	// Lambda function which extract GUID from pcap device name
	auto extractGuid = [](std::wstring_view name) -> std::optional<std::wstring_view>
		{
			auto start = name.find(L'{');
			if (start == std::wstring_view::npos) return std::nullopt;

			auto end = name.find(L'}', start);
			if (end == std::wstring_view::npos) return std::nullopt;

			return name.substr(start, end - start + 1);
		};

	auto pcapGuid = extractGuid(wszWideName);
	if (!pcapGuid)
	{
		return std::nullopt;
	}

	// Search for matching interface
	for (DWORD i = 0; i < pIfTable->dwNumEntries; i++)
	{
		const auto& entry = pIfTable->table[i];

		auto ifGuid = extractGuid(entry.wszName);
		if (ifGuid && *pcapGuid == (*ifGuid))
		{
			if (entry.dwPhysAddrLen == MAC_ADDR_LEN)
			{
				std::array<u_char, MAC_ADDR_LEN> macAddr;
				std::memcpy(macAddr.data(), entry.bPhysAddr, MAC_ADDR_LEN);
				return macAddr;
			}
		}
	}

	return std::nullopt;
}