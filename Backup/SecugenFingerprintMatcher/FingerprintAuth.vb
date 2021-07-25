
Imports SecuGen.FDxSDKPro.Windows
Imports System.Text

Public Class FingerprintAuth

    Public Sub New(ByRef rtb As RichTextBox)
        Me.rtb = rtb

        
        '    BindFingerprintANSI()
    End Sub

    Private rtb As New RichTextBox

    Private tempFolder As String = "C:\Allcardtech\UMIDOnlineInquiry"

    Private comboBoxDeviceName As New ComboBox
    'Private pic_Finger As New PictureBox
    Private ansifmrTemplate(0) As Byte

    'Fingerprint FDxSDKPro
    '=========================================
    Private m_FPM As SGFingerPrintManager
    Private m_LedOn As Boolean
    Private m_ImageWidth As Int32
    Private m_ImageHeight As Int32
    Private m_RegMin1(400) As Byte
    Private m_RegMin2(400) As Byte
    Private m_VrfMin(400) As Byte
    Private m_DevList() As SGFPMDeviceList
    Private m_StoredTemplate As Byte()
    Private m_MaxTemplateSize As Int32
    Private fp_image() As Byte
    '=========================================

    Private blnSuccess As Boolean
    Private strMessage As String = ""

    Private FingerPrintData_LeftPrim As Byte() = New Byte(1023) {}
    Private FingerPrintData_RightPrim As Byte() = New Byte(1023) {}
    Private FingerPrintData_LeftBak As Byte() = New Byte(1023) {}
    Private FingerPrintData_RightBak As Byte() = New Byte(1023) {}

    Private imgPhoto As Byte()

    Public ReadOnly Property IsSuccess() As Boolean
        Get
            Return blnSuccess
        End Get
    End Property

    Public ReadOnly Property ErrorMessage() As String
        Get
            Return strMessage
        End Get
    End Property

    Private Sub ClearBuffer(ByRef BuffData() As Byte)

        For i As Integer = 0 To BuffData.Length - 1
            BuffData(i) = 0
        Next

    End Sub

    Private Sub LabelStatus_ChangeText(ByVal sender As Object, ByVal e As EventArgs)
        'label_Status.Text = Me.label_Status
        Application.DoEvents()
    End Sub

    'Private Sub PopulateComboboxDeviceName()
    '    comboBoxDeviceName.Items.Add("USB")
    '    comboBoxDeviceName.Items.Add("Parallel")
    '    comboBoxDeviceName.Items.Add("CN_FDP01")
    '    comboBoxDeviceName.Items.Add("CN_FDU01")
    '    comboBoxDeviceName.Items.Add("CN_FDP02")
    '    comboBoxDeviceName.Items.Add("CN_FDU02")
    '    comboBoxDeviceName.Items.Add("CN_FDU03")
    '    comboBoxDeviceName.Items.Add("NO BIOMETRICS")
    'End Sub

    Private Function InitializeFingerprint() As Boolean
        Try
            Dim iError As Int32
            Dim device_name As SGFPMDeviceName
            Dim device_id As Int32

            m_FPM = New SGFingerPrintManager

            EnumerateDevices()

            If m_FPM.NumberOfDevice = 0 Then
                'label_Status.Text = "Unable to find biometic device"

                strMessage = "Unable to find biometic device"
                SaveLog("Unable to find biometic device", 1)
                Return False
            End If

            device_name = m_DevList(comboBoxDeviceName.SelectedIndex).DevName
            device_id = m_DevList(comboBoxDeviceName.SelectedIndex).DevID

            iError = m_FPM.Init(device_name)
            iError = m_FPM.OpenDevice(device_id)
            iError = m_FPM.SetBrightness(50)
            iError = m_FPM.SetTemplateFormat(SGFPMTemplateFormat.ANSI378)
            iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

            ReDim ansifmrTemplate(m_MaxTemplateSize)

            If (iError = SGFPMError.ERROR_NONE) Then
                GetDeviceInfo()
                'label_Status.Text = "Secugen Initialization Success"
                Return True
            Else
                strMessage = DisplayError("OpenDevice", iError)
                SaveLog(strMessage, 1)
                Return False
            End If
        Catch ex As Exception
            strMessage = ex.Message
            Return False
        End Try
    End Function

    'Public Pass As Boolean

    Private Sub RunTimeCounter(ByVal intCntr As Short)
        Do While intCntr > 0
            'label_cntr.Text = intCntr.ToString
            Application.DoEvents()
            System.Threading.Thread.Sleep(1000)
            intCntr -= 1
        Loop

        'label_cntr.Text = ""
        Application.DoEvents()
    End Sub

    Private Function GetDeviceInfo() As String

        Dim encoding As New ASCIIEncoding
        Dim pInfo As SGFPMDeviceInfoParam
        Dim iError As Int32

        Dim dInfo As String = "NA"

        pInfo = New SGFPMDeviceInfoParam
        iError = m_FPM.GetDeviceInfo(pInfo)

        If (iError = SGFPMError.ERROR_NONE) Then

            m_ImageWidth = pInfo.ImageWidth
            m_ImageHeight = pInfo.ImageHeight

            dInfo = "Device ID: " + Convert.ToString(pInfo.DeviceID) + vbNewLine + _
                    "Firmware Version: " + Convert.ToString(pInfo.FWVersion, 16) + vbNewLine + _
                    "Serial Number: " + encoding.GetString(pInfo.DeviceSN) + vbNewLine + _
                    "Image DPI: " + Convert.ToString(pInfo.ImageDPI) + vbNewLine + _
                    "Image Height: " + Convert.ToString(pInfo.ImageHeight) + vbNewLine + _
                    "Image Width: " + Convert.ToString(pInfo.ImageWidth) + vbNewLine + _
                    "Brightness: " + Convert.ToString(pInfo.Brightness) + vbNewLine + _
                    "Contrast: " + Convert.ToString(pInfo.Contrast) + vbNewLine + _
                    "Gain: " + Convert.ToString(pInfo.Gain)

        End If

        Return dInfo

    End Function

    Public Function EnumerateDevices() As Int32

        Dim iError As Int32
        Dim enum_device As String
        Dim i As Int32

        comboBoxDeviceName.Items.Clear()

        ' Enumerate Device
        iError = m_FPM.EnumerateDevice()

        'Get enumeration info into SGFPMDeviceList
        ReDim m_DevList(m_FPM.NumberOfDevice)

        For i = 0 To m_FPM.NumberOfDevice - 1

            m_DevList(i) = New SGFPMDeviceList
            m_FPM.GetEnumDeviceInfo(i, m_DevList(i))

            enum_device = m_DevList(i).DevName.ToString() + " : " + Convert.ToString(m_DevList(i).DevID)
            comboBoxDeviceName.Items.Add(enum_device)

            If (comboBoxDeviceName.Items.Count > 0) Then
                comboBoxDeviceName.SelectedIndex = 0
            End If
        Next
    End Function


    Private Function DisplayError(ByVal funcName As String, ByVal iError As Int32) As String
        Dim text As String

        text = ""

        Select Case iError
            Case 0                             'SGFDX_ERROR_NONE				= 0,
                text = "Error none"

            Case 1 'SGFDX_ERROR_CREATION_FAILED	= 1,
                text = "Can not create object"

            Case 2 '   SGFDX_ERROR_FUNCTION_FAILED	= 2,
                text = "Function Failed"

            Case 3 '   SGFDX_ERROR_INVALID_PARAM	= 3,
                text = "Invalid Parameter"

            Case 4 '   SGFDX_ERROR_NOT_USED			= 4,
                text = "Not used function"

            Case 5 'SGFDX_ERROR_DLLLOAD_FAILED	= 5,
                text = "Can not create object"

            Case 6 'SGFDX_ERROR_DLLLOAD_FAILED_DRV	= 6,
                text = "Can not load device driver"

            Case 7 'SGFDX_ERROR_DLLLOAD_FAILED_ALGO = 7,
                text = "Can not load sgfpamx.dll"

            Case 51 'SGFDX_ERROR_SYSLOAD_FAILED	   = 51,	// system file load fail
                text = "Can not load driver kernel file"

            Case 52 'SGFDX_ERROR_INITIALIZE_FAILED  = 52,   // chip initialize fail
                text = "Failed to initialize the device"

            Case 53 'SGFDX_ERROR_LINE_DROPPED		   = 53,   // image data drop
                text = "Data transmission is not good"

            Case 54 'SGFDX_ERROR_TIME_OUT			   = 54,   // getliveimage timeout error
                text = "Time out"

            Case 55 'SGFDX_ERROR_DEVICE_NOT_FOUND	= 55,   // device not found
                text = "Device not found"

            Case 56 'SGFDX_ERROR_DRVLOAD_FAILED	   = 56,   // dll file load fail
                text = "Can not load driver file"

            Case 57 'SGFDX_ERROR_WRONG_IMAGE		   = 57,   // wrong image
                text = "Wrong Image"

            Case 58 'SGFDX_ERROR_LACK_OF_BANDWIDTH  = 58,   // USB Bandwith Lack Error
                text = "Lack of USB Bandwith"

            Case 59 'SGFDX_ERROR_DEV_ALREADY_OPEN	= 59,   // Device Exclusive access Error
                text = "Device is already opened"

            Case 60 'SGFDX_ERROR_GETSN_FAILED		   = 60,   // Fail to get Device Serial Number
                text = "Device serial number error"

            Case 61 'SGFDX_ERROR_UNSUPPORTED_DEV		   = 61,   // Unsupported device
                text = "Unsupported device"

                ' Extract & Verification error
            Case 101 'SGFDX_ERROR_FEAT_NUMBER		= 101, // utoo small number of minutiae
                text = "The number of minutiae is too small"

            Case 102 'SGFDX_ERROR_INVALID_TEMPLATE_TYPE		= 102, // wrong template type
                text = "Template is invalid"


            Case 103 'SGFDX_ERROR_INVALID_TEMPLATE1		= 103, // wrong template type
                text = "1st template is invalid"

            Case 104 'SGFDX_ERROR_INVALID_TEMPLATE2		= 104, // vwrong template type
                text = "2nd template is invalid"

            Case 105 'SGFDX_ERROR_EXTRACT_FAIL		= 105, // extraction fail
                text = "Minutiae extraction failed"

            Case 106 'SGFDX_ERROR_MATCH_FAIL		= 106, // matching  fail
                text = "Matching failed"


        End Select

        text = funcName + " Error # " + Convert.ToString(iError) + " :" + text

        Return text

    End Function


    Private Sub ClearInfo()
        ClearBuffer(ansifmrTemplate)
    End Sub


    'Private Sub CaptureFingerPrint_bak()
    '    Dim iError As Int32
    '    Dim elap_time As Int32
    '    Dim timeout As Int32
    '    Dim quality As Int32
    '    Dim img_qlty As Int32

    '    Dim SecugenMin() As Byte

    '    iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

    '    ReDim SecugenMin(m_MaxTemplateSize)

    '    ReDim fp_image(m_ImageWidth * m_ImageHeight)

    '    timeout = Convert.ToInt32(10000)
    '    quality = Convert.ToInt32(80)
    '    elap_time = Environment.TickCount

    '    'Console.WriteLine("")
    '    'label_Status.Text = "Starting Biometric Capture..."
    '    'Console.WriteLine("Starting Biometric Capture...")

    '    iError = m_FPM.SetLedOn(True)

    '    iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)


    '    If (iError = SGFPMError.ERROR_NONE) Then
    '        elap_time = Environment.TickCount - elap_time
    '        'Console.WriteLine("Capture Time : " + Convert.ToString(elap_time) + " ms")
    '        'label_Status.Text = "Capture Time : " + Convert.ToString(elap_time) + " ms"

    '        'Console.WriteLine("")
    '        label_Status.Text = "Starting to create ANSI-FMR Template..."
    '        'Console.WriteLine("Starting to create ANSI-FMR Template...")

    '        System.Threading.Thread.Sleep(100)

    '        m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

    '        Dim finger_info As New SGFPMFingerInfo()
    '        finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
    '        finger_info.ImageQuality = CShort(img_qlty)
    '        finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
    '        finger_info.ViewNumber = 1

    '        ClearBuffer(SecugenMin)

    '        'Get ANSI Template
    '        iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

    '        If (iError = SGFPMError.ERROR_NONE) Then
    '            'Console.WriteLine("ASNI-FMR Template Created...")
    '            ' label_Status.Text = "Template is captured"
    '            label_Status.Text = "Matching..."
    '            'Console.WriteLine("Matching...")

    '            'MATCH ON UMID
    '            '========================
    '            Dim FingerPrintData As Byte() = New Byte(1023) {}
    '            Dim ErrorMessage As Byte() = New Byte(1023) {}
    '            Dim Data As Byte()
    '            Dim Result As Boolean

    '            Dim LeftPrimary As Byte = &HD
    '            Dim RightPrimary As Byte = &HE
    '            Dim LeftBackup As Byte = &HF
    '            Dim RightBackup As Byte = &H10

    '            'Dim LeftBackup As Byte = &HD
    '            'Dim RightBackup As Byte = &HE
    '            'Dim LeftPrimary As Byte = &HF
    '            'Dim RightPrimary As Byte = &H10

    '            'Get Fingerprint
    '            Dim Path_Fingerprint As String = Application.StartupPath + "\UMID_ANSI.ansi-fmr"

    '            If IO.File.Exists(Path_Fingerprint) Then
    '                IO.File.Delete(Path_Fingerprint)
    '            End If

    '            Data = System.Text.ASCIIEncoding.ASCII.GetBytes(Path_Fingerprint)

    '            Dim result1, result2, result3, result4 As Boolean

    '            'result1 = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(LeftPrimary, Data, ErrorMessage)
    '            'result2 = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(RightPrimary, Data, ErrorMessage)
    '            'result3 = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(LeftBackup, Data, ErrorMessage)
    '            'result4 = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(RightBackup, Data, ErrorMessage)

    '            If result1 = True Then
    '                Result = result1
    '            ElseIf result2 = True Then
    '                Result = result2
    '            ElseIf result3 = True Then
    '                Result = result3
    '            ElseIf result4 = True Then
    '                Result = result4
    '            Else
    '                strMessage = "Match Failed."
    '                blnSuccess = False
    '                Exit Sub
    '            End If

    '            If Not Result Then
    '                Application.DoEvents()
    '            Else
    '                Application.DoEvents()
    '                If IO.File.Exists(Path_Fingerprint) Then
    '                    FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
    '                End If
    '            End If

    '            label_Status.Text = "Authenticating Security Level 3, " + Result.ToString
    '            'Console.WriteLine("Authenticating Security Level 3, " + Result.ToString)

    '            'TEMPLATE TO TEMPLATE MATCHING
    '            '==========================================
    '            Dim matched As Boolean
    '            'iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            'If iError = SGFPMError.ERROR_NONE Then
    '            '    If matched Then
    '            '        MessageBox.Show("Template Matching Success...")
    '            '    Else
    '            '        MessageBox.Show("Template Matching Failed...")
    '            '    End If
    '            'End If

    '            'iError = m_FPM.MatchTemplate(SecugenMin, FingerPrintData, SGFPMSecurityLevel.NORMAL, matched)
    '            'iError = m_FPM.MatchTemplateEx(SecugenMin, SGFPMTemplateFormat.ANSI378, 0, FingerPrintData, SGFPMTemplateFormat.ANSI378, 0, SGFPMSecurityLevel.NORMAL, matched)
    '            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            If iError = SGFPMError.ERROR_NONE Then
    '                If matched Then
    '                    'strMessage = "Template Matching Success..."
    '                    blnSuccess = True
    '                    'dotNetUMIDSAM.UMIDSAM.UMIDCard_SL3(ErrorMessage)
    '                    'label_status.Text = "Authentication Status: Security Level 3 Acquired..."
    '                Else
    '                    strMessage = "Template Matching Failed..."
    '                    blnSuccess = False
    '                    'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '                End If
    '            Else
    '                strMessage = "Template Matching Failed..."
    '                blnSuccess = False
    '                'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '            End If
    '            '==========================================
    '        Else
    '            strMessage = DisplayError("CreateTemplate", iError)
    '            blnSuccess = False
    '            'Console.WriteLine(label_Status.Text)
    '        End If

    '    Else
    '        pic_Finger.Image = Nothing
    '        strMessage = DisplayError("GetImage", iError)
    '        'Console.WriteLine(label_Status.Text)
    '    End If

    '    iError = m_FPM.SetLedOn(False)
    'End Sub


    'Private Function CaptureFingerprintAndMatchToUMID() As Boolean
    '    'label_Status.Text = "Starting biometric capture..."

    '    'GetUMIDFingerprints()

    '    Dim iError As Int32
    '    Dim elap_time As Int32
    '    Dim timeout As Int32
    '    Dim quality As Int32
    '    Dim img_qlty As Int32

    '    Dim SecugenMin() As Byte

    '    iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

    '    ReDim SecugenMin(m_MaxTemplateSize)

    '    ReDim fp_image(m_ImageWidth * m_ImageHeight)

    '    timeout = Convert.ToInt32(10000)
    '    quality = Convert.ToInt32(80)
    '    elap_time = Environment.TickCount

    '    iError = m_FPM.SetLedOn(True)

    '    iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)


    '    If (iError = SGFPMError.ERROR_NONE) Then
    '        System.Threading.Thread.Sleep(100)

    '        m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

    '        Dim finger_info As New SGFPMFingerInfo()
    '        finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
    '        finger_info.ImageQuality = CShort(img_qlty)
    '        finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
    '        finger_info.ViewNumber = 1

    '        ClearBuffer(SecugenMin)

    '        'Get ANSI Template
    '        iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

    '        If (iError = SGFPMError.ERROR_NONE) Then

    '            'label_Status.Text = "Matching fingerprint..."
    '            Application.DoEvents()
    '            'If IO.File.Exists(Path_Fingerprint) Then
    '            'FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
    '            'End If
    '            'End If

    '            'TEMPLATE TO TEMPLATE MATCHING
    '            '==========================================
    '            Dim matched As Boolean



    '            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_LeftPrim, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            'match left primary
    '            If Not MatchedFingerprints(iError, matched) Then
    '                'match right primary
    '                iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_RightPrim, 0, SGFPMSecurityLevel.NORMAL, matched)

    '                If Not MatchedFingerprints(iError, matched) Then
    '                    'match left backup
    '                    iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_LeftBak, 0, SGFPMSecurityLevel.NORMAL, matched)

    '                    If Not MatchedFingerprints(iError, matched) Then
    '                        'match right backup
    '                        iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_RightBak, 0, SGFPMSecurityLevel.NORMAL, matched)

    '                        If Not MatchedFingerprints(iError, matched) Then

    '                            SaveLog("Fingerprints not matched", 1)
    '                            strMessage = "Failed to authenticate card"
    '                            blnSuccess = False
    '                        Else
    '                            blnSuccess = True
    '                        End If
    '                    Else
    '                        blnSuccess = True
    '                    End If
    '                Else
    '                    blnSuccess = True
    '                End If
    '            Else
    '                blnSuccess = True
    '            End If
    '        Else
    '            'strMessage = DisplayError("CreateTemplate", iError)

    '            SaveLog(DisplayError("CreateTemplate", iError), 1)
    '            strMessage = "Failed to capture fingerprint"
    '        End If

    '    Else

    '        pic_Finger.Image = Nothing
    '        'strMessage = DisplayError("GetImage", iError)
    '        strMessage = "Failed to capture fingerprint"
    '        SaveLog(DisplayError("GetImage", iError), 1)
    '        'Console.WriteLine(DisplayError("GetImage", iError))
    '    End If

    '    If blnSuccess = True Then
    '        iError = m_FPM.SetLedOn(False)
    '    End If

    '    'DisconnectCard(My.Settings.SmartCardReader)

    '    Return blnSuccess

    'End Function

    Private Function MatchedFingerprints(ByVal iError As Integer, ByVal matched As Boolean) As Boolean
        If iError = SGFPMError.ERROR_NONE Then
            If matched Then
                strMessage = ""
                Return True
            Else
                strMessage = "Template Matching Failed"
                Return False
            End If
        Else
            strMessage = "Template Matching Failed"
            Return False
        End If
    End Function

    'Private Function CaptureFingerLeftPrim() As Boolean


    '    Dim iError As Int32
    '    Dim elap_time As Int32
    '    Dim timeout As Int32
    '    Dim quality As Int32
    '    Dim img_qlty As Int32

    '    Dim SecugenMin() As Byte

    '    iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

    '    ReDim SecugenMin(m_MaxTemplateSize)

    '    ReDim fp_image(m_ImageWidth * m_ImageHeight)

    '    timeout = Convert.ToInt32(10000)
    '    quality = Convert.ToInt32(80)
    '    elap_time = Environment.TickCount

    '    'Console.WriteLine("")
    '    'Console.WriteLine("Starting Biometric Capture...")
    '    'label_Status.Text = "Starting Biometric Capture..."

    '    iError = m_FPM.SetLedOn(True)

    '    iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)


    '    If (iError = SGFPMError.ERROR_NONE) Then
    '        elap_time = Environment.TickCount - elap_time
    '        'Console.WriteLine("Capture Time : " + Convert.ToString(elap_time) + " ms")
    '        'label_Status.Text = "Capture Time : " + Convert.ToString(elap_time) + " ms"

    '        'Console.WriteLine("")
    '        'Console.WriteLine("Starting to create ANSI-FMR Template...")

    '        System.Threading.Thread.Sleep(100)

    '        m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

    '        Dim finger_info As New SGFPMFingerInfo()
    '        finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
    '        finger_info.ImageQuality = CShort(img_qlty)
    '        finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
    '        finger_info.ViewNumber = 1

    '        ClearBuffer(SecugenMin)

    '        'Get ANSI Template
    '        iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

    '        If (iError = SGFPMError.ERROR_NONE) Then

    '            'Console.WriteLine("ASNI-FMR Template Created...")
    '            ' label_Status.Text = "Template is captured"
    '            'Console.WriteLine("Matching...")
    '            'label_Status.Text = "Matching..."

    '            'MATCH ON UMID
    '            '========================
    '            Dim FingerPrintData As Byte() = New Byte(1023) {}
    '            Dim ErrorMessage As Byte() = New Byte(1023) {}
    '            Dim Data As Byte()
    '            Dim Result As Boolean

    '            Dim LeftPrimary As Byte = &HD
    '            Dim RightPrimary As Byte = &HE
    '            Dim LeftBackup As Byte = &HF
    '            Dim RightBackup As Byte = &H10

    '            'Dim LeftBackup As Byte = &HD
    '            'Dim RightBackup As Byte = &HE
    '            'Dim LeftPrimary As Byte = &HF
    '            'Dim RightPrimary As Byte = &H10

    '            'Get Fingerprint
    '            Dim Path_Fingerprint As String = Application.StartupPath + "\UMID_ANSI.ansi-fmr"


    '            If IO.File.Exists(Path_Fingerprint) Then
    '                IO.File.Delete(Path_Fingerprint)
    '            End If

    '            Data = System.Text.ASCIIEncoding.ASCII.GetBytes(Path_Fingerprint)

    '            'Result = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(LeftPrimary, Data, ErrorMessage)

    '            If Not Result = True Then
    '                strMessage = "Match Failed"
    '                blnSuccess = False
    '                Exit Function
    '            End If

    '            If Not Result Then
    '                Application.DoEvents()
    '            Else
    '                Application.DoEvents()
    '                If IO.File.Exists(Path_Fingerprint) Then
    '                    FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
    '                End If
    '            End If

    '            'Console.WriteLine("Authenticating Security Level 3, " + Result.ToString)

    '            'TEMPLATE TO TEMPLATE MATCHING
    '            '==========================================
    '            Dim matched As Boolean
    '            'iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            'If iError = SGFPMError.ERROR_NONE Then
    '            '    If matched Then
    '            '        MessageBox.Show("Template Matching Success...")
    '            '    Else
    '            '        MessageBox.Show("Template Matching Failed...")
    '            '    End If
    '            'End If

    '            'iError = m_FPM.MatchTemplate(SecugenMin, FingerPrintData, SGFPMSecurityLevel.NORMAL, matched)
    '            'iError = m_FPM.MatchTemplateEx(SecugenMin, SGFPMTemplateFormat.ANSI378, 0, FingerPrintData, SGFPMTemplateFormat.ANSI378, 0, SGFPMSecurityLevel.NORMAL, matched)
    '            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            If iError = SGFPMError.ERROR_NONE Then
    '                If matched Then

    '                    'strMessage = "Template Matching Success..."
    '                    blnSuccess = True
    '                    'dotNetUMIDSAM.UMIDSAM.UMIDCard_SL3(ErrorMessage)
    '                    'label_status.Text = "Authentication Status: Security Level 3 Acquired..."

    '                Else
    '                    strMessage = "Template Matching Failed"
    '                    blnSuccess = False

    '                    'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '                End If
    '            Else
    '                strMessage = "Template Matching Failed"
    '                blnSuccess = False
    '                'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '            End If
    '            '==========================================
    '        Else
    '            strMessage = DisplayError("CreateTemplate", iError)
    '            'Console.WriteLine(DisplayError("CreateTemplate", iError))
    '        End If

    '    Else
    '        pic_Finger.Image = Nothing
    '        strMessage = DisplayError("GetImage", iError)
    '        'Console.WriteLine(DisplayError("GetImage", iError))
    '    End If

    '    If blnSuccess = True Then
    '        iError = m_FPM.SetLedOn(False)
    '    End If

    '    Return blnSuccess

    'End Function

    'Private UMID As New UMID_Card_Read(My.Settings.UMID, My.Settings.SAM, My.Settings.DefaultPin)
    'Private UMID As New UMID_Card_Read(My.Settings.UMID, My.Settings.SAM, "123456")

    'Private Function CaptureFingerLeftBack() As Boolean

    '    Dim iError As Int32
    '    Dim elap_time As Int32
    '    Dim timeout As Int32
    '    Dim quality As Int32
    '    Dim img_qlty As Int32

    '    Dim SecugenMin() As Byte

    '    iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

    '    ReDim SecugenMin(m_MaxTemplateSize)

    '    ReDim fp_image(m_ImageWidth * m_ImageHeight)

    '    timeout = Convert.ToInt32(10000)
    '    quality = Convert.ToInt32(80)
    '    elap_time = Environment.TickCount

    '    'Console.WriteLine("")
    '    'Console.WriteLine("Starting Biometric Capture...")
    '    'label_Status.Text = "Starting Biometric Capture..."

    '    iError = m_FPM.SetLedOn(True)

    '    iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)


    '    If (iError = SGFPMError.ERROR_NONE) Then
    '        elap_time = Environment.TickCount - elap_time
    '        'Console.WriteLine("Capture Time : " + Convert.ToString(elap_time) + " ms")
    '        'label_Status.Text = "Capture Time : " + Convert.ToString(elap_time) + " ms"

    '        'Console.WriteLine("")
    '        'Console.WriteLine("Starting to create ANSI-FMR Template...")

    '        System.Threading.Thread.Sleep(100)

    '        m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

    '        Dim finger_info As New SGFPMFingerInfo()
    '        finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
    '        finger_info.ImageQuality = CShort(img_qlty)
    '        finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
    '        finger_info.ViewNumber = 1

    '        ClearBuffer(SecugenMin)

    '        'Get ANSI Template
    '        iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

    '        If (iError = SGFPMError.ERROR_NONE) Then

    '            'Console.WriteLine("ASNI-FMR Template Created...")
    '            ' label_Status.Text = "Template is captured"
    '            'Console.WriteLine("Matching...")
    '            'label_Status.Text = "Matching..."

    '            'MATCH ON UMID
    '            '========================
    '            Dim FingerPrintData As Byte() = New Byte(1023) {}
    '            Dim ErrorMessage As Byte() = New Byte(1023) {}
    '            Dim Data As Byte()
    '            Dim Result As Boolean

    '            Dim LeftPrimary As Byte = &HD
    '            Dim RightPrimary As Byte = &HE
    '            Dim LeftBackup As Byte = &HF
    '            Dim RightBackup As Byte = &H10

    '            'Dim LeftBackup As Byte = &HD
    '            'Dim RightBackup As Byte = &HE
    '            'Dim LeftPrimary As Byte = &HF
    '            'Dim RightPrimary As Byte = &H10

    '            'Get Fingerprint
    '            Dim Path_Fingerprint As String = Application.StartupPath + "\UMID_ANSI.ansi-fmr"


    '            If IO.File.Exists(Path_Fingerprint) Then
    '                IO.File.Delete(Path_Fingerprint)
    '            End If

    '            Data = System.Text.ASCIIEncoding.ASCII.GetBytes(Path_Fingerprint)

    '            'Result = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(LeftBackup, Data, ErrorMessage)

    '            If Not Result = True Then
    '                strMessage = "Match Failed"
    '                blnSuccess = False
    '                Exit Function
    '            End If

    '            If Not Result Then
    '                Application.DoEvents()
    '            Else
    '                Application.DoEvents()
    '                If IO.File.Exists(Path_Fingerprint) Then
    '                    FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
    '                End If
    '            End If

    '            'Console.WriteLine("Authenticating Security Level 3, " + Result.ToString)

    '            'TEMPLATE TO TEMPLATE MATCHING
    '            '==========================================
    '            Dim matched As Boolean
    '            'iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            'If iError = SGFPMError.ERROR_NONE Then
    '            '    If matched Then
    '            '        MessageBox.Show("Template Matching Success...")
    '            '    Else
    '            '        MessageBox.Show("Template Matching Failed...")
    '            '    End If
    '            'End If

    '            'iError = m_FPM.MatchTemplate(SecugenMin, FingerPrintData, SGFPMSecurityLevel.NORMAL, matched)
    '            'iError = m_FPM.MatchTemplateEx(SecugenMin, SGFPMTemplateFormat.ANSI378, 0, FingerPrintData, SGFPMTemplateFormat.ANSI378, 0, SGFPMSecurityLevel.NORMAL, matched)
    '            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            If iError = SGFPMError.ERROR_NONE Then
    '                If matched Then
    '                    'strMessage = "Template Matching Success..."
    '                    blnSuccess = True
    '                    'dotNetUMIDSAM.UMIDSAM.UMIDCard_SL3(ErrorMessage)
    '                    'label_status.Text = "Authentication Status: Security Level 3 Acquired..."

    '                Else
    '                    strMessage = "Template Matching Failed"
    '                    blnSuccess = False
    '                    'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '                End If
    '            Else
    '                strMessage = "Template Matching Failed"
    '                blnSuccess = False
    '                'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '            End If
    '            '==========================================
    '        Else
    '            strMessage = DisplayError("CreateTemplate", iError)
    '            'Console.WriteLine(DisplayError("CreateTemplate", iError))
    '        End If

    '    Else
    '        pic_Finger.Image = Nothing
    '        strMessage = DisplayError("GetImage", iError)
    '        'Console.WriteLine(DisplayError("GetImage", iError))
    '    End If

    '    If blnSuccess = True Then
    '        iError = m_FPM.SetLedOn(False)
    '    End If

    '    Return blnSuccess
    'End Function

    'Private Function CaptureFingerRightPrim() As Boolean
    '    Dim iError As Int32
    '    Dim elap_time As Int32
    '    Dim timeout As Int32
    '    Dim quality As Int32
    '    Dim img_qlty As Int32

    '    Dim SecugenMin() As Byte

    '    iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

    '    ReDim SecugenMin(m_MaxTemplateSize)

    '    ReDim fp_image(m_ImageWidth * m_ImageHeight)

    '    timeout = Convert.ToInt32(10000)
    '    quality = Convert.ToInt32(80)
    '    elap_time = Environment.TickCount

    '    'Console.WriteLine("")
    '    'Console.WriteLine("Starting Biometric Capture...")
    '    'label_Status.Text = "Starting Biometric Capture..."
    '    'Application.DoEvents()

    '    iError = m_FPM.SetLedOn(True)

    '    iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)

    '    If (iError = SGFPMError.ERROR_NONE) Then
    '        elap_time = Environment.TickCount - elap_time
    '        'Console.WriteLine("Capture Time : " + Convert.ToString(elap_time) + " ms")
    '        'label_Status.Text = "Capture Time : " + Convert.ToString(elap_time) + " ms"

    '        'Console.WriteLine("")
    '        'Console.WriteLine("Starting to create ANSI-FMR Template...")
    '        'label_Status.Text = "Starting to create ANSI-FMR Template..."

    '        System.Threading.Thread.Sleep(100)

    '        m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

    '        Dim finger_info As New SGFPMFingerInfo()
    '        finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
    '        finger_info.ImageQuality = CShort(img_qlty)
    '        finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
    '        finger_info.ViewNumber = 1

    '        ClearBuffer(SecugenMin)

    '        'Get ANSI Template
    '        iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

    '        If (iError = SGFPMError.ERROR_NONE) Then

    '            'Console.WriteLine("ASNI-FMR Template Created...")
    '            ' label_Status.Text = "Template is captured"
    '            'Console.WriteLine("Matching...")
    '            'label_Status.Text = "Matching..."
    '            'Application.DoEvents()

    '            'MATCH ON UMID
    '            '========================
    '            Dim FingerPrintData As Byte() = New Byte(1023) {}
    '            Dim ErrorMessage As Byte() = New Byte(1023) {}
    '            Dim Data As Byte()
    '            Dim Result As Boolean

    '            Dim LeftPrimary As Byte = &HD
    '            Dim RightPrimary As Byte = &HE
    '            Dim LeftBackup As Byte = &HF
    '            Dim RightBackup As Byte = &H10

    '            'Dim LeftBackup As Byte = &HD
    '            'Dim RightBackup As Byte = &HE
    '            'Dim LeftPrimary As Byte = &HF
    '            'Dim RightPrimary As Byte = &H10

    '            'Get Fingerprint
    '            Dim Path_Fingerprint As String = Application.StartupPath + "\UMID_ANSI.ansi-fmr"

    '            If IO.File.Exists(Path_Fingerprint) Then
    '                IO.File.Delete(Path_Fingerprint)
    '            End If

    '            Data = System.Text.ASCIIEncoding.ASCII.GetBytes(Path_Fingerprint)

    '            'Result = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(RightPrimary, Data, ErrorMessage)

    '            If Not Result = True Then
    '                strMessage = "Match Failed"
    '                blnSuccess = False
    '                Exit Function
    '            End If

    '            If Not Result Then
    '                Application.DoEvents()
    '            Else
    '                Application.DoEvents()
    '                If IO.File.Exists(Path_Fingerprint) Then
    '                    FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
    '                End If
    '            End If

    '            'Console.WriteLine("Authenticating Security Level 3, " + Result.ToString)

    '            'TEMPLATE TO TEMPLATE MATCHING
    '            '==========================================
    '            Dim matched As Boolean
    '            'iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            'If iError = SGFPMError.ERROR_NONE Then
    '            '    If matched Then
    '            '        MessageBox.Show("Template Matching Success...")
    '            '    Else
    '            '        MessageBox.Show("Template Matching Failed...")
    '            '    End If
    '            'End If

    '            'iError = m_FPM.MatchTemplate(SecugenMin, FingerPrintData, SGFPMSecurityLevel.NORMAL, matched)
    '            'iError = m_FPM.MatchTemplateEx(SecugenMin, SGFPMTemplateFormat.ANSI378, 0, FingerPrintData, SGFPMTemplateFormat.ANSI378, 0, SGFPMSecurityLevel.NORMAL, matched)
    '            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            If iError = SGFPMError.ERROR_NONE Then
    '                If matched Then
    '                    'strMessage = "Template Matching Success..."
    '                    blnSuccess = True
    '                    'dotNetUMIDSAM.UMIDSAM.UMIDCard_SL3(ErrorMessage)
    '                    'label_status.Text = "Authentication Status: Security Level 3 Acquired..."

    '                Else
    '                    strMessage = "Template Matching Failed"
    '                    blnSuccess = False
    '                    'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '                End If
    '            Else
    '                strMessage = "Template Matching Failed"
    '                blnSuccess = False
    '                'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '            End If
    '            '==========================================
    '        Else
    '            strMessage = DisplayError("CreateTemplate", iError)
    '            blnSuccess = False
    '            'Console.WriteLine(DisplayError("CreateTemplate", iError))
    '        End If

    '    Else
    '        pic_Finger.Image = Nothing
    '        strMessage = DisplayError("GetImage", iError)
    '        'Console.WriteLine(DisplayError("GetImage", iError))
    '    End If

    '    If blnSuccess = True Then
    '        iError = m_FPM.SetLedOn(False)
    '    End If

    '    Return blnSuccess
    'End Function


    'Private Function CaptureFingerRightBack() As Boolean
    '    Dim iError As Int32
    '    Dim elap_time As Int32
    '    Dim timeout As Int32
    '    Dim quality As Int32
    '    Dim img_qlty As Int32

    '    Dim SecugenMin() As Byte

    '    iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)

    '    ReDim SecugenMin(m_MaxTemplateSize)

    '    ReDim fp_image(m_ImageWidth * m_ImageHeight)

    '    timeout = Convert.ToInt32(10000)
    '    quality = Convert.ToInt32(80)
    '    elap_time = Environment.TickCount

    '    'Console.WriteLine("")
    '    'Console.WriteLine("Starting Biometric Capture...")
    '    'label_Status.Text = "Starting Biometric Capture..."

    '    iError = m_FPM.SetLedOn(True)

    '    iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)

    '    If (iError = SGFPMError.ERROR_NONE) Then
    '        elap_time = Environment.TickCount - elap_time
    '        'Console.WriteLine("Capture Time : " + Convert.ToString(elap_time) + " ms")
    '        'label_Status.Text = "Capture Time : " + Convert.ToString(elap_time) + " ms"

    '        'Console.WriteLine("")
    '        'Console.WriteLine("Starting to create ANSI-FMR Template...")

    '        System.Threading.Thread.Sleep(100)

    '        m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

    '        Dim finger_info As New SGFPMFingerInfo()
    '        finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
    '        finger_info.ImageQuality = CShort(img_qlty)
    '        finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
    '        finger_info.ViewNumber = 1

    '        ClearBuffer(SecugenMin)

    '        'Get ANSI Template
    '        iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

    '        If (iError = SGFPMError.ERROR_NONE) Then

    '            'Console.WriteLine("ASNI-FMR Template Created...")
    '            ' label_Status.Text = "Template is captured"
    '            'Console.WriteLine("Matching...")
    '            'label_Status.Text = "Matching..."

    '            'MATCH ON UMID
    '            '========================
    '            Dim FingerPrintData As Byte() = New Byte(1023) {}
    '            Dim ErrorMessage As Byte() = New Byte(1023) {}
    '            Dim Data As Byte()
    '            Dim Result As Boolean

    '            Dim LeftPrimary As Byte = &HD
    '            Dim RightPrimary As Byte = &HE
    '            Dim LeftBackup As Byte = &HF
    '            Dim RightBackup As Byte = &H10

    '            'Dim LeftBackup As Byte = &HD
    '            'Dim RightBackup As Byte = &HE
    '            'Dim LeftPrimary As Byte = &HF
    '            'Dim RightPrimary As Byte = &H10

    '            'Get Fingerprint
    '            Dim Path_Fingerprint As String = Application.StartupPath + "\UMID_ANSI.ansi-fmr"

    '            If IO.File.Exists(Path_Fingerprint) Then
    '                IO.File.Delete(Path_Fingerprint)
    '            End If

    '            Data = System.Text.ASCIIEncoding.ASCII.GetBytes(Path_Fingerprint)

    '            'Result = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(RightBackup, Data, ErrorMessage)


    '            If Not Result = True Then
    '                strMessage = "Match Failed"
    '                blnSuccess = False
    '                Exit Function
    '            End If

    '            If Not Result Then
    '                Application.DoEvents()
    '            Else
    '                Application.DoEvents()
    '                If IO.File.Exists(Path_Fingerprint) Then
    '                    FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
    '                End If
    '            End If

    '            'Console.WriteLine("Authenticating Security Level 3, " + Result.ToString)

    '            'TEMPLATE TO TEMPLATE MATCHING
    '            '==========================================
    '            Dim matched As Boolean
    '            'iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            'If iError = SGFPMError.ERROR_NONE Then
    '            '    If matched Then
    '            '        MessageBox.Show("Template Matching Success...")
    '            '    Else
    '            '        MessageBox.Show("Template Matching Failed...")
    '            '    End If
    '            'End If

    '            'iError = m_FPM.MatchTemplate(SecugenMin, FingerPrintData, SGFPMSecurityLevel.NORMAL, matched)
    '            'iError = m_FPM.MatchTemplateEx(SecugenMin, SGFPMTemplateFormat.ANSI378, 0, FingerPrintData, SGFPMTemplateFormat.ANSI378, 0, SGFPMSecurityLevel.NORMAL, matched)
    '            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData, 0, SGFPMSecurityLevel.NORMAL, matched)

    '            If iError = SGFPMError.ERROR_NONE Then
    '                If matched Then
    '                    'strMessage = "Template Matching Success..."
    '                    blnSuccess = True
    '                    'dotNetUMIDSAM.UMIDSAM.UMIDCard_SL3(ErrorMessage)
    '                    'label_status.Text = "Authentication Status: Security Level 3 Acquired..."

    '                Else
    '                    strMessage = "Template Matching Failed"
    '                    blnSuccess = False
    '                    'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '                End If
    '            Else
    '                strMessage = "Template Matching Failed"
    '                blnSuccess = False
    '                'label_status.Text = "Authentication Status: " + System.Text.ASCIIEncoding.ASCII.GetString(ErrorMessage)
    '            End If
    '            '==========================================
    '        Else
    '            strMessage = DisplayError("CreateTemplate", iError)
    '            'Console.WriteLine(DisplayError("CreateTemplate", iError))
    '            blnSuccess = False
    '        End If

    '    Else
    '        pic_Finger.Image = Nothing
    '        strMessage = DisplayError("GetImage", iError)
    '        'Console.WriteLine(DisplayError("GetImage", iError))
    '        blnSuccess = False
    '    End If


    '    If blnSuccess = True Then
    '        iError = m_FPM.SetLedOn(False)
    '    End If


    '    Return blnSuccess

    'End Function


    'Private Sub GetUMIDFingerprint_LeftPrim()
    '    'If FingerCode_LeftPrimary = "" Then Exit Sub
    '    'If FingerCode_LeftPrimary = "a" Then Exit Sub

    '    Dim ErrorMessage As Byte() = New Byte(1023) {}
    '    Dim Data As Byte()
    '    Dim Result As Boolean

    '    Dim LeftPrimary As Byte = &HD

    '    'Dim LeftBackup As Byte = &HD
    '    'Dim RightBackup As Byte = &HE
    '    'Dim LeftPrimary As Byte = &HF
    '    'Dim RightPrimary As Byte = &H10        

    '    If IO.File.Exists(Path_Fingerprint_LeftPrim) Then IO.File.Delete(Path_Fingerprint_LeftPrim)

    '    Data = System.Text.ASCIIEncoding.ASCII.GetBytes(Path_Fingerprint_LeftPrim)

    '    Result = dotNetUMIDSAM.UMIDSAM.UMIDCard_Get_FingerPrint(LeftPrimary, Data, ErrorMessage)

    '    If Not Result Then
    '        Application.DoEvents()
    '        strMessage = "Match Failed"
    '        blnSuccess = False
    '        Exit Sub
    '    Else
    '        Application.DoEvents()
    '        If IO.File.Exists(Path_Fingerprint_LeftPrim) Then FingerPrintData_LeftPrim = IO.File.ReadAllBytes(Path_Fingerprint_LeftPrim)
    '    End If
    'End Sub

    Private BACK_OCR As String = ""
    Private BARCODE As String = ""

    Private Sub LogToRTB(ByVal strMessage As String)
        rtb.AppendText(strMessage & Environment.NewLine)
        Application.DoEvents()
    End Sub

    Private Function LineBreak(ByVal intSelect As Short) As String
        Dim strLine As String = ""
        Dim intLength As Short = 80
        If intSelect = 0 Then intLength = intLength + 20
        For i As Short = 0 To intLength
            If intSelect = 0 Then
                strLine += "-"
            Else
                strLine += "="
            End If
        Next

        Return strLine
    End Function

    Private Function NewLine() As String
        Return Environment.NewLine
    End Function

    Public Sub ProcessRecord(ByVal back_ocr_ As String, ByVal barcode_ As String)
        BACK_OCR = back_ocr_
        BARCODE = barcode_

        'LogToRTB("Binding ansi files...")
        BindFingerprintANSI()

        If Not blnSuccess Then Exit Sub

        LogToRTB("Matching templates...")
        MatchTemplate()

        LogToRTB(back_ocr_ & " " & barcode_ & " " & blnSuccess.ToString & " " & Now.ToString & NewLine())
        LogToRTB(LineBreak(0) & NewLine())
        Application.DoEvents()
    End Sub

    Private Sub BindFingerprintANSI()
        Try
            Dim folder As String = BACK_OCR

            'Dim sr As New IO.StreamReader("C:\Allcardtech\UMIDOnlineInquiry\UMIDOnlineInquiry.txt")
            'Do While Not sr.EndOfStream
            '    Dim strLine As String = sr.ReadLine.Trim
            '    If strLine.Trim <> "" Then
            '        folder = strLine.Split("|")(0)
            '        BARCODE = strLine.Split("|")(1)
            '    End If
            'Loop
            'sr.Dispose()
            'sr.Close()

            Dim strRepository As String = My.Settings.UMID_Raw + "\" + folder + "\" + BARCODE

            If IO.Directory.Exists(strRepository) Then
                BindANSIFMR(strRepository & "\" & BARCODE & "_Lprimary.ansi-fmr", FingerPrintData_LeftPrim)
                BindANSIFMR(strRepository & "\" & BARCODE & "_Lbackup.ansi-fmr", FingerPrintData_LeftBak)
                BindANSIFMR(strRepository & "\" & BARCODE & "_Rprimary.ansi-fmr", FingerPrintData_RightPrim)
                BindANSIFMR(strRepository & "\" & BARCODE & "_Rbackup.ansi-fmr", FingerPrintData_RightBak)

                BindPhoto(strRepository & "\" & BARCODE & "_Photo.jpg")

                blnSuccess = True
            Else
                blnSuccess = False
                strMessage = "Unable to find fingerprint records"
                SaveLog("Unable to find fingerprint records", 1)
                LogToRTB(strMessage)
                SaveResult()
            End If
        Catch ex As Exception
            blnSuccess = False
            strMessage = ex.Message
            SaveLog(strMessage, 0)
            LogToRTB(strMessage)
        End Try

    End Sub

    Private Sub BindFingerprintANSI2()
        Try
            BACK_OCR = "UMID-TEST"
            BARCODE = "0120090819b0idd04007"

            Dim folder As String = BACK_OCR

            'Dim sr As New IO.StreamReader("C:\Allcardtech\UMIDOnlineInquiry\UMIDOnlineInquiry.txt")
            'Do While Not sr.EndOfStream
            '    Dim strLine As String = sr.ReadLine.Trim
            '    If strLine.Trim <> "" Then
            '        folder = strLine.Split("|")(0)
            '        BARCODE = strLine.Split("|")(1)
            '    End If
            'Loop
            'sr.Dispose()
            'sr.Close()

            Dim strRepository As String = My.Settings.UMID_Raw + folder + "\" + BARCODE

            If IO.Directory.Exists(strRepository) Then
                BindANSIFMR(strRepository & "\" & BARCODE & "_Lprimary.ansi-fmr", FingerPrintData_LeftPrim)
                BindANSIFMR(strRepository & "\" & BARCODE & "_Lbackup.ansi-fmr", FingerPrintData_LeftBak)
                BindANSIFMR(strRepository & "\" & BARCODE & "_Rprimary.ansi-fmr", FingerPrintData_RightPrim)
                BindANSIFMR(strRepository & "\" & BARCODE & "_Rbackup.ansi-fmr", FingerPrintData_RightBak)

                blnSuccess = True
            Else
                blnSuccess = False
                strMessage = "Unable to find fingerprint records"
                SaveLog("Unable to find fingerprint records", 1)
                LogToRTB(strMessage)
                SaveResult()
            End If
        Catch ex As Exception
            blnSuccess = False
            strMessage = ex.Message
            SaveLog(strMessage, 0)
            LogToRTB(strMessage)
        End Try

    End Sub

    Private Sub BindANSIFMR(ByVal strFile As String, ByRef ansi() As Byte)
        If IO.File.Exists(strFile) Then
            ansi = IO.File.ReadAllBytes(strFile)

            'Dim fs As System.IO.FileStream
            'fs = New System.IO.FileStream(strFile, System.IO.FileMode.Open, System.IO.FileAccess.Read)
            ''a byte array to read the image
            'ansi = New Byte(fs.Length - 1) {}
            'fs.Read(ansi, 0, System.Convert.ToInt32(fs.Length))
            'fs.Close()
        Else
            blnSuccess = False
            strMessage = "Failed to load ansi from webservice"
            SaveLog(strMessage, 0)
        End If
    End Sub

    Private Sub BindPhoto(ByVal strFile As String)
        If IO.File.Exists(strFile) Then
            imgPhoto = IO.File.ReadAllBytes(strFile)
        Else
            blnSuccess = False
            strMessage = "Failed to load ansi from webservice"
            SaveLog(strMessage, 0)
        End If
    End Sub

    Private Sub SaveLog(ByVal strLog As String, ByVal intSelect As Short)
        Dim sw As New IO.StreamWriter(tempFolder & "\SystemLog.txt", True)
        If intSelect = 1 Then 'system log
            sw.WriteLine(Now.ToString & ", System, " & strLog)
        Else 'error log
            sw.WriteLine(Now.ToString & ", Error, " & strLog)
        End If
        sw.Dispose()
        sw.Close()
    End Sub

    Private Sub SaveResult()
        'Dim strFile As String = tempFolder & "\Result - 0.txt"
        ''If IO.File.Exists(strFile) Then IO.File.Delete(strFile)
        ''If IO.File.Exists(strFile.Replace("\Result - 0.txt", "\Result - 1.txt")) Then IO.File.Delete(strFile.Replace("\Result - 0.txt", "\Result - 1.txt"))

        'Dim sw As New IO.StreamWriter(strFile, True)

        'If blnSuccess Then strFile = strFile.Replace("\Result - 0.txt", "\Result - 1.txt")

        'If blnSuccess Then
        '    sw.WriteLine("")
        'Else
        '    sw.WriteLine(ErrorMessage)
        'End If

        'sw.Dispose()
        'sw.Close()
        'LogToRTB("updating")

        'Dim strRepository As String = My.Settings.UMID_Raw + "\" + BACK_OCR + "\" + BARCODE
        Dim rawdata As String = ""

        'hold
        'If IO.Directory.Exists(strRepository) Then
        '    If IO.File.Exists(strRepository & "\" & BARCODE & "_Demographic") Then

        '        Dim objReader As IO.StreamReader

        '        Try
        '            objReader = New IO.StreamReader(strRepository & "\" & BARCODE & "_Demographic")
        '            rawdata = objReader.ReadToEnd
        '            objReader.Close()
        '        Catch ex As Exception

        '        End Try
        '    End If
        'End If


        Dim dbCon As New DBCon
        If Not dbCon.UpdateSecugenFingerprintMatcher(BACK_OCR, BARCODE, blnSuccess, strMessage, rawdata, imgPhoto) Then
            LogToRTB(strMessage)
            'SaveLog(strMessage, 0)
            'SaveResult()
        End If
    End Sub

    Public Sub MatchTemplate()
        Try
            Dim iError As Int32

            'PopulateComboboxDeviceName()

            If Not InitializeFingerprint() Then Exit Sub

            'Dim ansiFile As String = tempFolder & "\" & BARCODE & "_ansi.ansi-fmr"

            'TEMPLATE TO TEMPLATE MATCHING
            '==========================================
            Dim matched As Boolean

            Dim dbCon As New DBCon

            'Dim SecugenMin() As Byte = IO.File.ReadAllBytes(ansiFile)
            Dim SecugenMin() As Byte

            If Not dbCon.GetANSIFile(BACK_OCR, BARCODE, SecugenMin) Then
                blnSuccess = False
                Exit Sub
            End If

            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_LeftPrim, 0, SGFPMSecurityLevel.NORMAL, matched)

            'match left primary
            If Not MatchedFingerprints(iError, matched) Then
                'match right primary
                iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_RightPrim, 0, SGFPMSecurityLevel.NORMAL, matched)

                If Not MatchedFingerprints(iError, matched) Then
                    'match left backup
                    iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_LeftBak, 0, SGFPMSecurityLevel.NORMAL, matched)

                    If Not MatchedFingerprints(iError, matched) Then
                        'match right backup
                        iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_RightBak, 0, SGFPMSecurityLevel.NORMAL, matched)

                        If Not MatchedFingerprints(iError, matched) Then
                            strMessage = "Fingerprints not matched"
                            'SaveLog(strMessage, 1)
                            LogToRTB(strMessage)
                            blnSuccess = False
                        Else
                            blnSuccess = True
                        End If
                    Else
                        blnSuccess = True
                    End If
                Else
                    blnSuccess = True
                End If
            Else
                blnSuccess = True
            End If

            SaveResult()
        Catch ex As Exception
            blnSuccess = False
            strMessage = ex.Message
            'SaveLog(strMessage, 0)
            LogToRTB(strMessage)
            SaveResult()
        End Try
    End Sub

    Private pic_Finger As New PictureBox

    Public Function CaptureFingerprintAndMatchToUMID() As Boolean

        BindFingerprintANSI2()

        'PopulateComboboxDeviceName()
        If Not InitializeFingerprint() Then Exit Function

        Dim iError As Int32
        Dim elap_time As Int32
        Dim timeout As Int32
        Dim quality As Int32
        Dim img_qlty As Int32

        Dim SecugenMin() As Byte

        iError = m_FPM.GetMaxTemplateSize(m_MaxTemplateSize)
        Application.DoEvents()

        ReDim SecugenMin(m_MaxTemplateSize)

        ReDim fp_image(m_ImageWidth * m_ImageHeight)

        timeout = Convert.ToInt32(10000)
        quality = Convert.ToInt32(80)
        elap_time = Environment.TickCount

        iError = m_FPM.SetLedOn(True)

        iError = m_FPM.GetImageEx(fp_image, timeout, pic_Finger.Handle.ToInt32(), quality)
        Application.DoEvents()


        If (iError = SGFPMError.ERROR_NONE) Then
            System.Threading.Thread.Sleep(100)

            m_FPM.GetImageQuality(m_ImageWidth, m_ImageHeight, fp_image, img_qlty)

            Dim finger_info As New SGFPMFingerInfo()
            finger_info.FingerNumber = SGFPMFingerPosition.FINGPOS_UK
            finger_info.ImageQuality = CShort(img_qlty)
            finger_info.ImpressionType = CShort(SGFPMImpressionType.IMPTYPE_LP)
            finger_info.ViewNumber = 1

            ClearBuffer(SecugenMin)

            'Get ANSI Template
            iError = m_FPM.CreateTemplate(finger_info, fp_image, SecugenMin)

            If (iError = SGFPMError.ERROR_NONE) Then

                'label_Status.Text = "Matching fingerprint..."
                Application.DoEvents()
                'If IO.File.Exists(Path_Fingerprint) Then
                'FingerPrintData = IO.File.ReadAllBytes(Path_Fingerprint)
                'End If
                'End If

                'WriteANSI(SecugenMin)

                Dim matched As Boolean



                iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_LeftPrim, 0, SGFPMSecurityLevel.NORMAL, matched)
                'iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, SecugenMin, 0, SGFPMSecurityLevel.NORMAL, matched)

                'match left primary
                If Not MatchedFingerprints(iError, matched) Then
                    'match right primary
                    iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_RightPrim, 0, SGFPMSecurityLevel.NORMAL, matched)

                    If Not MatchedFingerprints(iError, matched) Then
                        'match left backup
                        iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_LeftBak, 0, SGFPMSecurityLevel.NORMAL, matched)

                        If Not MatchedFingerprints(iError, matched) Then
                            'match right backup
                            iError = m_FPM.MatchAnsiTemplate(SecugenMin, 0, FingerPrintData_RightBak, 0, SGFPMSecurityLevel.NORMAL, matched)

                            If Not MatchedFingerprints(iError, matched) Then

                                SaveLog("Fingerprints not matched", 1)
                                strMessage = "Failed to authenticate card"
                                blnSuccess = False
                            Else
                                blnSuccess = True
                            End If
                        Else
                            blnSuccess = True
                        End If
                    Else
                        blnSuccess = True
                    End If
                Else
                    blnSuccess = True
                End If
            Else
                'strMessage = DisplayError("CreateTemplate", iError)

                SaveLog(DisplayError("CreateTemplate", iError), 1)
                strMessage = "Failed to capture fingerprint"
            End If




        Else
            'strMessage = DisplayError("CreateTemplate", iError)
            SaveLog(DisplayError("CreateTemplate", iError), 1)
            strMessage = "Failed to capture fingerprint"

        End If



        If blnSuccess = True Then
            iError = m_FPM.SetLedOn(False)
        End If

        Return blnSuccess

    End Function

    Private Sub WriteANSI(ByVal ansi() As Byte)
        Dim FS1 As New System.IO.FileStream("D:\secugen.ansi-fmr", System.IO.FileMode.Create)
        Dim blob As Byte() = ansi
        FS1.Write(blob, 0, blob.Length)
        FS1.Close()
        FS1 = Nothing
    End Sub


End Class
