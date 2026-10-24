@echo off
rem Приклади:  run.cmd train --mode neural   |   run.cmd train --mode hybrid
rem            run.cmd mc --n 200            |   run.cmd ideal   |   run.cmd trace --mode hybrid
if "%UNITY_EDITOR_DATA%"=="" set UNITY_EDITOR_DATA=C:\Program Files\Unity\Hub\Editor\6000.4.8f1\Editor\Data
"%UNITY_EDITOR_DATA%\NetCoreRuntime\dotnet.exe" "%~dp0bin\HeadlessSim.dll" %*
