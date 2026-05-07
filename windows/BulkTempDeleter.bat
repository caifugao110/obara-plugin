:: @theme:批量删除temp文件工具
:: @author:Tobin
:: @version:1.2.0
:: @date:MAY.06.26

@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
color 0a
Title 批量删除temp文件工具
Mode con cols=115 lines=42
date/t
ECHO.
Echo                  ==========================================================================
ECHO.
Echo                                        批量删除temp文件工具
ECHO.
Echo                                      适用部门: 仅限技术开发二部
ECHO.
Echo                                        版本：1.2.0
ECHO.
Echo                    check地址：\\192.168.160.2\生产管理部3d\3D 资料\check\check27\mysynchro
ECHO.
rem Echo                                    码云地址：https://gitee.com/caifugao110
rem ECHO.
Echo.                 ==========================================================================
ECHO.
echo.
ECHO                   [1] PDM临时文件夹                  [4] 用户临时文件夹
ECHO.
ECHO                   [2] CAD临时文件夹                  [5] 浏览器缓存(Chrome/Edge)
ECHO.
ECHO                   [3] SolidWorks临时文件             [6] 回收站
ECHO.
Echo.                 ==========================================================================
ECHO.
Echo                   请输入Y全部处理，或输入数字选择对应项(如1235)
ECHO.
Echo                   请确认已关闭PDM、CAD、SolidWorks及浏览器
ECHO.
Echo.                 ==========================================================================
ECHO.

:ask
set "ST="
set /P ST= 请输入(Y/数字组合)或Enter(退出)：
echo.
if "!ST!"=="" goto done
if /I "!ST!"=="Y" goto jixu

if "!ST:~6,1!" neq "" (
    echo.
    Echo                   输入无效：最多输入6个字符，请重新输入
    echo.
    goto ask
)

set "VALID=1"
set "_tmp=!ST!"
:validloop
if "!_tmp!"=="" goto validend
set "_c=!_tmp:~0,1!"
set "_tmp=!_tmp:~1!"
if "!_c!"=="1" goto validloop
if "!_c!"=="2" goto validloop
if "!_c!"=="3" goto validloop
if "!_c!"=="4" goto validloop
if "!_c!"=="5" goto validloop
if "!_c!"=="6" goto validloop
set "VALID=0"
:validend
if "!VALID!"=="0" (
    echo.
    Echo                   输入无效：仅允许数字1-6，请重新输入
    echo.
    goto ask
)

set "DO1=0"
set "DO2=0"
set "DO3=0"
set "DO4=0"
set "DO5=0"
set "DO6=0"
set "_tmp=!ST!"
:parse
if "!_tmp!"=="" goto parseend
set "_c=!_tmp:~0,1!"
set "_tmp=!_tmp:~1!"
set "DO!_c!=1"
goto parse
:parseend

if "!DO1!!DO2!!DO3!!DO4!!DO5!!DO6!"=="000000" (
    echo.
    Echo                   未选择任何项目，请重新输入
    echo.
    goto ask
)

goto check1

:jixu
set "DO1=1"
set "DO2=1"
set "DO3=1"
set "DO4=1"
set "DO5=1"
set "DO6=1"

:check1
if "!DO1!"=="1" goto do1
goto skip1
:do1
Echo                                        正在删除PDM临时文件(D:\DCPDM\TMP)
del "D:\DCPDM\TMP\*" /f/s/q/a
rd  /s /q "D:\DCPDM\TMP\"
md  "D:\DCPDM\TMP"
:skip1

if "!DO2!"=="1" goto do2
goto skip2
:do2
Echo                                        正在删除CAD临时文件(Local Settings\Temp)
del "%userprofile%\Local Settings\Temp\*" /f/s/q/a
rd  /s /q "%userprofile%\Local Settings\Temp\"
md  "%userprofile%\Local Settings\Temp"
:skip2

if "!DO3!"=="1" goto do3
goto skip3
:do3
Echo                                        正在删除SolidWorks临时文件(TempSW备份目录)
del "%userprofile%\Local Settings\TempSW备份目录\*" /f/s/q/a
rd  /s /q "%userprofile%\Local Settings\TempSW备份目录\"
md  "%userprofile%\Local Settings\TempSW备份目录"
:skip3

if "!DO4!"=="1" goto do4
goto skip4
:do4
Echo                                        正在删除用户临时文件
del "%TEMP%\*" /f/s/q/a
rd  /s /q "%TEMP%"
md  "%TEMP%"
:skip4

if "!DO5!"=="1" goto do5
goto skip5
:do5
Echo                                        正在删除Chrome浏览器缓存
del "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache\*" /f/s/q/a
rd  /s /q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache"
del "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache\*" /f/s/q/a
rd  /s /q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache"
Echo                                        正在删除Edge浏览器缓存
del "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache\*" /f/s/q/a
rd  /s /q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache"
del "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache\*" /f/s/q/a
rd  /s /q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache"
:skip5

if "!DO6!"=="1" goto do6
goto skip6
:do6
Echo                                        正在清空回收站
PowerShell -Command "Clear-RecycleBin -Force -ErrorAction SilentlyContinue"
:skip6

Echo.
echo.
Echo.                 ==========================================================================
ECHO.
echo                                        临时文件清理完成，请按提示继续操作
ECHO.
Echo.                 ==========================================================================
Echo.
echo.
pause
exit

:done
endlocal
exit
