' ******************************************************************************
' 这是solidworks2018的宏代码文件。
' 250606处理日志文件名为空问题
' 250611更换log地址
' 260512增加质心显示并导出功能
' 260514增加PAYLOAD数据写入功能
' ******************************************************************************

' 声明SolidWorks应用程序接口类型（早期绑定）
Dim swApp As SldWorks.SldWorks
Dim swDrawing As SldWorks.DrawingDoc
Dim swSheet As SldWorks.Sheet
Dim swModel As SldWorks.ModelDoc2
Dim currentAssembly As SldWorks.ModelDoc2

' 常量定义
Const TEMPLATE_PATH As String = "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\VBA FOR SW(不要拷贝不定期更新)\自动生图模板\template\ToCadPlusAssemblyTemplate.drwdot"
Const NETWORK_BASE_PATH As String = "\\192.168.160.2\生产管理部3d\3D 资料\01-设计一课确认图\DWG-TEMP\"
' 日志相关常量
Const LOG_BASE_PATH As String = "\\192.168.160.2\生产管理部3d\3D 资料\check\check27\Version control\VBA FOR SW\LOG\DWG"

Sub main()
    On Error GoTo ErrorHandler
    
    ' 初始化SolidWorks应用
    Set swApp = Application.SldWorks
    swApp.UserControl = True
    
    ' 获取当前活动文档并验证
    ValidateCurrentDocument
    
    ' 创建输出目录
    Dim outputPath As String
    outputPath = CreateOutputPath
    
    ' 创建新工程图文档
    CreateNewDrawing
    
    ' 添加视图
    AddMainView
    AddUnfoldedViews
    
    ' 视图设置
    ConfigureViewSettings
    
    ' 显示质心并打开注解显示
    ShowCenterOfMassInAllViews
    
    ' 写入质量属性到表格
    Dim massProps As Variant
    massProps = GetMassPropertiesFromAssembly(currentAssembly)
    If Not IsEmpty(massProps) Then
        If Not swModel Is Nothing Then
            WriteMassPropertiesToTable swModel, massProps
        Else
            MsgBox "swModel 对象为空，无法写入表格", vbCritical
        End If
    End If
    
    ' 保存文件
    SaveDocuments outputPath
    
    ' 清理资源
    CleanUp
    
    Exit Sub
    
ErrorHandler:
    MsgBox "错误 " & Err.Number & ": " & Err.Description, vbCritical
    CleanUp
End Sub

' 验证当前活动文档
Sub ValidateCurrentDocument()
    Set currentAssembly = swApp.ActiveDoc
    
    If currentAssembly Is Nothing Then
        Err.Raise vbObjectError + 1, , "没有打开任何文档！"
    End If
    
    If currentAssembly.GetType() <> swDocASSEMBLY Then
        Err.Raise vbObjectError + 2, , "当前文档不是装配体文件！请使用其他模板。"
    End If
    
    ' 检查文档是否已保存
    ' If InStr(currentAssembly.GetPathName(), "\\") = 0 Then
    '     Err.Raise vbObjectError + 3, , "请先保存装配体文件！"
    ' End If
End Sub

' 创建输出路径
Function CreateOutputPath() As String
    On Error GoTo PathError
    
    Dim userName As String
    userName = Environ("Computername")
    
    Dim fullPath As String
    fullPath = NETWORK_BASE_PATH & userName & "\"
    
    ' 创建目录（如果不存在）
    If Dir(fullPath, vbDirectory) = "" Then
        MkDir fullPath
    End If
    
    CreateOutputPath = fullPath
    Exit Function
    
PathError:
    Err.Raise vbObjectError + 4, , "无法创建输出路径：" & fullPath & vbNewLine & Err.Description
End Function

