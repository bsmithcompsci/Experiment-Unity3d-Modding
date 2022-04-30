@echo off
call :NORMALIZE InternalPath "../Game Demo/Assets/Scripts/Modding/Plugins"
call :NORMALIZE ExternalPath "../Mod Demo for Game/Assets/ModSDK/Plugins"
call :NORMALIZE Release "./Packaged/Release"

copy "%Release%\ModSDK.dll" "%InternalPath%\ModSDK.dll"
copy "%Release%\ModSDK-Editor.dll" "%InternalPath%\ModSDK-Editor.dll"
copy "%Release%\ModSDK.dll" "%ExternalPath%\ModSDK.dll"
copy "%Release%\ModSDK-Editor.dll" "%ExternalPath%\ModSDK-Editor.dll"
pause

:NORMALIZE OUTVAR PATH
    set %1=%~f2
    exit /b