autorest readme.md
dotnet publish FuncLite.Client.csproj -c Release -r win10-x64 -o ./bin/Release/PublishOutput
cd npm
npm link
cd ..
