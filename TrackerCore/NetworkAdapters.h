#pragma once


/// <summary>
/// Singleton class to manage network adapter list with npcap
/// </summary>
class NetworkAdapters
{
public:
	/// <summary>
	/// Get singleton instance of NetworkAdapters.
	/// </summary>
	static NetworkAdapters& GetInstance()
	{
		static NetworkAdapters instance;
		return instance;
	}


	/// <summary>
	/// Get npcap adapter object pointer with adapter name
	/// </summary>
	/// <param name="strAdapterName">Adapter name</param>
	/// <returns>Npcap adapter object pointer</returns>
	const pcap_if_t* GetNetworkAdapterById(const std::string& strAdapterName);
	

	// Prevent object copy
	NetworkAdapters(const NetworkAdapters&) = delete;
	NetworkAdapters& operator=(const NetworkAdapters&) = delete;


	// Prevent object move
	NetworkAdapters(NetworkAdapters&&) = delete;
	NetworkAdapters& operator=(NetworkAdapters&&) = delete;

private:
	// Hidden constructor and destructor
	NetworkAdapters();
	~NetworkAdapters();

	pcap_if_t* deviceList = nullptr;
};