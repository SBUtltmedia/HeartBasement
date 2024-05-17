@echo off
set OUTPUT_PATH="RhubarbOutput.txt"
set INPUT_PATH="RhubarbInput.txt"
set RHUBARB_PATH="Rhubarb\rhubarb.exe"

if "%2"=="" (goto default) else goto extended

:default:
%RHUBARB_PATH% %1 -d %INPUT_PATH% --extendedShapes "" > %OUTPUT_PATH%
goto end

:extended:
%RHUBARB_PATH% %1 -d %INPUT_PATH% --extendedShapes %2 > %OUTPUT_PATH%
goto end

:end;
