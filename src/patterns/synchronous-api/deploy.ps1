dotnet test .\test\ProductApi.Test\ProductApi.Test.csproj
dotnet publish .\application\SynchronousApi.sln --configuration "Release" --framework "net6.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-x64 --self-contained false
terraform apply