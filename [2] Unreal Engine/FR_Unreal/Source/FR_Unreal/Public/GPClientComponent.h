#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "GPClient.h"
#include "GPClientComponent.generated.h"

UCLASS(ClassGroup = (Custom), meta = (BlueprintSpawnableComponent))
class FR_UNREAL_API UGPClientComponent : public UActorComponent
{
	GENERATED_BODY()
public:
	UGPClientComponent();
	virtual void BeginDestroy() override;

	UFUNCTION(BlueprintCallable, Category = "GazePoint")
	void Connect(const FString& Address = TEXT("127.0.0.1"), int32 Port = 4242);

	UFUNCTION(BlueprintCallable, Category = "GazePoint")
	void Disconnect();

	UFUNCTION(BlueprintCallable, Category = "GazePoint")
	void SendCommand(const FString& Cmd);

	UFUNCTION(BlueprintPure, Category = "GazePoint")
	bool IsConnected() const;

	UFUNCTION(BlueprintPure, Category = "GazePoint")
	bool HasRecentRx() const;

	UFUNCTION(BlueprintCallable, Category = "GazePoint")
	FString GetLatest();

private:
	#if PLATFORM_WINDOWS
	GPClient Client;
	#endif // PLATFORM_WINDOWS
};