' 创建新工程图文档
Sub CreateNewDrawing()
    On Error GoTo DrawingError
    
    Const SHEET_WIDTH As Double = 3.576
    Const SHEET_HEIGHT As Double = 2.523
    
    ' 检查模板文件是否存在
    If Dir(TEMPLATE_PATH) = "" Then
        Err.Raise vbObjectError + 5, , "模板文件不存在：" & TEMPLATE_PATH
    End If
    
    ' 使用模板创建新工程图
    Set swModel = swApp.NewDocument(TEMPLATE_PATH, 12, SHEET_WIDTH, SHEET_HEIGHT)
    Set swDrawing = swModel
    
    ' 配置图纸属性
    Set swSheet = swDrawing.GetCurrentSheet()
    swSheet.SetProperties2 12, 13, 1, 1, False, SHEET_WIDTH, SHEET_HEIGHT, True
    swSheet.SetTemplateName TEMPLATE_PATH
    swSheet.ReloadTemplate True
    
    Exit Sub
    
DrawingError:
    Err.Raise vbObjectError + 6, , "创建工程图失败：" & Err.Description
End Sub

' 添加主模型视图
Sub AddMainView()
    On Error GoTo ViewError
    
    Dim myView As SldWorks.View
    Const X_POS As Double = 1.74507863225707
    Const Y_POS As Double = 1.27989000106076
    
    ' 隐藏所有类型
    swModel.SetUserPreferenceToggle swUserPreferenceToggle_e.swViewDisplayHideAllTypes, True
    
    ' 从前视方向创建基础视图
    Set myView = swModel.CreateDrawViewFromModelView3(currentAssembly.GetPathName(), "*前视", X_POS, Y_POS, 0)
    
    ' 激活视图进行操作
    swModel.Extension.SelectByID2 "工程图视图1", "DRAWINGVIEW", 0, 0, 0, False, 0, Nothing, 0
    swModel.ActivateView "工程图视图1"
    
    Exit Sub
    
ViewError:
    Err.Raise vbObjectError + 7, , "添加主视图失败：" & Err.Description
End Sub

' 添加多个展开视图
Sub AddUnfoldedViews()
    On Error GoTo UnfoldError
    
    Dim viewCoordinates As Variant
    Dim i As Integer
    
    ' 定义所有展开视图的坐标数组
    viewCoordinates = Array( _
        Array(1.74507863225707, 2.19034092379043), _
        Array(1.74507863225707, 0.311448573698615), _
        Array(0.405497975247164, 1.27989000106076), _
        Array(3.00347258278153, 1.27989000106076) _
    )
    
    ' 循环创建视图
    For i = 0 To UBound(viewCoordinates)
        swModel.ClearSelection2 True
        swModel.Extension.SelectByID2 "工程图视图1", "DRAWINGVIEW", 0, 0, 0, False, 0, Nothing, 0
        swModel.CreateUnfoldedViewAt3 viewCoordinates(i)(0), viewCoordinates(i)(1), 0, False
    Next i
    
    Exit Sub
    
UnfoldError:
    Err.Raise vbObjectError + 8, , "添加展开视图失败：" & Err.Description
End Sub

' 配置视图显示设置
Sub ConfigureViewSettings()
    On Error GoTo ConfigError
    
    ' 优化视图显示
    swModel.ViewZoomtofit2
    swModel.GraphicsRedraw2
    
    Exit Sub
    
ConfigError:
    Err.Raise vbObjectError + 9, , "配置视图设置失败：" & Err.Description
End Sub

