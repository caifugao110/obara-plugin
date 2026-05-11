' **************************************************************************
' 这是solidworks2018的宏代码文件。
' 250611更换log地址
' **************************************************************************

Dim swApp As Object
Dim Part As Object
Dim boolstatus As Boolean
Dim longstatus As Long, longwarnings As Long

Sub main()
    On Error GoTo ErrorHandler

    Set swApp = Application.SldWorks
    Set Part = swApp.ActiveDoc

    ' 检查是否打开文件
    If Part Is Nothing Then
        MsgBox "未检测到打开的文件。", vbExclamation
        Exit Sub
    End If

    Dim FilePath As String
    FilePath = Part.GetPathName()

    ' 检查文件是否保存
    If FilePath = vbNullString Then
        MsgBox "当前文件未保存，请先保存文件。", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If

    ' 验证文件类型
    Dim FileType As String
    FileType = Right(FilePath, Len(FilePath) - InStrRev(FilePath, "."))
    If LCase(FileType) <> "sldasm" Then
        MsgBox "暂不支持 " & FileType & " 格式文件的导出。", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If

    ' 提取纯净文件名（包含后缀）
    Dim FileNameWithExtension As String
    FileNameWithExtension = Mid(FilePath, InStrRev(FilePath, "\") + 1)

    ' 提取纯净文件名（不包含后缀）
    Dim FileNameWithoutExtension As String
    FileNameWithoutExtension = Left(FileNameWithExtension, InStrRev(FileNameWithExtension, ".") - 1)

    ' 设置固定导出路径
    Const EXPORT_FOLDER As String = "\\192.168.160.2\生产管理部3d\3D 资料\设计一课3D资料\03-SV GUN STEP\"
    Dim ExportPath As String
    ExportPath = EXPORT_FOLDER & FileNameWithoutExtension & ".STEP"

    ' 处理文件覆盖确认
    If Dir(ExportPath) <> "" Then
        Dim UserChoice As Integer
        UserChoice = MsgBox("文件 " & FileNameWithoutExtension & ".STEP 已存在，是否覆盖？", _
                            vbYesNo + vbQuestion, _
                            "技术开发二部提醒您")
        
        If UserChoice = vbNo Then
            MsgBox "导出STEP操作已取消。", vbInformation, "技术开发二部提醒您"
            
            ' 写入日志 - 导出取消
            WriteLog "取消", FileNameWithExtension
            Exit Sub
        End If
    End If

    ' 配置STEP参数
    swApp.SetUserPreferenceIntegerValue swUserPreferenceIntegerValue_e.swStepAP, 214

    ' 执行文件导出
    longstatus = Part.SaveAs3(ExportPath, 0, 0)
    
    ' 写入日志
    If longstatus = 0 Then
        MsgBox "STEP 文件导出成功！" & vbCrLf & "路径：" & ExportPath, _
               vbInformation, _
               "技术开发二部提醒您"
        WriteLog "设计导出成功[T]", FileNameWithExtension
    Else
        MsgBox "STEP 文件导出失败[T]，错误代码：" & longstatus, _
               vbExclamation, _
               "技术开发二部提醒您"
        WriteLog "设计导出失败", FileNameWithExtension
    End If

    Exit Sub

ErrorHandler:
    MsgBox "发生错误: " & Err.Description, vbCritical
    WriteLog "设计导出错误", FileNameWithExtension
    Exit Sub

End Sub

' 日志记录函数
Sub WriteLog(Result As String, FileName As String)
    Dim LogFilePath As String
    Dim CurrentDateStr As String
    Dim CurrentTimeStr As String
    Dim UserName As String
    Dim LogContent As String
    Dim fso As Object
    Dim LogFile As Object
    
    ' 获取当前日期和时间
    CurrentDateStr = Format(Now, "yyyy-mm-dd")
    CurrentTimeStr = Format(Now, "yyyy-mm-dd hh:mm:ss")
    
    ' 获取当前用户名
    UserName = Environ("USERNAME")
    
    ' 构造日志内容，最后增加两个换行符
    LogContent = CurrentTimeStr & "," & UserName & "," & FileName & "," & Result & vbCrLf & vbCrLf
    
    ' 构造日志文件路径
    LogFilePath = "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\Version control\VBA FOR SW\LOG\STEP\" & CurrentDateStr & "_STEPLOG.TXT"
    
    ' 创建文件系统对象
    Set fso = CreateObject("Scripting.FileSystemObject")
    
    ' 确保日志文件夹存在
    If Not fso.FolderExists(fso.GetParentFolderName(LogFilePath)) Then
        fso.CreateFolder fso.GetParentFolderName(LogFilePath)
    End If
    
    ' 打开或创建日志文件
    If fso.FileExists(LogFilePath) Then
        Set LogFile = fso.OpenTextFile(LogFilePath, 8, True) ' 8表示追加模式
    Else
        Set LogFile = fso.CreateTextFile(LogFilePath, True)
    End If
    
    ' 写入日志
    LogFile.Write LogContent
    
    ' 关闭文件
    LogFile.Close
    
    Set fso = Nothing
    Set LogFile = Nothing
End Sub

