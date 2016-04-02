@echo off
setlocal

::Select the path to the root opencv folder
set "psCommand="(new-object -COM 'Shell.Application')^
.BrowseForFolder(0,'Please select the install folder for psmoveapi (ex: c:\psmoveapi).',0,0).self.path""
for /f "usebackq delims=" %%I in (`powershell %psCommand%`) do set "PSMOVE_ROOT_PATH=%%I"
if NOT DEFINED PSMOVE_ROOT_PATH (goto failure)

:: Write out the paths to a config batch file
del /Q SetPSMoveBuildVars.bat
echo @echo off >> SetPSMoveBuildVars.bat
echo set PSMOVE_ROOT_PATH=%PSMOVE_ROOT_PATH%>> SetPSMoveBuildVars.bat

echo [SUCCESS] Bound PSMoveAPI Build to: %PSMOVE_ROOT_PATH%
pause
goto exit

:failure
echo [ERROR] Failed to bind PSMoveAPI path
goto exit

:exit
endlocal