' 显示所有视图中的质心并打开相关显示开关
Sub ShowCenterOfMassInAllViews()
    On Error Resume Next
    Dim swModelDocExt As Object
    Dim views As Variant
    Dim vv As Variant
    Dim sheetIdx As Long
    Dim viewIdx As Long
    Dim swView As Object
    Dim viewName As String
    Dim hasCOM As Boolean
    
    swModel.ForceRebuild3 True
    
    Set swModelDocExt = swModel.Extension
    If Not swModelDocExt Is Nothing Then
        swModelDocExt.SetUserPreferenceToggle swViewDisplayHideAllTypes, False
        swModelDocExt.SetUserPreferenceToggle swDisplayAllAnnotations, swDetailingNoOptionSpecified, True
        swModelDocExt.SetUserPreferenceToggle swDisplayCenterOfMass, swDetailingNoOptionSpecified, True
    End If
    
    views = swModel.GetViews
    If IsEmpty(views) Then Exit Sub
    
    hasCOM = False
    
    For sheetIdx = LBound(views) To UBound(views)
        vv = views(sheetIdx)
        If Not IsEmpty(vv) Then
            For viewIdx = 1 To UBound(vv)
                Set swView = vv(viewIdx)
                If Not swView Is Nothing Then
                    viewName = swView.Name
                    swView.DisplayCenterOfMass = True
                    swModel.ActivateView viewName
                    swView.ForceRebuild
                    
                    If UnblankCenterOfMassInView(swView) Then
                        hasCOM = True
                    End If
                End If
            Next viewIdx
        End If
    Next sheetIdx
    
    If hasCOM Then
        swModel.Rebuild swRebuildAll
        swModel.GraphicsRedraw2
    End If
End Sub

