' ******************************************************************************
' 这是solidworks2018的宏代码文件。
' 250604获取当前打开工程图文件名
' 250605处理日志文件名为空问题
' 250606增加文档打开判断逻辑，非工程图则退出
' 250611更换log地址
' 260512增加质心显示并导出功能
' ******************************************************************************

Dim swApp As Object
Dim Part As Object
Dim networkPath As String
Dim logFilePath As String ' 定义日志文件路径
Dim logFileName As String ' 定义日志文件名

Sub Main()
    On Error Resume Next ' 添加错误处理以避免未响应情况
    
    ' 初始化 SolidWorks 应用程序对象和活动文档对象
    Set swApp = Application.SldWorks
    If swApp Is Nothing Then
        MsgBox "SolidWorks 应用程序未打开", vbExclamation, "错误"
        Exit Sub
    End If
    
    Set Part = swApp.ActiveDoc
    If Part Is Nothing Then
        MsgBox "没有打开的活动文档", vbExclamation, "错误"
        Exit Sub
    End If
    
    ' 检查当前活动文档格式
    Dim docType As Long
    docType = Part.GetType
    
    ' 判断是否为 SLDASM 或 SLDPRT 格式
    If docType = swDocASSEMBLY Or docType = swDocPART Then
        MsgBox "当前打开文件格式不支持此命令，请在工程图窗口中运行此命令", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If
    
    InitializeEnvironment
    CreateNetworkFolder
    ShowCenterOfMassInAllViews
    ProcessDocument
End Sub

Private Sub InitializeEnvironment()
    networkPath = "\\192.168.160.2\生产管理部3d\3D 资料\01-设计一课确认图\DWG-TEMP\" & Environ("Computername") & "\"
    ' 初始化日志文件路径和文件名
    logFilePath = "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\Version control\VBA FOR SW\LOG\DWG\"
    logFileName = Format(Now(), "yyyy-mm-dd") & "_DWGLOG.TXT"
    ' 初始化视图
    Part.ViewZoomtofit2
    Part.GraphicsRedraw2
End Sub

Private Sub CreateNetworkFolder()
    On Error Resume Next
    Dim fso As Object
    Set fso = CreateObject("Scripting.FileSystemObject")
    If Not fso.FolderExists(networkPath) Then
        fso.CreateFolder networkPath
    End If
    ' 创建日志文件夹
    If Not fso.FolderExists(logFilePath) Then
        fso.CreateFolder logFilePath
    End If
    Set fso = Nothing
End Sub

Private Sub ShowCenterOfMassInAllViews()
    On Error Resume Next
    Dim swModelDocExt As Object
    Dim views As Variant
    Dim vv As Variant
    Dim sheetIdx As Long
    Dim viewIdx As Long
    Dim swView As Object
    Dim viewName As String
    Dim hasCOM As Boolean
    
    Set swModelDocExt = Part.Extension
    If Not swModelDocExt Is Nothing Then
        swModelDocExt.SetUserPreferenceToggle swDisplayAllAnnotations, swDetailingNoOptionSpecified, True
        swModelDocExt.SetUserPreferenceToggle swDisplayCenterOfMass, swDetailingNoOptionSpecified, True
    End If
    
    views = Part.GetViews
    If IsEmpty(views) Then Exit Sub
    
    hasCOM = False
    
    For sheetIdx = LBound(views) To UBound(views)
        vv = views(sheetIdx)
        If Not IsEmpty(vv) Then
            For viewIdx = 1 To UBound(vv)
                Set swView = vv(viewIdx)
                If Not swView Is Nothing Then
                    viewName = swView.Name
                    Part.ActivateView viewName
                    If UnblankCenterOfMassInView(swView) Then
                        hasCOM = True
                    End If
                End If
            Next viewIdx
        End If
    Next sheetIdx
    
    If hasCOM Then
        Part.Rebuild swRebuildAll
        Part.GraphicsRedraw2
    End If
End Sub

