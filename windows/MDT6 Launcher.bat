@echo off
setlocal enabledelayedexpansion

set "targetPath1=C:\Program Files (x86)\MDT6\acad.exe"
set "targetPath2=C:\Program Files\MDT6\acad.exe"

set "targetPath="
set "configPath="

if exist "%targetPath1%" (
    set "targetPath=%targetPath1%"
    set "configPath=C:\Program Files (x86)\MDT6\acadm\acadmpp.arg"
) else if exist "%targetPath2%" (
    set "targetPath=%targetPath2%"
    set "configPath=C:\Program Files\MDT6\acadm\acadmpp.arg"
) else (
    if exist "C:\Program Files (x86)\MDT6" (
        set "targetPath=%targetPath1%"
        set "configPath=C:\Program Files (x86)\MDT6\acadm\acadmpp.arg"
    ) else if exist "C:\Program Files\MDT6" (
        set "targetPath=%targetPath2%"
        set "configPath=C:\Program Files\MDT6\acadm\acadmpp.arg"
    ) else (
        color 0c
        cls
        echo.
        echo ==============================================
        echo           ERROR: Directory Not Found
        echo ==============================================
        echo.
        echo   The MDT6 installation directory was not found.
        echo.
        echo   Please ensure MDT6 is properly installed.
        echo.
        echo ==============================================
        pause
        goto :eof
    )
    
    copy "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\其他3D\CAD-WIN10\acad.exe" "!targetPath!" >nul
    if errorlevel 1 (
        color 0c
        cls
        echo.
        echo ==============================================
        echo           ERROR: Copy Failed
        echo ==============================================
        echo.
        echo   Failed to copy file from remote server.
        echo   Please check your network connection.
        echo.
        echo ==============================================
        pause
        goto :eof
    )
)

if not exist "!configPath!" (
    for %%A in ("!configPath!") do set "configDir=%%~dpA"
    if not exist "!configDir!" mkdir "!configDir!"
    
    copy "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\其他3D\CAD-WIN10\acadmpp.arg" "!configPath!" >nul
    if errorlevel 1 (
        color 0c
        cls
        echo.
        echo ==============================================
        echo           ERROR: Config Copy Failed
        echo ==============================================
        echo.
        echo   Failed to copy config file from server.
        echo   Please check your network connection.
        echo.
        echo ==============================================
        pause
        goto :eof
    )
)

start "" "!targetPath!" /p "!configPath!"
endlocal