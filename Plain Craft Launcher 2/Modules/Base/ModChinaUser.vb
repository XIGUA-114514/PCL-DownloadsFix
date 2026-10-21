Imports System.Runtime.InteropServices

''' <summary>
''' 处理「用户是否为 中国用户」的确认，以及由此产生的 MC 下载功能限制。
''' 启动时若检测到系统的首选语言为中文，则询问用户一次，并将回答写入配置文件中的 IsChinaUser。
''' </summary>
Public Module ModChinaUser

#Region "常量"

    ''' <summary>
    ''' 配置项的键名。该配置保存在 PCL\Setup.ini 中。
    ''' </summary>
    Public Const SettingKey As String = "IsChinaUser"
    ''' <summary>
    ''' IsChinaUser 的取值：用户尚未选择。
    ''' </summary>
    Public Const ValueNone As String = "None"
    ''' <summary>
    ''' IsChinaUser 的取值：用户是中国用户。
    ''' </summary>
    Public Const ValueTrue As String = "True"
    ''' <summary>
    ''' IsChinaUser 的取值：用户不是中国用户。
    ''' </summary>
    Public Const ValueFalse As String = "False"

    ''' <summary>
    ''' 询问用户时使用的弹窗文案。
    ''' </summary>
    Public Const DialogCaption As String = "我们发现您的首选语言是简体中文。如果您是中国用户，为遵守相关法律法规，PCL 暂时不再提供 MC 下载功能。"
    ''' <summary>
    ''' 询问用户时使用的弹窗标题。
    ''' </summary>
    Public Const DialogTitle As String = "你是中国用户吗？"
    ''' <summary>
    ''' 询问用户时使用的第一个选项。
    ''' </summary>
    Public Const DialogButtonYes As String = "是，我是中国用户"
    ''' <summary>
    ''' 询问用户时使用的第二个选项。
    ''' </summary>
    Public Const DialogButtonNo As String = "否，我不是中国用户"

#End Region

