@echo off
setlocal

set thisdir=%~dp0
set configuration=Debug
set runtests=true

:parseargs
if "%1" == "" goto argsdone
if /i "%1" == "-c" goto set_configuration
if /i "%1" == "--configuration" goto set_configuration
if /i "%1" == "-notest" goto set_notest
if /i "%1" == "--notest" goto set_notest

echo Unsupported argument: %1
goto error

:set_configuration
set configuration=%2
shift
shift
goto parseargs

:set_notest
set runtests=false
shift
goto parseargs

:argsdone

:: IxMilia.Dwg needs a custom invocation to generate code
pwsh %~dp0src\IxMilia.Dwg\build-and-test.ps1 -notest -c %configuration%
if errorlevel 1 echo Error pre-building IxMilia.Dwg && exit /b 1

:: IxMilia.Dxf needs a custom invocation to generate code
call %~dp0src\IxMilia.Dxf\build-and-test.cmd -notest -c %configuration%
if errorlevel 1 echo Error pre-building IxMilia.Dxf && exit /b 1

:: build
set SOLUTION=%~dp0IxMilia.Converters.sln
dotnet restore %SOLUTION%
if errorlevel 1 exit /b 1
dotnet build %SOLUTION% -c %configuration%
if errorlevel 1 exit /b 1

:: test
if /i "%runtests%" == "true" (
    dotnet test "%SOLUTION%" -c %configuration% --no-restore --no-build
    if errorlevel 1 goto error
)

dotnet pack --no-restore --no-build --configuration %configuration%
set PACKAGE_COUNT=0
for %%a in ("%thisdir%artifacts\packages\%configuration%\*.nupkg") do set /a PACKAGE_COUNT+=1
if not "%PACKAGE_COUNT%" == "1" echo Expected a single NuGet package but found %PACKAGE_COUNT% at '%thisdir%artifacts\packages\%configuration%' && goto error

goto :eof

:error
echo Error building project.
exit /b 1
