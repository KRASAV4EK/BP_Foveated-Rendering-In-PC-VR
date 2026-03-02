#include "GPClientComponent.h"

UGPClientComponent::UGPClientComponent()
{
#if PLATFORM_WINDOWS
	PrimaryComponentTick.bCanEverTick = false;
#endif // PLATFORM_WINDOWS
}

void UGPClientComponent::BeginDestroy()
{
#if PLATFORM_WINDOWS
	Disconnect();
	Super::BeginDestroy();
#endif // PLATFORM_WINDOWS
}

void UGPClientComponent::Connect(const FString& Address, int32 Port)
{
#if PLATFORM_WINDOWS
	Client.set_address(TCHAR_TO_UTF8(*Address));
	Client.set_port((unsigned)Port);
	Client.client_connect();
#endif // PLATFORM_WINDOWS
}

void UGPClientComponent::Disconnect()
{
#if PLATFORM_WINDOWS
	Client.client_disconnect();
#endif // PLATFORM_WINDOWS
}

void UGPClientComponent::SendCommand(const FString& Cmd)
{
#if PLATFORM_WINDOWS
	Client.send_cmd(TCHAR_TO_UTF8(*Cmd));
#endif // PLATFORM_WINDOWS
}

bool UGPClientComponent::IsConnected() const 
{
#if PLATFORM_WINDOWS
	return const_cast<GPClient&>(Client).is_connected();
#else
	return false;
#endif // PLATFORM_WINDOWS
}
bool UGPClientComponent::HasRecentRx() const 
{
#if PLATFORM_WINDOWS
	return const_cast<GPClient&>(Client).get_rx_status();
#else
	return false;
#endif // PLATFORM_WINDOWS
}

FString UGPClientComponent::GetLatest()
{
#if PLATFORM_WINDOWS
	std::string s = Client.get_rx_latest();
	return UTF8_TO_TCHAR(s.c_str());
#else
	return FString();
#endif // PLATFORM_WINDOWS
}
