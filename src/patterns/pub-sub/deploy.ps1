dotnet restore .\functions\PubSub.sln
dotnet publish .\functions\PubSub.sln --configuration "Release" --framework "net6.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-x64 --self-contained false
terraform apply