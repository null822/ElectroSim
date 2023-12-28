dotnet publish ^
../ElectroSim.csproj ^
/p:DebugType=none ^
/p:DebugSymbols=false ^
-r win-x64 ^
-c Release ^
--self-contained
