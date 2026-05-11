' ******************************************************************************
' 这是solidworks2018的宏代码文件。
' 此代码功能为尺寸属性变更、全屏保存、显示样式带边线上色、隐藏草图和其他所有样式。
' 功能持续添加中。
' 2025.02.21 By GAOJ
' ******************************************************************************
Dim swApp As Object

Dim Part As Object
Dim boolstatus As Boolean
Dim longstatus As Long, longwarnings As Long

Sub main()
    ' 声明并设置 SolidWorks 应用程序对象
    Set swApp = Application.SldWorks  ' swApp 是 SolidWorks 的对象
    
    ' 获取并设置当前活动的文档对象
    Set Part = swApp.ActiveDoc        ' Part 是当前活动文档的对象
    
    ' 设置标注尾零的用户偏好设置
    ' swUserPreferenceIntegerValue_e.swDetailingTrailingZeroProperties：指定要修改的属性
    ' swUserPreferenceOption_e.swDetailingDimension：指定是针对标注的设置
    ' swDetailingDimTrailingZero_e.swDimShowTrailingZeroes：是否显示标注尾零
    boolstatus = Part.Extension.SetUserPreferenceInteger(swUserPreferenceIntegerValue_e.swDetailingTrailingZeroProperties, _
                                                         swUserPreferenceOption_e.swDetailingDimension, _
                                                         swDetailingDimTrailingZero_e.swDimShowTrailingZeroes)
    
    ' 设置预览图片的缩放质量用户偏好设置为 "Zoom To Fit"
    ' swUserPreferenceToggle_e.swImageQualityZoomToFitForPreviewImages：指定要修改的属性
    ' 0：未使用（第三个参数是值）
    ' False：表示禁用
    boolstatus = Part.Extension.SetUserPreferenceToggle(swUserPreferenceToggle_e.swImageQualityZoomToFitForPreviewImages, 0, False)
    
    ' 设置线框图质量的用户偏好设置为自定义模式
    ' swUserPreferenceIntegerValue_e.swImageQualityWireframe：指定要修改的属性
    ' 0：未使用（第三个参数是值）
    ' swImageQualityWireframe_e.swWireframeImageQualityCustom：自定义线框图质量
    boolstatus = Part.Extension.SetUserPreferenceInteger(swUserPreferenceIntegerValue_e.swImageQualityWireframe, 0, _
                                                         swImageQualityWireframe_e.swWireframeImageQualityCustom)
    
    ' 声明并设置当前活动的视图对象
    Dim activeModelView As Object
    Set activeModelView = Part.ActiveView  ' 获取当前活动视图
    
    ' 设置视图的显示模式为 "带边线的着色模式"
    activeModelView.DisplayMode = swViewDisplayMode_e.swViewDisplayMode_ShadedWithEdges
    
    ' 设置用户隐藏所有类型的显示偏好的用户偏好设置为启用
    boolstatus = Part.SetUserPreferenceToggle(swUserPreferenceToggle_e.swViewDisplayHideAllTypes, True)
    
    ' 显示名为 "*前视" 的命名视图
    Part.ShowNamedView2 "*前视", 1  ' 1 表示激活该视图
    
    ' 适应视图到当前窗口
    Part.ViewZoomtofit2
    
    ' 保存文档
    Dim swErrors As Long       ' 保存时返回的错误数量
    Dim swWarnings As Long     ' 保存时返回的警告数量
    boolstatus = Part.Save3(1, swErrors, swWarnings)  ' 1 表示强制保存
End Sub



