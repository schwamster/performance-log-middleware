#!bin/bash
set -e
dotnet restore
dotnet test test/health-check-middleware.test/project.json -xml $(pwd)/testresults/out.xml
rm -rf $(pwd)/package
dotnet pack src/health-check-middleware/project.json -c release -o $(pwd)/package --version-suffix=${BuildNumber}
mkdir $(pwd)/symbols
cp $(pwd)/package/*.symbols.nupkg $(pwd)/symbols
rm $(pwd)/package/*.symbols.nupkg