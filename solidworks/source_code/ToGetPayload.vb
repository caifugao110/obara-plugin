'----------------------------------------------------------------------
' 这是solidworks2018的宏代码文件。
' 260512增加质心位置和坐标系显示功能
'----------------------------------------------------------------------

Option Explicit

' 定义全局变量，保持代码整洁
Dim swApp As SldWorks.SldWorks
Dim swModel As SldWorks.ModelDoc2
Dim swMathUtil As SldWorks.MathUtility
Dim swMass As SldWorks.MassProperty

Sub main()
    Dim swFeat As SldWorks.Feature
    Dim swNextFeat As SldWorks.Feature
    Dim swKeepFeat As SldWorks.Feature
    
    Dim swMathTrans As SldWorks.MathTransform
    Dim swInverseTrans As SldWorks.MathTransform
    Dim swMathPoint As SldWorks.MathPoint
    Dim swComPointCustom As SldWorks.MathPoint
    
    Dim boolStatus As Boolean
    Dim deleteHappened As Boolean
    Dim massValue As Double
    Dim centerOfMass As Variant
    Dim moi As Variant
    Dim com_custom As Variant
    Dim moi_custom(8) As Double
    
    Dim outputStr As String
    Dim fileTitle As String
    Dim title As String
    
    ' 初始化应用程序
    Set swApp = Application.SldWorks
    Set swModel = swApp.ActiveDoc
    
    ' 1. 环境检查
    If swModel Is Nothing Then
        MsgBox "没有打开文档。请打开零件或装配体。"
        Exit Sub
    End If
    
    If swModel.GetType <> swDocPART And swModel.GetType <> swDocASSEMBLY Then
        MsgBox "此宏仅适用于零件或装配体。"
        Exit Sub
    End If
    
    Set swMathUtil = swApp.GetMathUtility
    
    ' 2. 坐标系逻辑处理
    Dim coordSysFound As Boolean
    coordSysFound = False
    Dim anyCoordSysFound As Boolean
    anyCoordSysFound = False
    Dim firstCoordSys As SldWorks.Feature
    
    ' 扫描特征寻找名为"坐标系1"的坐标系，并记录第一个坐标系
    Set swFeat = swModel.FirstFeature
    Do While Not swFeat Is Nothing
        If swFeat.GetTypeName = "CoordSys" Then
            anyCoordSysFound = True
            If firstCoordSys Is Nothing Then
                Set firstCoordSys = swFeat
            End If
            If swFeat.Name = "坐标系1" Then
                Set swKeepFeat = swFeat
                coordSysFound = True
                Exit Do
            End If
        End If
        Set swFeat = swFeat.GetNextFeature
    Loop
    
    ' 处理坐标系情况
    If Not coordSysFound Then
        If anyCoordSysFound Then
            ' 有坐标系但没有"坐标系1"，将第一个坐标系重命名为"坐标系1"
            firstCoordSys.Name = "坐标系1"
            coordSysFound = True
        Else
            ' 没有任何坐标系，提示用户
            MsgBox "没有找到名为'坐标系1'的坐标系。请先在模型中创建参考坐标系，并命名为'坐标系1'。"
            Exit Sub
        End If
    End If
    
    ' 删除多余的坐标系，只保留名为"坐标系1"的坐标系
    Do
        deleteHappened = False
        Set swFeat = swModel.FirstFeature
        Do While Not swFeat Is Nothing
            Set swNextFeat = swFeat.GetNextFeature
            ' 如果是坐标系且不是我们要保留的那个
            If swFeat.GetTypeName = "CoordSys" And swFeat.Name <> "坐标系1" Then
                boolStatus = swFeat.Select2(False, 0)
                If boolStatus Then
                    swModel.DeleteSelection True
                    deleteHappened = True
                End If
            End If
            Set swFeat = swNextFeat
        Loop
    Loop While deleteHappened
    
    ' 强制重建模型以更新引用
    swModel.EditRebuild3
    
    ' 3. 获取变换矩阵
    ' 获取坐标系1到全局的变换矩阵
    Set swMathTrans = swModel.Extension.GetCoordinateSystemTransformByName("坐标系1")
    If swMathTrans Is Nothing Then
        MsgBox "无法获取'坐标系1'的变换数据。"
        Exit Sub
    End If
    
    ' 获取逆变换（用于将全局重心点转换到局部坐标系）
    Set swInverseTrans = swMathTrans.Inverse()
    
    ' 4. 提取原始质量属性 (基于默认全局坐标系)
    ' 使用 GetMassProperties2 直接传入精度 9，获取最高精度数据
    Dim vMassProps As Variant
    Dim nErrors As Long
    vMassProps = swModel.Extension.GetMassProperties2(9, False, nErrors)
    
    ' 获取质量 (索引 5)
    massValue = vMassProps(5)
    
    ' 5. 数学计算：转换重心坐标
    Dim com_arr(2) As Double
    com_arr(0) = vMassProps(0) ' X 坐标
    com_arr(1) = vMassProps(1) ' Y 坐标
    com_arr(2) = vMassProps(2) ' Z 坐标
    
    Set swMathPoint = swMathUtil.CreatePoint(com_arr)
    ' 将全局点乘以逆矩阵 -> 得到局部点
    Set swComPointCustom = swMathPoint.MultiplyTransform(swInverseTrans)
    com_custom = swComPointCustom.ArrayData
    
    ' 6. 数学计算：旋转惯性张量 (核心算法)
    ' 目标：计算 I_local = R^T * I_global * R
    
    ' (A) 构建全局惯性张量矩阵 I_global
    ' GetMassProperties2返回的数组结构：
    ' 0: 重心X坐标 (相对于原点)
    ' 1: 重心Y坐标 (相对于原点)
    ' 2: 重心Z坐标 (相对于原点)
    ' 3: 体积
    ' 4: 表面积
    ' 5: 质量
    ' 6: Ixx (相对于重心)
    ' 7: Iyy (相对于重心)
    ' 8: Izz (相对于重心)
    ' 9: Ixy (相对于重心)
    ' 10: Ixz (相对于重心)
    ' 11: Iyz (相对于重心)
    Dim I_global(2, 2) As Double
    I_global(0, 0) = vMassProps(6)   ' Ixx (相对于重心)
    I_global(0, 1) = -vMassProps(9)  ' -Ixy (相对于重心)
    I_global(0, 2) = -vMassProps(10) ' -Ixz (相对于重心)
    I_global(1, 0) = -vMassProps(9)  ' -Iyx (相对于重心)
    I_global(1, 1) = vMassProps(7)   ' Iyy (相对于重心)
    I_global(1, 2) = -vMassProps(11) ' -Iyz (相对于重心)
    I_global(2, 0) = -vMassProps(10) ' -Izx (相对于重心)
    I_global(2, 1) = -vMassProps(11) ' -Izy (相对于重心)
    I_global(2, 2) = vMassProps(8)   ' Izz (相对于重心)
    
    ' (B) 获取旋转矩阵 R (全局到坐标系1)
    ' 使用逆变换矩阵，因为它表示从全局到局部的变换
    Dim arrData As Variant
    arrData = swInverseTrans.ArrayData
    Dim R(2, 2) As Double
    Dim i As Integer, j As Integer
    ' SolidWorks变换矩阵按列存储，因此需要调整索引顺序
    For i = 0 To 2: For j = 0 To 2
        R(i, j) = arrData(j * 3 + i)
    Next j: Next i
    
    ' (C) 计算 I_local = R * I_global * R^T
    Dim temp(2, 2) As Double
    Dim I_local(2, 2) As Double
    Dim k As Integer
    
    ' Step 1: temp = R * I_global
    For i = 0 To 2: For j = 0 To 2
        temp(i, j) = 0
        For k = 0 To 2: temp(i, j) = temp(i, j) + R(i, k) * I_global(k, j): Next k
    Next j: Next i
    
    ' Step 2: I_local = temp * R^T
    For i = 0 To 2: For j = 0 To 2
        I_local(i, j) = 0
        For k = 0 To 2: I_local(i, j) = I_local(i, j) + temp(i, k) * R(j, k): Next k
    Next j: Next i
    
    ' 7. 将计算结果填入输出数组
    moi_custom(0) = I_local(0, 0) ' Lxx
    moi_custom(4) = I_local(1, 1) ' Lyy
    moi_custom(8) = I_local(2, 2) ' Lzz
    
    ' 8. 显示质心位置
    swModel.SetUserPreferenceToggle 198, False
    Dim existingCom As SldWorks.Feature
    Set existingCom = swModel.FeatureByName("Center of Mass")
    If existingCom Is Nothing Then
        Set existingCom = swModel.FeatureByName("重心")
    End If
    If Not existingCom Is Nothing Then
        existingCom.Select2 False, 0
        swModel.DeleteSelection True
    End If
    Set swFeat = swModel.FeatureManager.InsertCenterOfMass
    
    ' 9. 显示坐标系1
    boolStatus = swModel.Extension.SelectByID2("坐标系1", "COORDSYS", 0, 0, 0, False, 0, Nothing, 0)
    
    ' 10. 隐藏草图和临时轴
    swModel.Extension.SetUserPreferenceToggle swUserPreferenceToggle_e.swDisplaySketches, swUserPreferenceOption_e.swDetailingNoOptionSpecified, False
    swModel.Extension.SetUserPreferenceToggle swUserPreferenceToggle_e.swDisplayTemporaryAxes, swUserPreferenceOption_e.swDetailingNoOptionSpecified, False
    
    ' 11. 准备输出
    title = swModel.GetTitle
    If InStr(title, ".") > 0 Then
        fileTitle = Left(title, InStrRev(title, ".") - 1)
    Else
        fileTitle = title
    End If
    
    outputStr = fileTitle & "的质量属性" & vbCrLf & vbCrLf & _
                "报告与以下项相对的坐标值：坐标系1" & vbCrLf & vbCrLf & _
                "质量 = " & Format(massValue, "0.000") & " 千克" & vbCrLf & vbCrLf & _
                "重心: ( 米 )" & vbCrLf & _
                "X = " & Format(com_custom(0), "0.000") & vbCrLf & _
                "Y = " & Format(com_custom(1), "0.000") & vbCrLf & _
                "Z = " & Format(com_custom(2), "0.000") & vbCrLf & vbCrLf & _
                "惯性张量: ( 千克 * 平方米 )" & vbCrLf & _
                "由重心决定，并且对齐输出的坐标系。" & vbCrLf & _
                "Lxx = " & Format(moi_custom(0), "0.000") & vbCrLf & _
                "Lyy = " & Format(moi_custom(4), "0.000") & vbCrLf & _
                "Lzz = " & Format(moi_custom(8), "0.000")
    
    MsgBox outputStr, vbInformation, "质量属性"

End Sub

