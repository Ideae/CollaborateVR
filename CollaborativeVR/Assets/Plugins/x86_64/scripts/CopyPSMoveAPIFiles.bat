@echo on

IF NOT EXIST SetPSMoveBuildVars.bat goto BUILD_VAR_NOT_SET

call SetPSMoveBuildVars.bat

xcopy %PSMOVE_ROOT_PATH%\build\Release\assets ..\assets /S /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\magnetometer_calibration.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\external\PS3EYEDriver\sdl\x64\Release\ps3eye_sdl.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\psmoveapi.dll ..\psmoveapi.dll /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\psmoveapi_tracker.dll ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\psmovepair.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\SDL2.dll ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\test_calibration.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\test_opengl.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\test_tracker.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\tracker_camera_calibration.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

xcopy %PSMOVE_ROOT_PATH%\build\Release\visual_coregister_dk2.exe ..\ /Y
if not errorlevel 0 goto COPY_FAILED

echo [SUCCESS] PSMoveAPI Release build copy complete
pause
goto EXIT

:COPY_FAILED
echo [ERROR] Failed to copy expected file
pause
goto EXIT

:BUILD_VAR_NOT_SET
echo [ERROR] Run BindPSMoveAPIBuild.bat first
pause
goto EXIT

:EXIT