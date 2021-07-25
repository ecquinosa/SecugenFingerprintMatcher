
Imports SecuGen.FDxSDKPro.Windows

Public Class Form2

    Private ansi1 As String = ""
    Private ansi2 As String = ""

    Private Sub Button1_Click(sender As System.Object, e As System.EventArgs) Handles Button1.Click
        If ansi1 = "" Then Return
        If ansi2 = "" Then Return

        lblRESULT.Text = ""
        If MatchFileTemplate(ansi1, ansi2) Then
            'MessageBox.Show("Matched!")
            lblRESULT.Text = "MATCHED!"
            lblRESULT.ForeColor = Color.Green
        Else
            'MessageBox.Show("Not matched!")
            lblRESULT.Text = "NOT MATCHED!"
            lblRESULT.ForeColor = Color.OrangeRed
        End If
    End Sub

    Public Function MatchFileTemplate(ByVal ansiTemplate1 As String, ByVal ansiTemplate2 As String) As Boolean
        Try
            Dim ansiFromDevice As Byte() = IO.File.ReadAllBytes(ansiTemplate1)
            Dim ansiFromFile As Byte() = IO.File.ReadAllBytes(ansiTemplate2)

            Dim strErrorMessage As String = ""

            Dim bln As Boolean = SecugenMatchFileTemplate(ansiFromDevice, ansiFromFile, strErrorMessage)

            If bln Then
                Return True
            Else
                If strErrorMessage <> "" Then
                    'SharedFunction.SaveToErrorLog(SharedFunction.TimeStamp & "|" & "MatchFileTemplate(): " & strErrorMessage & "|" & kioskIP & "|" & getbranchCoDE_1 & "|" & cardType)
                    'Dim _showMsg As New Action(AddressOf ShowMatchFileTemplateError)
                    '_showMsg.Invoke("")
                End If
                Return False
            End If
        Catch ex As Exception
            'SharedFunction.SaveToErrorLog(SharedFunction.TimeStamp & "|" & "Runtime error MatchFileTemplate(R): " & ex.Message & "|" & kioskIP & "|" & getbranchCoDE_1 & "|" & cardType)
            'Dim _showMsg As New Action(AddressOf ShowMatchFileTemplateError)
            '_showMsg.Invoke("R")
            Return False
        End Try

    End Function

    Private m_FPM As New SGFingerPrintManager
    Private m_ImageWidth As Int32
    Private m_ImageHeight As Int32

    Private Function SecugenMatchFileTemplate(ByVal ansiFile1 As Byte(), ByVal ansiFile2 As Byte(), ByVal ErrorMessage As String) As Boolean
        Try
            Dim iError As Int32
            Dim matched As Boolean

            iError = m_FPM.InitEx(m_ImageWidth, m_ImageHeight, 500)
            iError = m_FPM.MatchAnsiTemplate(ansiFile1, 0, ansiFile2, 0, SGFPMSecurityLevel.NORMAL, matched)
            'Application.DoEvents()

            If MatchedFingerprints(iError, matched) Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            ErrorMessage = ex.Message
            Return False
        End Try

    End Function

    Private Function MatchedFingerprints(ByVal iError As Integer, ByVal matched As Boolean) As Boolean
        If iError = SGFPMError.ERROR_NONE Then
            If matched Then
                Return True
            Else
                'strErrorMessage = "Template Matching Failed"
                Return False
            End If
        Else
            'strErrorMessage = "Template Matching Failed"
            Return False
        End If
    End Function

    Private Sub Button2_Click(sender As System.Object, e As System.EventArgs) Handles Button2.Click
        ansi1 = ""
        BrowseFile(TextBox1)
        ansi1 = TextBox1.Text
        TextBox1.Text = Microsoft.VisualBasic.Right(TextBox1.Text, 90)
    End Sub

    Private Sub Button3_Click(sender As System.Object, e As System.EventArgs) Handles Button3.Click
        ansi2 = ""
        BrowseFile(TextBox2)
        ansi2 = TextBox2.Text
        TextBox2.Text = Microsoft.VisualBasic.Right(TextBox2.Text, 90)
    End Sub

    Private Sub BrowseFile(ByVal txtbox As TextBox)
        Dim ofd As New OpenFileDialog
        If ofd.ShowDialog = Windows.Forms.DialogResult.OK Then
            txtbox.Text = ofd.FileName
        End If
        ofd.Dispose()
        ofd = Nothing
    End Sub

End Class