Private Function UnblankCenterOfMassInView(swView As Object) As Boolean
    On Error Resume Next
    Dim viewName As String
    Dim swRefDoc As Object
    Dim modelTitle As String
    Dim viewNumStr As String
    Dim comName As String
    Dim boolstatus As Boolean
    Dim i As Long
    
    viewName = swView.Name
    modelTitle = ""
    
    Set swRefDoc = swView.ReferencedDocument
    If Not swRefDoc Is Nothing Then
        modelTitle = swRefDoc.GetTitle
        If InStrRev(modelTitle, ".") > 0 Then
            modelTitle = Left(modelTitle, InStrRev(modelTitle, ".") - 1)
        End If
    End If
    
    If modelTitle = "" Then
        UnblankCenterOfMassInView = False
        Exit Function
    End If
    
    viewNumStr = ""
    For i = Len(viewName) To 1 Step -1
        If IsNumeric(Mid(viewName, i, 1)) Then
            viewNumStr = Mid(viewName, i, 1) & viewNumStr
        Else
            Exit For
        End If
    Next i
    
    If viewNumStr <> "" Then
        comName = "质心 (COM)@" & modelTitle & "-" & viewNumStr & "@" & viewName
        boolstatus = Part.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
    End If
    
    If Not boolstatus Then
        comName = "质心 (COM)@" & modelTitle & "@" & viewName
        boolstatus = Part.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
    End If
    
    If Not boolstatus And viewNumStr <> "" Then
        comName = "CenterOfMass (COM)@" & modelTitle & "-" & viewNumStr & "@" & viewName
        boolstatus = Part.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
    End If
    
    If Not boolstatus Then
        comName = "CenterOfMass (COM)@" & modelTitle & "@" & viewName
        boolstatus = Part.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
    End If
    
    If boolstatus Then
        Part.UnBlankRefGeom
        UnblankCenterOfMassInView = True
    Else
        UnblankCenterOfMassInView = False
    End If
    
    Part.ClearSelection2 True
End Function

Private Sub ProcessDocument()
    On Error Resume Next
    Dim baseName As String, fso As Object
    Dim dwgPath As String, sldPath As String
    Dim newName As String, i As Integer
    Dim logContent As String ' 定义日志内容
    Dim saveSuccess As Boolean ' 定义保存成功标志
    Dim finalFileName As String ' 实际保存的文件名
    Set fso = CreateObject("Scripting.FileSystemObject")
    
    ' 使用当前文档名称作为基础名称
    If Part.GetTitle <> "" Then
        baseName = Part.GetTitle
        ' 如果文档名包含扩展名，则去除扩展名
        If InStrRev(baseName, ".") > 0 Then
            baseName = Left(baseName, InStrRev(baseName, ".") - 1)
        End If
        ' 获取 " - " 前面的内容
        Dim dashPos As Integer
        dashPos = InStr(baseName, " - ")
        If dashPos > 0 Then
            baseName = Left(baseName, dashPos - 1)
        End If
    Else
        baseName = "未命名"
    End If
    saveSuccess = False
    finalFileName = ""
    
    ' 删除一天前的文件
    Dim folder As Object, file As Object
    If fso.FolderExists(networkPath) Then
        Set folder = fso.GetFolder(networkPath)
        For Each file In folder.Files
            If file.DateLastModified < Date Then
                file.Delete True
            End If
        Next file
        Set folder = Nothing
    End If
    
    Part.ViewZoomtofit2
    Part.GraphicsRedraw2
    Part.Save4 True, False, Nothing
    Part.Rebuild swRebuildAll
    Part.GraphicsRedraw2
    
    ' DWG 处理
    dwgPath = networkPath & baseName & ".DWG"
    If Not fso.FileExists(dwgPath) Then
        Part.SaveAs3 dwgPath, 0, 0
        saveSuccess = True
        finalFileName = baseName & ".DWG"
        MsgBox "DWG文件已保存至：" & vbCrLf & dwgPath, vbInformation, "技术开发二部提醒您"
    Else
        For i = 1 To 99
            newName = baseName & "-" & Format(i, "00")
            dwgPath = networkPath & newName & ".DWG"
            If Not fso.FileExists(dwgPath) Then
                Part.SaveAs3 dwgPath, 0, 0
                saveSuccess = True
                finalFileName = newName & ".DWG"
                MsgBox "存在同名图纸，已保存为新名称至：" & vbCrLf & dwgPath, vbInformation, "技术开发二部提醒您"
                Exit For
            End If
        Next i
    End If
    
    ' 构建日志内容
    If saveSuccess Then
        logContent = Now() & "," & Environ("Username") & "," & finalFileName & "," & "图纸导出成功[C]" & vbCrLf
    Else
        logContent = Now() & "," & Environ("Username") & "," & baseName & "," & "图纸导出失败[C]" & vbCrLf
    End If
    
    ' 写入日志
    Call WriteLog(logContent)
    
    ' 最终视图调整
    Part.ViewZoomtofit2
    Part.GraphicsRedraw2
    CleanUp
    Set fso = Nothing
End Sub

Private Sub WriteLog(logContent As String)
    On Error Resume Next
    Dim fso As Object
    Dim logFile As Object
    Set fso = CreateObject("Scripting.FileSystemObject")
    
    ' 创建日志文件（如果不存在）
    If Not fso.FileExists(logFilePath & logFileName) Then
        Set logFile = fso.CreateTextFile(logFilePath & logFileName, True)
    End If
    
    ' 追加日志内容
    Set logFile = fso.OpenTextFile(logFilePath & logFileName, 8, True) ' 8表示追加模式
    logFile.WriteLine logContent
    logFile.Close
    Set logFile = Nothing
    Set fso = Nothing
End Sub

Private Sub CleanUp()
    Set Part = Nothing
    Set swApp = Nothing
End Sub


