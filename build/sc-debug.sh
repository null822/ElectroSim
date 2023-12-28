#!/bin/bash
dotnet build \
../ElectroSim.csproj \
-r linux-x64 \
--self-contained