#Region "首选语言检测"

    ''' <summary>
    ''' LANG_CHINESE，即中文的主语言 ID。
    ''' </summary>
    Private Const LangChinese As Integer = &H4

    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True)>
    Private Function GetUserPreferredUILanguages(dwFlags As UInteger, ByRef pulNumLanguages As UInteger,
                                               pwszLanguagesBuffer As IntPtr, ByRef pcchLanguagesBuffer As UInteger) As Boolean
    End Function
    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function GetUserDefaultUILanguage() As Integer
    End Function

    ''' <summary>
    ''' 获取 Windows 首选 UI 语言列表。获取失败时返回空列表。
    ''' </summary>
    Public Function GetPreferredUILanguages() As List(Of String)
        Dim Result As New List(Of String)
        Try
            '先获取所需的缓冲区大小（单位为字符，包含结尾的两个空字符）
            Dim NumLanguages As UInteger = 0
            Dim BufferChars As UInteger = 0
            If Not GetUserPreferredUILanguages(0, NumLanguages, IntPtr.Zero, BufferChars) Then
                Throw New Exception("获取首选语言缓冲区大小失败（错误码 " & Marshal.GetLastWin32Error() & "）")
            End If
            If BufferChars = 0 Then Throw New Exception("系统未返回任何首选语言")
            '再获取实际的语言列表
            Dim Buffer As IntPtr = IntPtr.Zero
            Try
                Buffer = Marshal.AllocHGlobal(CInt(BufferChars) * 2)
                Dim ActualNum As UInteger = 0
                Dim ActualChars As UInteger = BufferChars
                If Not GetUserPreferredUILanguages(0, ActualNum, Buffer, ActualChars) Then
                    Throw New Exception("获取首选语言列表失败（错误码 " & Marshal.GetLastWin32Error() & "）")
                End If
                ParsePreferredUILanguages(Buffer, ActualChars, Result)
            Finally
                If Buffer <> IntPtr.Zero Then Marshal.FreeHGlobal(Buffer)
            End Try
        Catch ex As Exception
            Logger.Warn(ex, "获取 Windows 首选语言列表失败")
            Result.Clear()
        End Try
        Return Result
    End Function
    ''' <summary>
    ''' 解析 GetUserPreferredUILanguages 写入的双空字符结尾的语言名列表。
    ''' </summary>
    Private Sub ParsePreferredUILanguages(Buffer As IntPtr, MaxChars As Integer, Result As List(Of String))
        Dim Ptr As IntPtr = Buffer
        Dim Current As New StringBuilder
        For Read As Integer = 1 To MaxChars
            Dim Code As Integer = Marshal.ReadInt16(Ptr)
            Ptr = IntPtr.Add(Ptr, 2)
            If Code = 0 Then
                If Current.Length > 0 Then
                    Result.Add(Current.ToString())
                    Current.Clear()
                Else
                    Exit For '连续的空字符表示列表结束
                End If
            Else
                Current.Append(ChrW(Code And &HFFFF))
            End If
        Next
    End Sub
    ''' <summary>
    ''' 判断系统的首选语言是否为中文。
    ''' 优先使用 Windows 首选 UI 语言列表；若获取失败，则依次降级为默认 UI 语言与当前 UI 区域。
    ''' </summary>
    Public Function IsChineseLanguage() As Boolean
        '首选语言列表
        Dim Languages = GetPreferredUILanguages()
        If Languages.Any Then
            Logger.Info($"Windows 首选语言：{Languages.Join("、")}")
            '不使用 Lambda，避免生成闭包类（便于本模块被整体移植到其他版本）
            For Each Language In Languages
                If Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) Then Return True
            Next
            Return False
        End If
        '降级一：用户默认 UI 语言的主语言 ID
        Try
            Dim LangId As Integer = GetUserDefaultUILanguage()
            Dim PrimaryId As Integer = LangId And &H3FF
            Logger.Info($"Windows 默认 UI 语言：{LangId}（主语言 ID {PrimaryId}）")
            Return PrimaryId = LangChinese
        Catch ex As Exception
            Logger.Warn(ex, "获取 Windows 默认 UI 语言失败")
        End Try
        '降级二：.NET 的当前 UI 区域
        Try
            Dim CultureName = Globalization.CultureInfo.CurrentUICulture.Name
            Logger.Info($"当前 UI 区域：{CultureName}")
            Return CultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
        Catch ex As Exception
            Logger.Warn(ex, "获取当前 UI 区域失败")
        End Try
        Return False
    End Function

#End Region

#Region "配置读写"

    ''' <summary>
    ''' 读取用户的选择。若尚未选择，则返回 ValueNone。
    ''' </summary>
    Public Function GetUserChoice() As String
        Try
            Dim Value = ReadUserChoiceRaw()
            If String.IsNullOrWhiteSpace(Value) Then Return ValueNone
            Select Case Value.Trim().ToLowerInvariant()
                Case "true"
                    Return ValueTrue
                Case "false"
                    Return ValueFalse
                Case Else
                    Return ValueNone
            End Select
        Catch ex As Exception
            Logger.Warn(ex, "读取 IsChinaUser 设置失败")
            Return ValueNone
        End Try
    End Function
    ''' <summary>
    ''' 从配置文件中读取 IsChinaUser 的原始值。
    ''' 直接读写 ini 文件，而不依赖 PCL 的设置系统，以便同一份逻辑也能用于修改官方版本。
    ''' </summary>
    Private Function ReadUserChoiceRaw() As String
        Dim Path As String = GetConfigPath()
        If Not File.Exists(Path) Then Return Nothing
        For Each Line As String In File.ReadAllLines(Path, Encoding.UTF8)
            Dim Index = Line.IndexOf(":", StringComparison.Ordinal)
            If Index > 0 AndAlso Line.Substring(0, Index).Trim() = SettingKey Then
                Return Line.Substring(Index + 1).Trim()
            End If
        Next
        Return Nothing
    End Function
    ''' <summary>
    ''' 保存用户的选择到配置文件。
    ''' </summary>
    Public Sub SetUserChoice(Value As String)
        Try
            Dim Path As String = GetConfigPath()
            Dim Folder = PathUtils.RemoveLastPart(Path)
            If Not Directory.Exists(Folder) Then Directory.CreateDirectory(Folder)
            '读取现有内容，保留其他设置项
            Dim Lines As New List(Of String)
            If File.Exists(Path) Then
                For Each Line As String In File.ReadAllLines(Path, Encoding.UTF8)
                    Dim Index = Line.IndexOf(":", StringComparison.Ordinal)
                    If Index > 0 AndAlso Line.Substring(0, Index).Trim() = SettingKey Then Continue For '跳过旧的同键行
                    Lines.Add(Line)
                Next
            End If
            Lines.Add(SettingKey & ":" & Value)
            File.WriteAllLines(Path, Lines, Encoding.UTF8)
            Logger.Info($"已保存 {SettingKey}：{Value}（{Path}）")
        Catch ex As Exception
            Logger.Error(ex, $"保存 {SettingKey} 设置失败")
        End Try
    End Sub
    ''' <summary>
    ''' 配置文件（PCL\Setup.ini）的完整路径。
    ''' </summary>
    Private Function GetConfigPath() As String
        Return PathUtils.AddSlashSuffix(Paths.Base) & "PCL\Setup.ini"
    End Function
    ''' <summary>
    ''' 是否需要询问用户。
    ''' 当配置文件中不存在 IsChinaUser，或其值为 None（未选择）时需要询问。
    ''' </summary>
    Public Function NeedAsk() As Boolean
        Try
            Dim Raw = ReadUserChoiceRaw()
            If String.IsNullOrWhiteSpace(Raw) Then Return True '配置文件中没有该项
            Return GetUserChoice() = ValueNone
        Catch ex As Exception
            Logger.Warn(ex, "检查 IsChinaUser 设置失败")
            Return True
        End Try
    End Function