Function UnblankCenterOfMassInView(swView As Object) As Boolean
    On Error Resume Next
    Dim viewName As String
    Dim swRefDoc As Object
    Dim modelTitle As String
    Dim viewNumStr As String
    Dim comName As String
    Dim boolstatus As Boolean
    Dim found As Boolean
    Dim i As Long
    
    found = False
    viewName = swView.Name
    
    Set swRefDoc = swView.ReferencedDocument
    modelTitle = ""
    If Not swRefDoc Is Nothing Then
        modelTitle = swRefDoc.GetTitle
        If InStrRev(modelTitle, ".") > 0 Then
            modelTitle = Left(modelTitle, InStrRev(modelTitle, ".") - 1)
        End If
    End If
    
    viewNumStr = ""
    For i = Len(viewName) To 1 Step -1
        If IsNumeric(Mid(viewName, i, 1)) Then
            viewNumStr = Mid(viewName, i, 1) & viewNumStr
        Else
            Exit For
        End If
    Next i
    
    If modelTitle <> "" And viewNumStr <> "" Then
        comName = "质心 (COM)@" & modelTitle & "-" & viewNumStr & "@" & viewName
        boolstatus = swModel.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
        If boolstatus Then swModel.UnBlankRefGeom: found = True
    End If
    
    If Not found And modelTitle <> "" Then
        comName = "质心 (COM)@" & modelTitle & "@" & viewName
        boolstatus = swModel.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
        If boolstatus Then swModel.UnBlankRefGeom: found = True
    End If
    
    If Not found And modelTitle <> "" And viewNumStr <> "" Then
        comName = "CenterOfMass (COM)@" & modelTitle & "-" & viewNumStr & "@" & viewName
        boolstatus = swModel.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
        If boolstatus Then swModel.UnBlankRefGeom: found = True
    End If
    
    If Not found And modelTitle <> "" Then
        comName = "CenterOfMass (COM)@" & modelTitle & "@" & viewName
        boolstatus = swModel.Extension.SelectByID2(comName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
        If boolstatus Then swModel.UnBlankRefGeom: found = True
    End If
    
    If Not found Then
        If UnblankCOMRecursiveInView(swView, swView) Then found = True
    End If
    
    swModel.ClearSelection2 True
    UnblankCenterOfMassInView = found
End Function

Function UnblankCOMRecursiveInView(swView As Object, swParentFeat As Object) As Boolean
    On Error Resume Next
    Dim swFeat As Object
    Dim swNextFeat As Object
    Dim found As Boolean
    Dim featName As String
    Dim typeName As String
    Dim selName As String
    Dim bRet As Boolean
    
    found = False
    Set swFeat = swParentFeat.GetFirstSubFeature
    
    Do While Not swFeat Is Nothing
        featName = swFeat.Name
        typeName = swFeat.GetTypeName2
        
        If InStr(featName, "质心") > 0 Or InStr(typeName, "CenterOfMass") > 0 Then
            selName = featName & "@" & swView.Name
            bRet = swModel.Extension.SelectByID2(selName, "CENTEROFMASS", 0, 0, 0, False, 0, Nothing, 0)
            If bRet Then
                swModel.UnBlankRefGeom
                found = True
            End If
            
            bRet = swModel.Extension.SelectByID2(featName, "COM", 0, 0, 0, False, 0, Nothing, 0)
            If bRet Then
                swModel.UnBlankRefGeom
                found = True
            End If
        End If
        
        If UnblankCOMRecursiveInView(swView, swFeat) Then found = True
        
        Set swNextFeat = swFeat.GetNextSubFeature
        Set swFeat = swNextFeat
    Loop
    
    UnblankCOMRecursiveInView = found
End Function

' 保存文件
Sub SaveDocuments(outputPath As String)
    On Error GoTo SaveError
    
    Dim baseName As String
    baseName = GetFileNameWithoutExtension(currentAssembly.GetTitle())
    
    ' 保存为DWG格式（带冲突检测）
    SaveAsDWGWithConflictCheck outputPath, baseName
    
    Exit Sub
    
SaveError:
    Err.Raise vbObjectError + 10, , "保存文件失败：" & Err.Description
End Sub

' 智能保存DWG文件
Sub SaveAsDWGWithConflictCheck(folderPath As String, baseName As String)
    On Error GoTo DWGError
    
    Dim fso As Object
    Set fso = CreateObject("Scripting.FileSystemObject")
    
    ' 删除一天前的文件
    Dim folder As Object, file As Object
    If fso.FolderExists(folderPath) Then
        Set folder = fso.GetFolder(folderPath)
        For Each file In folder.Files
            If file.DateLastModified < Date Then
                file.Delete True
            End If
        Next file
        Set folder = Nothing
    End If
    
    Dim originalPath As String
    originalPath = folderPath & baseName & ".DWG"
    
    ' 检查文件是否存在
    If Not fso.FileExists(originalPath) Then
        swModel.SaveAs3 originalPath, 0, 0
        Call WriteLog(baseName & ".DWG", "图纸导出成功[E]")
        MsgBox "DWG文件已保存至：" & vbNewLine & originalPath, vbInformation, "技术开发二部提醒您"
        Exit Sub
    End If
    
    ' 查找可用序号
    Dim finalPath As String
    Dim newName As String
    Dim i As Integer
    For i = 1 To 99
        newName = baseName & "-" & Format(i, "00") & ".DWG"
        finalPath = folderPath & newName
        If Not fso.FileExists(folderPath & newName) Then
            swModel.SaveAs3 folderPath & newName, 0, 0
            Call WriteLog(newName, "图纸导出成功[E]")
            MsgBox "存在同名图纸，已保存为新名称至：" & vbNewLine & finalPath, vbInformation, "技术开发二部提醒您"
            Exit Sub
        End If
    Next i
    
    Call WriteLog(baseName & ".DWG", "图纸导出失败[E]")
    Err.Raise vbObjectError + 11, , "无法生成唯一文件名，请手动清理文件！"
    
    Exit Sub
    
DWGError:
    Call WriteLog(baseName & ".DWG", "图纸导出失败[E]")
    Err.Raise vbObjectError + 12, , "保存DWG文件失败：" & Err.Description
End Sub

' 写入日志文件
Sub WriteLog(dwgFileName As String, result As String)
    On Error GoTo LogError
    
    Dim fso As Object
    Set fso = CreateObject("Scripting.FileSystemObject")
    
    ' 格式化当前日期
    Dim currentDate As String
    currentDate = Format(Date, "yyyy-mm-dd")
    
    ' 创建日志文件夹（如果不存在）
    If Dir(LOG_BASE_PATH, vbDirectory) = "" Then
        MkDir LOG_BASE_PATH
    End If
    
    ' 拼接日志文件路径
    Dim logFilePath As String
    logFilePath = LOG_BASE_PATH & "\" & currentDate & "_DWGLOG.TXT"
    
    ' 打开日志文件（如果不存在则创建）
    Dim logFile As Object
    Set logFile = fso.OpenTextFile(logFilePath, 8, True)
    
    ' 写入日志内容
    Dim logContent As String
    logContent = Now() & "," & Environ("USERNAME") & "," & dwgFileName & "," & result & vbNewLine
    logFile.WriteLine logContent
    
    ' 关闭文件
    logFile.Close
    
    Exit Sub
    
LogError:
    MsgBox "日志记录失败：" & Err.Description, vbExclamation
End Sub

' 获取无扩展名文件名
Function GetFileNameWithoutExtension(fileName As String) As String
    Dim arr() As String
    arr = Split(fileName, ".")
    If UBound(arr) > 0 Then
        GetFileNameWithoutExtension = arr(0)
    Else
        GetFileNameWithoutExtension = fileName
    End If
End Function

' 获取质量属性（基于坐标系1）
Function GetMassPropertiesFromAssembly(assemblyModel As SldWorks.ModelDoc2) As Variant
    On Error GoTo ErrorHandler
    
    Dim swMathUtil As SldWorks.MathUtility
    Dim swMathTrans As SldWorks.MathTransform
    Dim swInverseTrans As SldWorks.MathTransform
    Dim swMathPoint As SldWorks.MathPoint
    Dim swComPointCustom As SldWorks.MathPoint
    
    Dim massValue As Double
    Dim com_custom As Variant
    Dim moi_custom(8) As Double
    
    Set swMathUtil = swApp.GetMathUtility
    
    Set swMathTrans = assemblyModel.Extension.GetCoordinateSystemTransformByName("坐标系1")
    If swMathTrans Is Nothing Then
        GetMassPropertiesFromAssembly = Empty
        Exit Function
    End If
    
    Set swInverseTrans = swMathTrans.Inverse()
    
    Dim vMassProps As Variant
    Dim nErrors As Long
    vMassProps = assemblyModel.Extension.GetMassProperties2(9, False, nErrors)
    
    massValue = vMassProps(5)
    
    Dim com_arr(2) As Double
    com_arr(0) = vMassProps(0)
    com_arr(1) = vMassProps(1)
    com_arr(2) = vMassProps(2)
    
    Set swMathPoint = swMathUtil.CreatePoint(com_arr)
    Set swComPointCustom = swMathPoint.MultiplyTransform(swInverseTrans)
    com_custom = swComPointCustom.ArrayData
    
    Dim I_global(2, 2) As Double
    I_global(0, 0) = vMassProps(6)
    I_global(0, 1) = -vMassProps(9)
    I_global(0, 2) = -vMassProps(10)
    I_global(1, 0) = -vMassProps(9)
    I_global(1, 1) = vMassProps(7)
    I_global(1, 2) = -vMassProps(11)
    I_global(2, 0) = -vMassProps(10)
    I_global(2, 1) = -vMassProps(11)
    I_global(2, 2) = vMassProps(8)
    
    Dim arrData As Variant
    arrData = swInverseTrans.ArrayData
    Dim R(2, 2) As Double
    Dim i As Integer, j As Integer, k As Integer
    For i = 0 To 2: For j = 0 To 2
        R(i, j) = arrData(j * 3 + i)
    Next j: Next i
    
    Dim temp(2, 2) As Double
    Dim I_local(2, 2) As Double
    For i = 0 To 2: For j = 0 To 2
        temp(i, j) = 0
        For k = 0 To 2: temp(i, j) = temp(i, j) + R(i, k) * I_global(k, j): Next k
    Next j: Next i
    
    For i = 0 To 2: For j = 0 To 2
        I_local(i, j) = 0
        For k = 0 To 2: I_local(i, j) = I_local(i, j) + temp(i, k) * R(j, k): Next k
    Next j: Next i
    
    moi_custom(0) = I_local(0, 0)
    moi_custom(4) = I_local(1, 1)
    moi_custom(8) = I_local(2, 2)
    
    Dim result As Variant
    ReDim result(6)
    result(0) = massValue
    result(1) = com_custom(0)
    result(2) = com_custom(1)
    result(3) = com_custom(2)
    result(4) = moi_custom(0)
    result(5) = moi_custom(4)
    result(6) = moi_custom(8)
    
    GetMassPropertiesFromAssembly = result
    Exit Function
    
ErrorHandler:
    GetMassPropertiesFromAssembly = Empty
End Function

' 向工程图表格写入质量属性数据
Sub WriteMassPropertiesToTable(drawingModel As Object, massProps As Variant)
    On Error GoTo ErrorHandler

    Dim swTableObj As Object
    Dim tableTitle As String
    Dim success As Boolean
    
    tableTitle = "总表1"

    If IsEmpty(massProps) Then
        MsgBox "质量属性数据为空", vbExclamation
        Exit Sub
    End If

    Set swTableObj = FindTableByName(drawingModel, tableTitle)
    If swTableObj Is Nothing Then
        MsgBox "未找到名为 '" & tableTitle & "' 的表格", vbExclamation
        Exit Sub
    End If

    success = WriteTableDataDirect(swTableObj, massProps)
    If Not success Then
        MsgBox "写入表格数据失败", vbExclamation
    End If

    Exit Sub

ErrorHandler:
    MsgBox "写入表格失败: " & Err.Description & vbNewLine & "错误行: " & Erl, vbCritical
End Sub

' SelectByID2(..., "GENERALTABLEFEAT", ...) 得到的是 GeneralTableFeature，不是 TableAnnotation；
' 后者才支持通过 Text 属性写入单元格。
Function ResolveToTableAnnotation(swObj As Object, ByVal tableName As String) As Object
    Dim tblCount As Long
    Dim vTables As Variant
    Dim i As Long
    Dim t As Object
    Dim rc As Long
    
    If swObj Is Nothing Then Exit Function
    
    On Error Resume Next
    rc = swObj.RowCount
    If Err.Number = 0 Then
        Set ResolveToTableAnnotation = swObj
        Exit Function
    End If
    Err.Clear
    
    tblCount = swObj.GetTableAnnotationCount
    If Err.Number = 0 And tblCount > 0 Then
        vTables = swObj.GetTableAnnotations
        If Err.Number = 0 Then
            If IsArray(vTables) Then
                For i = LBound(vTables) To UBound(vTables)
                    Set t = vTables(i)
                    If Not t Is Nothing Then
                        If StrComp(t.Title, tableName, vbTextCompare) = 0 Then
                            Set ResolveToTableAnnotation = t
                            Exit Function
                        End If
                    End If
                Next i
                For i = LBound(vTables) To UBound(vTables)
                    Set t = vTables(i)
                    If Not t Is Nothing Then
                        Set ResolveToTableAnnotation = t
                        Exit Function
                    End If
                Next i
                Exit Function
            Else
                Err.Clear
                Set t = Nothing
                On Error Resume Next
                Set t = vTables
                If Err.Number = 0 Then
                    If Not t Is Nothing Then
                        Set ResolveToTableAnnotation = t
                        Exit Function
                    End If
                End If
                Err.Clear
            End If
        End If
    End If
    Err.Clear
    Set ResolveToTableAnnotation = swObj
End Function

Function FindTableByName(drawingModel As Object, tableName As String) As Object
    Dim swTable As Object
    Dim swAnnotation As Object
    Dim swAnnotations As Variant
    Dim i As Integer
    Dim boolstatus As Boolean
    
    boolstatus = drawingModel.Extension.SelectByID2(tableName, "GENERALTABLEFEAT", 0, 0, 0, False, 0, Nothing, 0)
    If boolstatus Then
        On Error Resume Next
        Set swTable = drawingModel.SelectionManager.GetSelectedObject6(1, -1)
        On Error GoTo 0
        drawingModel.ClearSelection2 True
        If Not swTable Is Nothing Then
            Set FindTableByName = ResolveToTableAnnotation(swTable, tableName)
            Exit Function
        End If
    End If
    
    boolstatus = drawingModel.Extension.SelectByID2(tableName, "TABLE", 0, 0, 0, False, 0, Nothing, 0)
    If boolstatus Then
        On Error Resume Next
        Set swTable = drawingModel.SelectionManager.GetSelectedObject6(1, -1)
        On Error GoTo 0
        drawingModel.ClearSelection2 True
        If Not swTable Is Nothing Then
            Set FindTableByName = ResolveToTableAnnotation(swTable, tableName)
            Exit Function
        End If
    End If
    
    On Error Resume Next
    swAnnotations = drawingModel.GetAnnotations
    On Error GoTo 0
    
    If IsEmpty(swAnnotations) Then
        Set FindTableByName = Nothing
        Exit Function
    End If

    For i = LBound(swAnnotations) To UBound(swAnnotations)
        Set swAnnotation = swAnnotations(i)
        If Not swAnnotation Is Nothing Then
            On Error Resume Next
            Set swTable = swAnnotation.GetTableAnnotation
            On Error GoTo 0
            
            If Not swTable Is Nothing Then
                If StrComp(swTable.Title, tableName, vbTextCompare) = 0 Then
                    Set FindTableByName = ResolveToTableAnnotation(swTable, tableName)
                    Exit Function
                End If
            End If
        End If
    Next i
    
    Set FindTableByName = Nothing
End Function

' Payload Mass：装配体质量先截断小数（向 0 取整）+6 后写入
Private Function FormatPayloadMassForTable(ByVal rawMass As Variant) As String
    FormatPayloadMassForTable = CStr(CLng(Fix(CDbl(rawMass))) + 6&)
End Function

Private Function GetTableCellTextSafe(tbl As Object, rowIdx As Integer, colIdx As Integer) As String
    Dim s As String
    On Error Resume Next
    s = CStr(tbl.Text(rowIdx, colIdx))
    If Err.Number = 0 And Len(Trim$(s)) > 0 Then GetTableCellTextSafe = s: Exit Function
    Err.Clear
    s = CStr(tbl.displayedText(rowIdx, colIdx))
    If Err.Number = 0 Then GetTableCellTextSafe = s Else GetTableCellTextSafe = "": Err.Clear
End Function

' 占位「Mass」所在格即首行数值格；向下连续 7 格对应质量属性（与合并标题行后的 API 行号无关）
Private Function FindPayloadMassPlaceholderCell(tbl As Object, ByRef outRow As Integer, ByRef outCol As Integer) As Boolean
    Dim r As Integer, c As Integer
    Dim rc As Long, cc As Long
    Dim s As String
    
    FindPayloadMassPlaceholderCell = False
    On Error Resume Next
    rc = tbl.RowCount
    cc = tbl.ColumnCount
    If Err.Number <> 0 Then Err.Clear: Exit Function
    If rc < 1 Or cc < 1 Then Exit Function
    
    For r = 0 To rc - 1
        For c = 0 To cc - 1
            s = Trim$(GetTableCellTextSafe(tbl, r, c))
            If StrComp(s, "Mass", vbTextCompare) = 0 Then
                outRow = r
                outCol = c
                FindPayloadMassPlaceholderCell = True
                Exit Function
            End If
        Next c
    Next r
End Function

Private Function TrySetTableCellText(tbl As Object, rowIdx As Integer, colIdx As Integer, cellText As String) As Boolean
    On Error Resume Next
    tbl.Text(rowIdx, colIdx) = cellText
    If Err.Number = 0 Then TrySetTableCellText = True: GoTo done
    Err.Clear
    
    tbl.Text rowIdx, colIdx, cellText
    If Err.Number = 0 Then TrySetTableCellText = True: GoTo done
    Err.Clear
    
    tbl.Text2 rowIdx, colIdx, False, cellText
    If Err.Number = 0 Then TrySetTableCellText = True: GoTo done
    Err.Clear
    
    tbl.Text2 rowIdx, colIdx, True, cellText
    If Err.Number = 0 Then TrySetTableCellText = True: GoTo done
    Err.Clear
    
    TrySetTableCellText = False
done:
    Err.Clear
End Function

Private Function CellNumericTextMatches(tbl As Object, rowIdx As Integer, colIdx As Integer, ByVal expectedText As String) As Boolean
    Dim got As String
    got = Trim$(GetTableCellTextSafe(tbl, rowIdx, colIdx))
    If Len(got) = 0 Then Exit Function
    CellNumericTextMatches = (Abs(Val(got) - Val(expectedText)) < 0.0000001)
End Function

Private Function TryWritePayloadTableCell(tbl As Object, rowIdx As Integer, colIdx As Integer, cellText As String) As Boolean
    If Not TrySetTableCellText(tbl, rowIdx, colIdx, cellText) Then Exit Function
    TryWritePayloadTableCell = CellNumericTextMatches(tbl, rowIdx, colIdx, cellText)
End Function

Private Function ResolvePayloadValueOriginCell(tbl As Object, massProps As Variant, ByRef baseRow As Integer, ByRef baseCol As Integer) As Boolean
    Dim probe As String
    Dim pairs As Variant
    Dim k As Integer
    Dim br As Integer, bc As Integer
    
    ResolvePayloadValueOriginCell = False
    probe = FormatPayloadMassForTable(massProps(0))
    
    If FindPayloadMassPlaceholderCell(tbl, baseRow, baseCol) Then
        If TryWritePayloadTableCell(tbl, baseRow, baseCol, probe) Then
            ResolvePayloadValueOriginCell = True
            Exit Function
        End If
    End If
    
    pairs = Array(1, 1, 0, 1, 2, 2, 2, 1, 1, 2, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2)
    For k = 0 To UBound(pairs) Step 2
        br = pairs(k)
        bc = pairs(k + 1)
        If TryWritePayloadTableCell(tbl, br, bc, probe) Then
            baseRow = br
            baseCol = bc
            ResolvePayloadValueOriginCell = True
            Exit Function
        End If
    Next k
End Function

Function WriteTableDataDirect(swTable As Object, massProps As Variant) As Boolean
    Dim i As Integer
    Dim baseRow As Integer
    Dim baseCol As Integer
    Dim cellVal As Double
    
    WriteTableDataDirect = False
    If swTable Is Nothing Then Exit Function
    
    If Not ResolvePayloadValueOriginCell(swTable, massProps, baseRow, baseCol) Then Exit Function
    
    For i = 1 To 6
        cellVal = CDbl(massProps(i))
        If i <= 3 Then cellVal = Abs(cellVal)
        If Not TryWritePayloadTableCell(swTable, baseRow + i, baseCol, Format(cellVal, "0.000")) Then Exit Function
    Next i
    
    WriteTableDataDirect = True
End Function

' 清理资源
Sub CleanUp()
    On Error Resume Next
    Set swSheet = Nothing
    Set swDrawing = Nothing
    Set swModel = Nothing
    Set currentAssembly = Nothing
    swApp.ActivateDoc2 currentAssembly.GetTitle(), False, 0
End Sub




