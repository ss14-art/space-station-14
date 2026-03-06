@echo off
dotnet build Content.Client
if errorlevel 1 exit /b %errorlevel%
dotnet build Content.Server
if errorlevel 1 exit /b %errorlevel%

Start "Client" /D "bin\\Content.Client" "Content.Client.exe"
Start "Server" /D "bin\\Content.Server" "Content.Server.exe"
