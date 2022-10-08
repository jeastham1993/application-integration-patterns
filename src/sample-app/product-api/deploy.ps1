dotnet test .\test\ProductApi.Test\ProductApi.Test.csproj
dotnet publish .\application\ApplicationIntegrationPatterns.sln --configuration "Release" --framework "net6.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-x64 --self-contained false
terraform apply --var-file dev.tfvars
dotnet test .\test\ProductApi.IntegrationTest\ProductApi.IntegrationTest.csproj