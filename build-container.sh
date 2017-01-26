#!bin/bash
set -e
dotnet restore
dotnet test test/performance-log-middleware.test/project.json -xml $(pwd)/testresults/out.xml
rm -rf $(pwd)/package
dotnet pack src/performance-log-middleware/project.json -c release -o $(pwd)/package --version-suffix=${BuildNumber}
mkdir $(pwd)/symbols
cp $(pwd)/package/*.symbols.nupkg $(pwd)/symbols
rm $(pwd)/package/*.symbols.nupkg