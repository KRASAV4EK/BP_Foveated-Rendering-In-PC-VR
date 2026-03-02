#if PLATFORM_WINDOWS

#include "GPClient.h"
#include "Windows/PreWindowsApi.h"
#include <winsock2.h>
#include <ws2tcpip.h>
#include "Windows/PostWindowsApi.h"

#define RX_TCP_BUFFER_MAX 64000

GPClient::GPClient()
{
    _ip_port = 4242;
    _ip_address = "127.0.0.1";
    _rx_buffer_size = 60 * 60 * 3;
}

GPClient::~GPClient()
{
    client_disconnect();
}

void GPClient::client_connect()
{
    std::lock_guard<std::mutex> L(_rx_mutex);
    _tx_buffer.clear();
    _rx_buffer.clear();

    std::thread t(GPClientThread, this);
    t.detach();
    std::this_thread::sleep_for(std::chrono::milliseconds(200));
}

void GPClient::client_disconnect()
{
    {
        std::lock_guard<std::mutex> L(_rx_mutex);
        _thread_exit = true;
    }
    std::this_thread::sleep_for(std::chrono::milliseconds(100));
    {
        std::lock_guard<std::mutex> L(_rx_mutex);
        _rx_buffer.clear();
        _tx_buffer.clear();
    }
}

unsigned int GPClient::GPClientThread(GPClient* ptr)
{
    unsigned int result = 0;
    unsigned int delimiter_index = 0;
    std::string rxstr;
    char rxbuffer[RX_TCP_BUFFER_MAX];
    int state = 0;

    auto lastRxTime = std::chrono::steady_clock::now();

    WSADATA wsadata;
    SOCKET ipsocket;
    u_long poll = true;

    if (WSAStartup(0x0202, &wsadata))
        return 0;

    ipsocket = socket(AF_INET, SOCK_STREAM, 0);
    if (ipsocket == INVALID_SOCKET)
        return 0;

    if (ioctlsocket(ipsocket, FIONBIO, &poll) == SOCKET_ERROR)
        return 0;

    SOCKADDR_IN addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(static_cast<u_short>(ptr->_ip_port));
    inet_pton(AF_INET, ptr->_ip_address.c_str(), &addr.sin_addr);

    connect(ipsocket, (struct sockaddr*)&addr, sizeof(addr));

    ptr->_thread_exit = false;
    std::this_thread::sleep_for(std::chrono::milliseconds(250));
    ptr->_rx_status = false;
    ptr->_connected_status = true;

    do
    {
        if (std::chrono::steady_clock::now() - lastRxTime > std::chrono::seconds(4))
            ptr->_rx_status = false;

        if (ipsocket != NULL && !ptr->_thread_exit)
        {
            do
            {
                result = recv(ipsocket, rxbuffer, RX_TCP_BUFFER_MAX, 0);

                if (result == SOCKET_ERROR)
                {
                    state = WSAGetLastError(); (void)state;
                }
                else if (result > 0)
                {
                    lastRxTime = std::chrono::steady_clock::now();
                    ptr->_rx_status = true;

                    if (result > RX_TCP_BUFFER_MAX - 1)
                        result = RX_TCP_BUFFER_MAX - 1;

                    rxbuffer[result] = '\0';
                    rxstr += rxbuffer;

                    delimiter_index = static_cast<unsigned int>(rxstr.find("\r\n", 0));
                    std::lock_guard<std::mutex> L(ptr->_rx_mutex);
                    while (delimiter_index != std::string::npos && !rxstr.empty())
                    {
                        std::string tmp = rxstr.substr(0, delimiter_index);
                        ptr->_rx_buffer.push_back(tmp);
                        while (ptr->_rx_buffer.size() > ptr->_rx_buffer_size)
                            ptr->_rx_buffer.pop_front();

                        rxstr.erase(0, delimiter_index + 2);
                        delimiter_index = static_cast<unsigned int>(rxstr.find("\r\n", 0));
                    }
                }
            } while (result > 0 && result != SOCKET_ERROR && !ptr->_thread_exit);

            {
                std::lock_guard<std::mutex> T(ptr->_tx_mutex);
                for (auto& s : ptr->_tx_buffer)
                    send(ipsocket, s.c_str(), static_cast<int>(s.size()), 0);
                ptr->_tx_buffer.clear();
            }
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(5));
    } while (!ptr->_thread_exit);

    closesocket(ipsocket);
    WSACleanup();
    ptr->_connected_status = false;
    return true;
}

void GPClient::send_cmd(std::string cmd)
{
    cmd += "\r\n";
    std::lock_guard<std::mutex> L(_rx_mutex);
    _tx_buffer.push_back(cmd);
}

std::string GPClient::get_rx_latest()
{
    std::string tmp;
    std::lock_guard<std::mutex> L(_rx_mutex);
    if (!_rx_buffer.empty())
    {
        tmp = _rx_buffer.back();
        _rx_buffer.clear();
    }
    return tmp;
}

void GPClient::get_rx(std::deque<std::string>& data)
{
    std::lock_guard<std::mutex> L(_rx_mutex);
    data.clear();
    _rx_buffer.swap(data);
}

bool GPClient::get_rx_status() { return _rx_status; }
bool GPClient::is_connected() { return _connected_status; }

#endif // PLATFORM_WINDOWS
