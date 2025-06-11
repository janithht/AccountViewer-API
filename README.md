"# AccountViewer-API" 

Run Project:

dotnet build AccountViewer-BackEnd/AccountsViewer.csproj --configuration Release

dotnet run --project AccountViewer-BackEnd/AccountsViewer.csproj --configuration Release --no-build

Run Tests:

dotnet build AccountViewer.Tests/AccountViewer.Tests.csproj --configuration Release

dotnet test AccountViewer.Tests/AccountViewer.Tests.csproj --configuration Release --no-build
