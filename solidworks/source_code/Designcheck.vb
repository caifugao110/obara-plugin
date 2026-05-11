'----------------------------------------------------------------------
' 这是solidworks2018的宏代码文件。
' 在此维护需要检查的关键字（不区分大小写）。
' 追加示例：
'   ReDim Preserve keywords(0 To UBound(keywords) + 1)
'   keywords(UBound(keywords)) = "temp"
'----------------------------------------------------------------------

Option Explicit

Private Sub GetKeywords(ByRef keywords() As String)
    Dim i As Long
    ReDim keywords(0 To 104)
    keywords(0) = "mod"
    keywords(1) = "new"
    keywords(2) = "--"
    keywords(3) = "装配体"
    keywords(4) = "assembly"
    keywords(5) = "GAI"
    For i = 1 To 9
        keywords(5 + i) = "X" & CStr(i)
    Next i
End Sub

' 将完整报告按行拆成多段，避免单条 MsgBox 过长被系统截断（不依赖外部程序）。
Private Function SplitReportIntoChunks(ByVal fullReport As String, ByVal maxChars As Long) As Collection
    Dim chunks As New Collection
    Dim lines() As String
    Dim i As Long
    Dim buf As String
    Dim ln As String
    Dim lineSep As String

    lineSep = vbCrLf
    lines = Split(fullReport, vbCrLf)
    buf = ""

    For i = LBound(lines) To UBound(lines)
        ln = lines(i)

        Do While Len(ln) > maxChars
            If Len(buf) > 0 Then
                chunks.Add buf
                buf = ""
            End If
            chunks.Add Left$(ln, maxChars)
            ln = Mid$(ln, maxChars + 1)
        Loop

        If Len(buf) = 0 Then
            buf = ln
        ElseIf Len(buf) + Len(lineSep) + Len(ln) <= maxChars Then
            buf = buf & lineSep & ln
        Else
            chunks.Add buf
            buf = ln
        End If
    Next i

    If Len(buf) > 0 Then chunks.Add buf
    Set SplitReportIntoChunks = chunks
End Function

Private Function SplitReportByRecords(ByVal fullReport As String, ByVal recordsPerPage As Long, ByRef totalPages As Long) As Collection
    Dim chunks As New Collection
    Dim lines() As String
    Dim i As Long
    Dim lineSep As String
    Dim currentChunk As String
    Dim recordCount As Long

    lineSep = vbCrLf
    lines = Split(fullReport, vbCrLf)
    currentChunk = ""
    recordCount = 0
    totalPages = 0

    For i = LBound(lines) To UBound(lines)
        If Len(currentChunk) > 0 Then
            currentChunk = currentChunk & lineSep
        End If
        currentChunk = currentChunk & lines(i)

        If InStr(lines(i), "  序号 : ") > 0 Then
            recordCount = recordCount + 1
            If recordCount Mod recordsPerPage = 0 Then
                chunks.Add currentChunk
                currentChunk = ""
            End If
        End If
    Next i

    If Len(currentChunk) > 0 Then
        chunks.Add currentChunk
    End If

    totalPages = chunks.Count
    Set SplitReportByRecords = chunks
End Function

Private Sub ShowDesignCheckFailPaged(ByVal fullReport As String, ByVal recordCount As Long)
    Const RECORDS_PER_PAGE As Long = 3
    Const TITLE_BAR As String = "技术开发二部提醒您"

    Dim allChunks As New Collection
    Dim page As Long
    Dim pages As Long
    Dim msg As String
    Dim footer As String
    Dim lineSep As String
    Dim response As VbMsgBoxResult
    Dim btnType As Long

    lineSep = vbCrLf

    If recordCount > RECORDS_PER_PAGE Then
        Dim recordChunks As Collection
        Dim recordPages As Long
        Set recordChunks = SplitReportByRecords(fullReport, RECORDS_PER_PAGE, recordPages)
        Dim recordChunk As Variant
        For Each recordChunk In recordChunks
            allChunks.Add recordChunk
        Next recordChunk
    Else
        allChunks.Add fullReport
    End If
    
    pages = allChunks.Count
    
    If pages > 1 Then
        For page = 1 To pages
            msg = allChunks(page)
            footer = lineSep & lineSep & String(16, "-") & " 第 " & CStr(page) & " / " & CStr(pages) & " 页 " & String(16, "-")
            footer = footer & lineSep & "共 " & CStr(recordCount) & " 条记录。"
            
            If page < pages Then
                msg = msg & footer & lineSep & "[确定] = 下一页  [取消] = 退出"
                btnType = vbExclamation + vbOKCancel
                response = MsgBox(msg, btnType, TITLE_BAR)
                If response = vbCancel Then Exit For
            Else
                msg = msg & footer
                btnType = vbExclamation + vbOKOnly
                MsgBox msg, btnType, TITLE_BAR
            End If
        Next page
    Else
        msg = fullReport & lineSep & String(42, "-") & lineSep & "共 " & CStr(recordCount) & " 条记录。"
        MsgBox msg, vbExclamation, TITLE_BAR
    End If
