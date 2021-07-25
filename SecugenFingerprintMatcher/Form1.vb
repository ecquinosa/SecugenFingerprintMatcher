Public Class Form1

    Private IsProcessFree As Boolean = True

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        TextBox1.Text = My.Settings.UMID_Raw
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

    Private Sub ProcessTable()
        Dim dbCon As New DBCon
        Dim dt As DataTable
        If dbCon.SelectSecugenFingerprintMatcher(dt) Then
            If dt.DefaultView.Count > 0 Then

                rtb.AppendText(dt.DefaultView.Count.ToString & " record(s) found   " & Now.ToString() & NewLine())
                rtb.AppendText(LineBreak(0) & NewLine())
                Application.DoEvents()

                For Each rw As DataRow In dt.Rows
                    Dim fingerprintauth As New FingerprintAuth(rtb)
                    fingerprintauth.ProcessRecord(rw("back_ocr"), rw("barcode"))
                Next
            End If
        End If
    End Sub

    Private Sub Timer1_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If IsProcessFree Then
            IsProcessFree = False
            'Dim fi As New FingerprintAuth()
            'fi.MatchTemplate()
            ProcessTable()
            IsProcessFree = True
        End If
    End Sub

    Private Sub StartToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StartToolStripMenuItem.Click
        StartToolStripMenuItem.Visible = False
        StopToolStripMenuItem.Visible = True
        Timer1.Start()
    End Sub

    Private Sub StopToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StopToolStripMenuItem.Click
        StartToolStripMenuItem.Visible = True
        StopToolStripMenuItem.Visible = False
        Timer1.Stop()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Dim f As New FingerprintAuth(rtb)
        f.CaptureFingerprintAndMatchToUMID()
        MessageBox.Show(f.IsSuccess)
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        My.Settings.UMID_Raw = TextBox1.Text
        My.Settings.Save()
    End Sub
End Class
