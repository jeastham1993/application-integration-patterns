dotnet restore .\functions\AntiCorruptionLayer.sln
dotnet publish .\functions\AntiCorruptionLayer.sln --configuration "Release" --framework "net6.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-x64 --self-contained false
terraform apply