@echo off
rem Збирання headless-стенду (ядро симуляції + тренер ES) компілятором Roslyn і .NET,
rem які постачаються разом з Unity. За потреби змініть UNITY_EDITOR_DATA.
setlocal enabledelayedexpansion
if "%UNITY_EDITOR_DATA%"=="" set UNITY_EDITOR_DATA=C:\Program Files\Unity\Hub\Editor\6000.4.8f1\Editor\Data
set U=%UNITY_EDITOR_DATA%
set FW=
for /d %%d in ("%U%\NetCoreRuntime\shared\Microsoft.NETCore.App\*") do set FW=%%~d
if "%FW%"=="" (echo .NET runtime not found in "%U%" & exit /b 1)
pushd "%~dp0..\.."
if not exist Tools\HeadlessSim\bin mkdir Tools\HeadlessSim\bin
(for %%f in ("%FW%\*.dll") do @echo -r:"%%~f") > Tools\HeadlessSim\bin\refs_all.txt
findstr /v /i "Native DiaSym clr coreclr hostpolicy mscordaccore mscordbi mscorrc msquic createdump" Tools\HeadlessSim\bin\refs_all.txt > Tools\HeadlessSim\bin\refs.rsp
"%U%\NetCoreRuntime\dotnet.exe" "%U%\DotNetSdkRoslyn\csc.dll" @Tools\HeadlessSim\bin\refs.rsp @Tools\HeadlessSim\sources.rsp
set ERR=%ERRORLEVEL%
popd
if not "%ERR%"=="0" exit /b %ERR%
copy /y "%~dp0runtimeconfig.template.json" "%~dp0bin\HeadlessSim.runtimeconfig.json" >nul
echo build ok
