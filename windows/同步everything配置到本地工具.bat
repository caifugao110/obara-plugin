:: @theme:同步everything配置到本地工具
:: @author:Tobin
:: @version:1.3.0
:: @date:MAY.07.26

@echo off
color 0a
title 同步everything配置到本地工具
mode con cols=115 lines=35

Echo                  ==========================================================================
ECHO.
Echo                                        同步everything配置到本地工具
ECHO.
Echo                                        作者: Tobin
ECHO.
Echo                                        版本：1.3.0
ECHO.
Echo                          check地址：\\192.168.160.2\生产管理部3d\3D 资料\check\check27
ECHO.
Echo                                        码云地址：https://gitee.com/caifugao110
ECHO.
Echo.                 ==========================================================================

:: 主程序入口

:: 获取当前日期和时间
for /f "tokens=2 delims==" %%i in ('wmic os get localdatetime /value') do set datetime=%%i
set "log_date=%datetime:~0,4%-%datetime:~4,2%-%datetime:~6,2%"
set "start_time=%datetime:~8,2%:%datetime:~10,2%:%datetime:~12,2%"

:: 获取数据库最后更新时间
set "filepath=\\192.168.160.2\生产管理部3d\3D 资料\check\check27\Everything\Everything.db"
for %%i in ("%filepath%") do set "database_time=%%~ti"

:: 变量定义
set "log_path=\\192.168.160.2\生产管理部3d\3D 资料\check\check27\Version control\Everything\LOG"
set "log_file=%log_path%\%log_date%_EverythingLOG.TXT"
set "source_path=\\192.168.160.2\生产管理部3d\3D 资料\check\check27\everything"
set "target_path=D:\tobin\everything"
set "network_address=192.168.160.2"

goto :check_network

:: 函数：检测局域网连接
:check_network
echo.
echo 检测局域网连接中...
ping -n 1 %network_address% >nul 2>&1
if %errorlevel% equ 0 goto :network_ok
echo 局域网连接失败，等待10秒后重试...
timeout /t 10 >nul
ping -n 1 %network_address% >nul 2>&1
if %errorlevel% equ 0 goto :network_ok
echo 局域网连接失败，程序终止
pause
exit /b

:network_ok
echo 局域网连接正常
goto :check_everything_running

:: 函数：检测并终止everything.exe进程
:check_everything_running
echo.
echo 检测everything是否在运行...
call :check_everything_process
if %check_result% neq 0 goto :everything_not_running
echo 发现everything正在运行，正在终止进程...
taskkill /f /im Everything.exe >nul 2>&1
timeout /t 8 /nobreak >nul
call :check_everything_process
if %check_result% neq 0 goto :everything_killed
echo 第一次终止未生效，正在重试...
wmic process where "name='Everything.exe'" call terminate >nul 2>&1
timeout /t 8 /nobreak >nul
call :check_everything_process
if %check_result% neq 0 goto :everything_killed
echo 进程终止失败，请手动关闭Everything后重试
pause
exit /b

:everything_killed
echo 进程终止成功
goto :prepare_log_dir

:everything_not_running
echo everything未在运行
goto :prepare_log_dir

:check_everything_process
set "tmp_file=%temp%\check_everything_%random%.tmp"
tasklist /fi "imagename eq Everything.exe" /nh >"%tmp_file%" 2>nul
findstr /i "Everything.exe" "%tmp_file%" >nul 2>&1
set "check_result=%errorlevel%"
del "%tmp_file%" >nul 2>&1
exit /b

:: 函数：创建日志目录，显示为同步权限
:prepare_log_dir
echo.
echo 检查同步权限...
if not exist "%log_path%" (
    echo 同步权限不可用，正在申请...
    mkdir "%log_path%"
    if %errorlevel% equ 0 (
        echo 同步权限申请成功
    ) else (
        echo 同步权限申请失败，程序终止
        pause
        exit /b
    )
) else (
    echo 同步权限已获取
)
goto :log_operation

:: 函数：记录日志
:log_operation
set "computer_name=%COMPUTERNAME%"

:: 计算程序运行时间
for /f "tokens=1-4 delims=:.," %%a in ("%start_time%") do (
    set /a "h=1%%a - 100"
    set /a "m=1%%b - 100"
    set /a "s=1%%c - 100"
    set /a "start_seconds=h*3600 + m*60 + s"
)

for /f "tokens=2 delims=:" %%d in ('ipconfig ^| findstr "IP"') do (
    set ip_address=%%d
)

echo date:%log_date%,computer_name:%computer_name%,start_time:%start_time%,database_time:%database_time%，ip_address:%ip_address% >> "%log_file%"

::echo.
::echo 运行日志已记录到: %log_file%

goto :clean_target_dir

:: 函数：清空调图文件夹
:clean_target_dir
echo.
echo 清空调图文件夹: %target_path%
if exist "%target_path%" (
    takeown /f "%target_path%" /r /d y >nul 2>&1
    icacls "%target_path%" /grant administrators:F /t >nul 2>&1
    del "%target_path%\*" /f/s/q >nul 2>&1
    echo 调图文件夹清理完成

) else (
    echo 调图文件夹不存在，将创建新文件夹
    mkdir "%target_path%"
    if exist "%target_path%" (
        echo 调图文件夹创建成功
    ) else (
        echo 调图文件夹创建失败，程序终止
        pause
        exit /b
    )
)
goto :sync_files

:: 函数：同步文件
:sync_files
echo.
echo 开始同步数据库...
xcopy "%source_path%" "%target_path%" /s/e/i/y >nul 2>&1
if %errorlevel% equ 0 (
    echo %COMPUTERNAME%您好，数据库已同步完成
	echo 数据库最后更新时间为: %database_time%
	
	echo.
	echo 正在打开Everything，请稍等...
	start "" "%target_path%\Everything.exe"
	echo. 
	pause
) else (
    echo 数据库同步失败，程序终止
	echo. 
    pause
    exit /b
)




goto :eof



