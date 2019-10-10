@echo off

:: IxMilia.Dxf needs a custom invocation to generate code
call %~dp0src\IxMilia.Dxf\build-and-test.cmd -notest
if errorlevel 1 echo Error pre-building IxMilia.Dxf && exit /b 1

set SOLUTION=%~dp0IxMilia.Converters.sln
dotnet restore %SOLUTION%
if errorlevel 1 exit /b 1

dotnet build %SOLUTION%
if errorlevel 1 exit /b 1

dotnet test %SOLUTION%
if errorlevel 1 exit /b 1
