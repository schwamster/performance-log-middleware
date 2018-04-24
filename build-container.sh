#!bin/bash
set -e
dotnet restore
dotnet test test/performance-log-middleware.test/performance-log-middleware.test.csproj --test-adapter-path:. --logger:xunit
rm -rf $(pwd)/package
dotnet pack src/performance-log-middleware/performance-log-middleware.csproj -c release -o $(pwd)/package
mkdir $(pwd)/symbols
cp $(pwd)/package/*.symbols.nupkg $(pwd)/symbols
rm $(pwd)/package/*.symbols.nupkg