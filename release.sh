#!/bin/bash
dotnet publish \
/p:DebugType=none \
/p:DebugSymbols=false \
-r linux-x64 \
-c Release
