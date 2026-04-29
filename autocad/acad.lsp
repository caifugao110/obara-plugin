(defun c:q ()
  ;; 加载Visual LISP扩展
  (vl-Load-Com)
  
  ;; 使用vl-Catch-All-Apply来捕获可能的错误
  (vl-Catch-All-Apply
    '(lambda ()
       ;; 获取当前AutoCAD文档的图层集合
       (vla-Remove
         (vla-GetExtensionDictionary
           (vla-Get-Layers
             (vla-Get-ActiveDocument
               (vlax-Get-Acad-Object)
             )
           )
         )
         "ACAD_LAYERFILTERS"  ;; 删除图层过滤器
       )
     )
  )
  
  ;; 打印提示信息，表示所有图层过滤器已被删除
  (princ "\nAll layer filters have been deleted.")
  (princ)
  
  ;; 执行AutoCAD命令，清理未使用的对象
  (command "-purge" "a" "*" "N" "y")
  
  ;; 执行AutoCAD命令，检查并修复图形中的错误
  (command "audit" "y")
  
  ;; 执行AutoCAD命令，缩放到图形范围
  (command "zoom" "E")
  
  ;; 设置线型比例为1
  (command "LTSCALE" "1")
  
  ;; 设置尺寸标注的零抑制为8
  (command "DIMTZIN" "8")
  
  ;; 设置视图分辨率为20000
  (command "VIEWRES" "Y" "20000")
 
  ;; 关闭命令回显
  (setvar "cmdecho" 0)
  
  ;; 关闭坐标显示
  (command "UCSICON" "OFF")
  
  ;; 初始化一个空列表，用于存储块名
  (setq bnlist '())
  
  ;; 获取第一个块定义
  (setq bn (tblnext "block" T))
  
  ;; 如果块名不以"*"开头，则将其添加到列表中
  (if (/= "*" (substr (cdr (assoc 2 bn)) 1 1))
    (setq bnlist (cons (cdr (assoc 2 bn)) bnlist))
  )
  
  ;; 循环获取所有块定义
  (while bn
    (setq bn (tblnext "block"))
    (if (and bn (/= "*" (substr (cdr (assoc 2 bn)) 1 1)))
      (setq bnlist (cons (cdr (assoc 2 bn)) bnlist))
    )
  )
  
  ;; 遍历块名列表
  (foreach x bnlist
    (setq x1 x)
    ;; 获取块名的前三个字符
    (setq a (substr x1 1 3))
    ;; 如果前三个字符是"A$C"，则重命名块
    (if (= "A$C" a)
      (progn
        (setq t2 (strcat "Y" x1))
        (command "rename" "b" x1 t2)
      )
    )
  )
  
  ;; 打印提示信息，表示所有块已被重命名
  (princ "\nAll blocks have been renamed.")
  
    ;; 保存当前图形,并输出保存成功信息
  (command "_QSAVE")
  (princ "\nThe drawing has been saved.")
  
  (princ)
)