#End Region

#Region "限制判断"

    ''' <summary>
    ''' 当前是否应当限制 MC 下载功能。
    ''' 仅当用户明确选择了「是，我是中国用户」时才会限制。
    ''' </summary>
    Public ReadOnly Property IsRestricted As Boolean
        Get
            Return GetUserChoice() = ValueTrue
        End Get
    End Property

#End Region

#Region "启动检查"

    ''' <summary>
    ''' 启动时检查系统首选语言，并在需要时询问用户是否为 中国用户。
    ''' 若系统的首选语言不是中文，则直接跳过，不做任何提示与写入。
    ''' </summary>
    Public Sub StartupCheck()
        Try
            '首选语言不是中文 → 跳过提示
            If Not IsChineseLanguage() Then
                Logger.Info("系统首选语言不是中文，跳过中国用户身份确认")
                Exit Sub
            End If
            '已经回答过 → 不再询问
            If Not NeedAsk() Then
                Logger.Info($"配置文件中已存在 {SettingKey}（{GetUserChoice()}），跳过中国用户身份确认")
                Exit Sub
            End If
            '询问用户
            Logger.Info($"检测到系统首选语言为中文，正在询问用户身份（{SettingKey} 尚未确定）")
            Dim Answer = MyMsgBox(DialogCaption, DialogTitle, DialogButtonYes, DialogButtonNo)
            Select Case Answer
                Case 1
                    SetUserChoice(ValueTrue)
                Case 2
                    SetUserChoice(ValueFalse)
                Case Else
                    SetUserChoice(ValueNone)
            End Select
        Catch ex As Exception
            Logger.Error(ex, "中国用户身份确认出错")
        End Try
    End Sub

#End Region

End Module
