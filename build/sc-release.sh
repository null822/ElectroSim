#!/bin/bash
dotnet publish \
../ElectroSim.csproj \
/p:DebugType=none \
/p:DebugSymbols=false \
-r linux-x64 \
-c Release \
--self-contained