End Sub

Private Function FileNameFromPath(ByVal fullPath As String) As String
    Dim p As Long
    If Len(fullPath) = 0 Then
        FileNameFromPath = ""
        Exit Function
    End If
    p = InStrRev(fullPath, "\")
    If p = 0 Then p = InStrRev(fullPath, "/")
    If p > 0 Then
        FileNameFromPath = Mid$(fullPath, p + 1)
    Else
        FileNameFromPath = fullPath
    End If
End Function

Private Function IsAssemblyPath(ByVal fullPath As String) As Boolean
    Dim ext As String
    ext = LCase$(Right$(fullPath, 7))
    IsAssemblyPath = (ext = ".sldasm")
End Function

' 不区分大小写：统一转成小写再 InStr
' 排除"model"关键词，但保留"mod"
' X1-X9如果前面有数字则忽略
' 仅排除X2C这一个
' X前面如果是字母S或者W也要排除
' 包含NEW时排除特定名称"CON-C0039-NEW"
Private Function MatchKeywords(ByVal txt As String, ByRef keywords() As String) As String
    Dim i As Long
    Dim low As String
    Dim pos As Long
    Dim keyLow As String
    Dim prevChar As String
    low = LCase$(txt)
    For i = LBound(keywords) To UBound(keywords)
        If Len(keywords(i)) > 0 Then
            If keywords(i) = "mod" And InStr(1, low, "model", vbBinaryCompare) > 0 Then
            ElseIf keywords(i) = "new" And InStr(1, LCase$(txt), "con-c0039-new", vbBinaryCompare) > 0 Then
                ' 排除特定名称"CON-C0039-NEW"
            Else
                keyLow = LCase$(keywords(i))
                pos = InStr(1, low, keyLow, vbBinaryCompare)
                If pos > 0 Then
                    ' 检查是否是 X1-X9 这类关键词
                    If Left$(keyLow, 1) = "x" And Len(keyLow) = 2 Then
                        Dim secondChar As String
                        secondChar = Mid$(keyLow, 2, 1)
                        If secondChar >= "1" And secondChar <= "9" Then
                            ' 是 X1-X9，检查前面是否有数字、S或W
                            If pos > 1 Then
                                prevChar = Mid$(low, pos - 1, 1)
                                If (prevChar >= "0" And prevChar <= "9") Or prevChar = "s" Or prevChar = "w" Then
                                    ' 前面有数字、S或W，跳过这个匹配
                                    GoTo NextKeyword
                                End If
                            End If
                            ' 仅排除X2C这一个
                            If keyLow = "x2" And pos + 2 <= Len(low) Then
                                If Mid$(low, pos, 3) = "x2c" Then
                                    GoTo NextKeyword
                                End If
                            End If
                        End If
                    End If
                    ' 其他关键词或没有前缀数字，匹配成功
                    MatchKeywords = keywords(i)
                    Exit Function
                End If
            End If
        End If
NextKeyword:
    Next i
    MatchKeywords = ""
End Function

Private Function BuildHitDetail(ByVal fileNm As String, ByVal instName As String, ByVal docTitle As String, ByRef keywords() As String) As String
    Dim parts As String
    Dim hFile As String
    parts = ""

    hFile = MatchKeywords(fileNm, keywords)

    If Len(hFile) > 0 Then
        parts = parts & "关键字：" & hFile
    End If

    BuildHitDetail = parts
End Function

' 父级子装配体被抑制时，子零件的 GetSuppression2 仍可能为“已解析”，必须沿装配树向上判断。
Private Function GetComponentParentSafe(ByVal swComp As Component2) As Component2
    On Error Resume Next
    Set GetComponentParentSafe = swComp.GetParent
    If Err.Number <> 0 Then
        Err.Clear
        Set GetComponentParentSafe = Nothing
    End If
    On Error GoTo 0
End Function

' 判断“这一级”引用是否被抑制（压缩）。
' 要点（SW2018）：
'   1) Component2.IsSuppressed 与装配树/界面一致，优先采用。
'   2) GetSuppression2 的返回值按文档为 swSuppressionState_e：抑制 = 2（swComponentSuppressed）。
'   3) 旧接口 GetSuppression 使用 swComponentSuppressionState_e：抑制 = 0（swComponentSuppressed），
'      完全解析 = 2（swComponentFullyResolved）。把 GetSuppression=2 当成“抑制”会把正常件整批误判跳过。
'   勿用 GetModelDoc2 是否为空推断抑制。
Private Function CompSuppressionState2(ByVal swComp As Component2) As Long
    Const STATE_UNAVAILABLE As Long = &H7FFFFFFF
    On Error Resume Next
    CompSuppressionState2 = swComp.GetSuppression2()
    If Err.Number <> 0 Then
        Err.Clear
        CompSuppressionState2 = STATE_UNAVAILABLE
    End If
    On Error GoTo 0
End Function

Private Function IsThisReferenceSuppressed(ByVal swComp As Component2) As Boolean
    Const SUPPRESSED_VIA_GETSUPPRESSION2 As Long = 2   ' GetSuppression2：swComponentSuppressed
    Const SUPPRESSED_VIA_GETSUPPRESSION As Long = 0    ' GetSuppression：swComponentSuppressionState_e.swComponentSuppressed
    Const STATE_UNAVAILABLE As Long = &H7FFFFFFF

    Dim st As Long
    Dim bSup As Boolean

    IsThisReferenceSuppressed = False

    On Error Resume Next
    bSup = swComp.IsSuppressed
    If Err.Number = 0 Then
        If bSup Then
            IsThisReferenceSuppressed = True
            On Error GoTo 0
            Exit Function
        End If
        ' IsSuppressed=False 时仍用状态码复核（与多配置/阵列等边界情况兼容）
    End If
    Err.Clear
    On Error GoTo 0

    st = CompSuppressionState2(swComp)
    If st = SUPPRESSED_VIA_GETSUPPRESSION2 Then
        IsThisReferenceSuppressed = True
        Exit Function
    End If

    ' GetSuppression2 不可用或返回 -1（多配置场景偶发）时，用旧接口的枚举值判断
    If st <> STATE_UNAVAILABLE And st <> -1 Then Exit Function

    On Error Resume Next
    st = swComp.GetSuppression()
    If Err.Number = 0 And st = SUPPRESSED_VIA_GETSUPPRESSION Then IsThisReferenceSuppressed = True
    On Error GoTo 0
End Function

' 自身或任意上级零部件引用处于抑制状态时，均不检查
Private Function ShouldSkipDueToSuppression(ByVal swComp As Component2) As Boolean
    Dim cur As Component2
    Dim depth As Long
    Set cur = swComp
    depth = 0
    Do While Not cur Is Nothing
        depth = depth + 1
        If depth > 200 Then Exit Function
        If IsThisReferenceSuppressed(cur) Then
            ShouldSkipDueToSuppression = True
            Exit Function
        End If
        Set cur = GetComponentParentSafe(cur)
    Loop
End Function

Sub main()
    Dim swApp As SldWorks.SldWorks
    Dim swModel As ModelDoc2
    Dim swAssy As AssemblyDoc
    Dim vComps As Variant
    Dim compVar As Variant
    Dim swComp As Component2

    Dim pathName As String
    Dim fileNm As String
    Dim instName As String
    Dim docTitle As String
    Dim swRefDoc As ModelDoc2

    Dim keywords() As String
    Dim issues As String
    Dim hitDetail As String
    Dim kindText As String
    Dim idx As Long
    Dim sep As String
    Dim lineSep As String
    Dim fullReport As String
    Dim fileCountDict As Object
    Dim fileHitDetailDict As Object
    Dim fileKey As String
    Dim hitCount As Long

    Set swApp = Application.SldWorks
    Set swModel = swApp.ActiveDoc

    If swModel Is Nothing Then
        MsgBox "当前没有打开的文档。", vbInformation, "技术开发二部提醒您"
        Exit Sub
    End If

    If swModel.GetType <> swDocASSEMBLY Then
        MsgBox "当前打开的文件不是装配体，无法进行设计检查。" & vbCrLf & vbCrLf & _
               "请先打开装配体后再运行本宏。", vbExclamation, "技术开发二部提醒您"
        Exit Sub
    End If

    GetKeywords keywords

    Set fileCountDict = CreateObject("Scripting.Dictionary")
    fileCountDict.CompareMode = vbTextCompare
    Set fileHitDetailDict = CreateObject("Scripting.Dictionary")
    fileHitDetailDict.CompareMode = vbTextCompare

    Set swAssy = swModel
    issues = ""
    idx = 0

    sep = String(42, "-")   ' 分隔线
    lineSep = vbCrLf

    vComps = swAssy.GetComponents(False) ' False = 所有层级

    If IsEmpty(vComps) Then
        MsgBox "装配体中未找到零部件引用。" & vbCrLf & vbCrLf & "设计检查已通过。", vbInformation, "技术开发二部提醒您"
        Exit Sub
    End If

    For Each compVar In vComps
        Set swComp = compVar
        If Not swComp Is Nothing Then
            If ShouldSkipDueToSuppression(swComp) Then GoTo NextComp

            pathName = swComp.GetPathName
            fileNm = FileNameFromPath(pathName)
            instName = swComp.Name2

            docTitle = ""
            Set swRefDoc = Nothing
            On Error Resume Next
            Set swRefDoc = swComp.GetModelDoc2
            If Not swRefDoc Is Nothing Then
                docTitle = swRefDoc.GetTitle
            End If
            On Error GoTo 0

            hitDetail = BuildHitDetail(fileNm, instName, docTitle, keywords)

            If Len(hitDetail) > 0 Then
                fileKey = LCase$(fileNm)
                If fileCountDict.Exists(fileKey) Then
                    fileCountDict(fileKey) = fileCountDict(fileKey) + 1
                Else
                    fileCountDict.Add fileKey, 1
                    fileHitDetailDict.Add fileKey, hitDetail
                End If
            End If
        End If
NextComp:
    Next compVar

    idx = 0
    Dim keys() As Variant
    keys = fileCountDict.keys
    Dim fileNm2 As String
    Dim pathName2 As String
    Dim instName2 As String
    Dim kindText2 As String

    For Each compVar In vComps
        Set swComp = compVar
        If Not swComp Is Nothing Then
            If ShouldSkipDueToSuppression(swComp) Then GoTo NextComp2

            pathName2 = swComp.GetPathName
            fileNm2 = FileNameFromPath(pathName2)
            fileKey = LCase$(fileNm2)

            If fileCountDict.Exists(fileKey) Then
                hitCount = fileCountDict(fileKey)
                If hitCount > 0 Then
                    idx = idx + 1
                    fileCountDict(fileKey) = 0

                    If IsAssemblyPath(pathName2) Then
                        kindText2 = "子装配体"
                    Else
                        kindText2 = "零件"
                    End If

                    If hitCount > 1 Then
                        issues = issues & sep & lineSep & _
                                 "  序号 : " & CStr(idx) & "     数量 : " & CStr(hitCount) & lineSep & _
                                 "  类型 : " & kindText2 & lineSep & _
                                 "  文件 : " & fileNm2 & lineSep & _
                                 "  路径 : " & pathName2 & lineSep & _
                                 "  命中 : " & fileHitDetailDict(fileKey) & lineSep
                    Else
                        issues = issues & sep & lineSep & _
                                 "  序号 : " & CStr(idx) & lineSep & _
                                 "  类型 : " & kindText2 & lineSep & _
                                 "  文件 : " & fileNm2 & lineSep & _
                                 "  路径 : " & pathName2 & lineSep & _
                                 "  命中 : " & fileHitDetailDict(fileKey) & lineSep
                    End If
                End If
            End If
        End If
NextComp2:
    Next compVar

    If Len(issues) > 0 Then
        fullReport = "设计检查未通过!" & lineSep & lineSep & _
                     "以下零部件名称中含有非法关键字，请关注：" & lineSep & lineSep & _
                     issues & sep & lineSep & "检查结束。" & lineSep

        ShowDesignCheckFailPaged fullReport, idx
    Else
        MsgBox "设计检查已通过。" & lineSep & lineSep & "未发现名称中含有非法关键字的零部件(已压缩/轻量化的零部件不参与设计检查)!", _
               vbInformation, "技术开发二部提醒您"
    End If
End Sub


