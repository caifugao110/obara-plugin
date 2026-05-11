' **************************************************************************
' 这是solidworks2018的宏代码文件。
' 250611更换log地址
' **************************************************************************

Dim swApp As Object
Dim Part As Object
Dim boolstatus As Boolean
Dim longstatus As Long, longwarnings As Long
Dim fso As Object

Sub main()
    On Error GoTo ErrorHandler

    Set swApp = Application.SldWorks
    Set Part = swApp.ActiveDoc

    ' 检查是否保存文件
    If Part Is Nothing Then
        MsgBox "未检测到打开的文件。", vbExclamation
        Exit Sub
    End If

    Dim FilePath As String
    FilePath = Part.GetPathName()

    ' 如果文件未保存，提示保存
    If FilePath = vbNullString Then
        MsgBox "当前文件未保存，请先保存文件。", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If

    ' 获取文件类型
    Dim FileType As String
    FileType = Right(FilePath, Len(FilePath) - InStrRev(FilePath, "."))

    ' 判断文件格式是否为 sldasm
    If LCase(FileType) <> "sldasm" Then
        MsgBox "暂不支持 " & FileType & " 格式文件的导出。", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If

    ' 设置导出路径和文件名
    Dim ExportPath As String
    ExportPath = Replace(FilePath, FileType, "STEP")

    ' 处理文件覆盖确认
    If Dir(ExportPath) <> "" Then
        Dim UserChoice As Integer
        UserChoice = MsgBox("导出的文件名 " & ExportPath & " 已存在，是否覆盖？", _
                            vbYesNo + vbQuestion, _
                            "技术开发二部提醒您")
        
        If UserChoice = vbNo Then
            MsgBox "导出STEP操作已取消。", vbInformation, "技术开发二部提醒您"
            Exit Sub
        End If
    End If

    ' 设置 STEP 导出协议为 AP214
    swApp.SetUserPreferenceIntegerValue swUserPreferenceIntegerValue_e.swStepAP, 214

    ' 导出 STEP 文件
    longstatus = Part.SaveAs3(ExportPath, 0, 0)
    
    ' 写入日志
    Dim ExportResult As String
    If longstatus = 0 Then
        ExportResult = "确认图导出成功[S]"
    Else
        ExportResult = "确认图导出失败[S]"
    End If
    WriteLog Part.GetTitle(), ExportResult
    
    If longstatus = 0 Then
        MsgBox "STEP 文件导出成功！文件路径：" & ExportPath, vbInformation, "技术开发二部提醒您"
    Else
        MsgBox "STEP 文件导出失败，请检查相关设置。", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If

    ' 退出
    Exit Sub

ErrorHandler:
    MsgBox "发生错误: " & Err.Description, vbCritical
    Exit Sub
End Sub

' 写入日志函数
Sub WriteLog(FileName As String, ExportResult As String)
    On Error Resume Next
    
    ' 获取当前时间和用户名
    Dim CurrentTime As String
    CurrentTime = FormatDateTime(Now(), vbShortDate) & " " & FormatDateTime(Now(), vbLongTime)
    
    Dim UserName As String
    UserName = Environ("username")
    
    ' 日志内容
    Dim LogContent As String
    LogContent = CurrentTime & "," & UserName & "," & FileName & "," & ExportResult & vbCrLf
    
    ' 日志路径
    Dim LogBasePath As String
    LogBasePath = "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\Version control\VBA FOR SW\LOG\STEP"
    
    ' 日志文件名（基于当前日期）
    Dim LogFileName As String
    LogFileName = FormatDateTime(Now(), vbShortDate) & "_STEPLOG.TXT"
    LogFileName = Replace(LogFileName, "/", "-") ' 替换日期分隔符
    
    ' 完整日志路径
    Dim FullLogPath As String
    FullLogPath = LogBasePath & "\" & LogFileName
    
    ' 创建文件系统对象
    Set fso = CreateObject("Scripting.FileSystemObject")
    
    ' 检查目录是否存在，不存在则创建
    If Not fso.FolderExists(LogBasePath) Then
        fso.CreateFolder (LogBasePath)
    End If
    
    ' 打开或创建日志文件并写入内容
    Dim LogFile As Object
    Set LogFile = fso.OpenTextFile(FullLogPath, 8, True) ' 8=追加模式，True=创建如果不存在
    LogFile.WriteLine LogContent
    LogFile.Close
    
    Set fso = Nothing
End Sub

