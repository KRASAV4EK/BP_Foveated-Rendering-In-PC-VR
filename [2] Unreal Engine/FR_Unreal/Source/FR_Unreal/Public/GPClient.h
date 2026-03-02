#pragma once

#if PLATFORM_WINDOWS

#include <string>
#include <vector>
#include <deque>
#include <thread>
#include <mutex>
#include <chrono>

class GPClient
{
private:
	unsigned int _ip_port;
	std::string _ip_address;

	unsigned int _rx_buffer_size;
	std::deque<std::string> _rx_buffer;
	std::vector<std::string> _tx_buffer;

	std::mutex _rx_mutex;
	std::mutex _tx_mutex;
	volatile bool _thread_exit;
	static unsigned int GPClientThread(GPClient* ptr);

	bool _keep_all_data = false;
	bool _rx_status = false;
	bool _connected_status = false;

public:
	GPClient();
	~GPClient();

	void set_address(std::string address) { _ip_address = address; }
	void set_port(unsigned int port) { _ip_port = port; }

	void client_connect();
	void client_disconnect();

	void send_cmd(std::string cmd);

	void set_rx_buffer_max(unsigned int max) { _rx_buffer_size = max; }
	std::string get_rx_latest();
	void get_rx(std::deque<std::string>& data);
	bool get_rx_status();
	bool is_connected();
};

#endif // PLATFORM_WINDOWS