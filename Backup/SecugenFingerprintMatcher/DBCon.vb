
Imports System.Data.SqlClient

Public Class DBCon

    Private con As SqlConnection
    Private cmd As SqlCommand
    Private da As SqlDataAdapter

    Private Sub CloseConnection()
        If Not cmd Is Nothing Then cmd.Dispose()
        If Not da Is Nothing Then da.Dispose()
        If con.State = ConnectionState.Open Then con.Close()
    End Sub

    Private Sub Cmd_ExecuteNonQuery(ByVal cmdType As CommandType)
        cmd.CommandType = cmdType

        con.Open()
        cmd.ExecuteNonQuery()
        con.Close()
    End Sub

    Private Sub FillDataAdapter(ByVal cmdType As CommandType, ByRef dt As DataTable)
        cmd.CommandType = cmdType

        da.Fill(dt)
    End Sub

    Private Function ConStr() As String
        'Return "Server=192.168.40.7;Database=DataRep_Images_DEV;Uid=sa;Pwd=P@ssw0rd2008;"
        'Return "Server=MIS-PC\NETDEVSQLSERVER;Database=DataRep_Images_DEV;Uid=sa;Pwd=password2011;"
        Return "Server=EDEL-PC\ACTDEVSQLSERVER;Database=DataRepository;Uid=sa;Pwd=password2013;"
    End Function

    Public Function SelectSecugenFingerprintMatcher(ByRef dt As DataTable) As Boolean
        Try
            Dim con As New SqlClient.SqlConnection(ConStr)
            Dim cmd As New SqlClient.SqlCommand("prcSelectSecugenFingerprintMatcher", con)
            Dim ds As New DataSet
            Dim da As New SqlClient.SqlDataAdapter(cmd)
            cmd.CommandType = CommandType.StoredProcedure

            con.Open()
            da.Fill(ds, "Result")
            con.Close()

            dt = ds.Tables("Result")

            Return True
        Catch ex As Exception
            'strErrorMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function IsProcessSecugenFingerprintMatcherTable(ByRef bln As Boolean) As Boolean
        Try
            Dim con As New System.Data.SqlClient.SqlConnection(ConStr)
            Dim cmd As New System.Data.SqlClient.SqlCommand("prcIsProcessSecugenFingerprintMatcherTable", con)
            cmd.CommandType = CommandType.StoredProcedure

            Dim param As SqlClient.SqlParameter
            param = New SqlClient.SqlParameter("@bln", SqlDbType.Bit)
            param.Direction = ParameterDirection.Output
            cmd.Parameters.Add(param)

            con.Open()
            bln = CType(cmd.ExecuteScalar, Boolean)
            con.Close()

            Return True
        Catch ex As Exception
            'strErrorMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function GetANSIFile(ByVal back_ocr As String, ByVal barcode As String, ByRef ansi() As Byte) As Boolean
        Try
            Dim con As New System.Data.SqlClient.SqlConnection(ConStr)
            Dim cmd As New System.Data.SqlClient.SqlCommand("SELECT ansi FROM dbo.tblSecugenFingerprintMatcher WHERE back_ocr ='" & back_ocr & "' AND barcode = '" & barcode & "'", con)
            cmd.CommandType = CommandType.Text
            con.Open()
            ansi = CType(cmd.ExecuteScalar, Byte())
            con.Close()

            Return True
        Catch ex As Exception
            'strErrorMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function AddSecugenFingerprintMatcher(ByVal back_ocr As String, ByVal barcode As String, _
                                                  ByVal ansi() As Byte) As Boolean
        Try
            Dim con As New System.Data.SqlClient.SqlConnection(ConStr)
            Dim cmd As New System.Data.SqlClient.SqlCommand("prcAddSecugenFingerprintMatcher", con)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@back_ocr", back_ocr)
            cmd.Parameters.AddWithValue("@barcode", barcode)
            cmd.Parameters.AddWithValue("@ansi", ansi)

            con.Open()
            cmd.ExecuteNonQuery()
            con.Close()

            Return True
        Catch ex As Exception
            'strErrorMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function UpdateSecugenFingerprintMatcher(ByVal back_ocr As String, ByVal barcode As String, _
                                              ByVal result As Boolean, ByVal message As String, _
                                              ByVal rawdata As String, ByVal imgPhoto As Byte()) As Boolean
        Try
            Dim con As New System.Data.SqlClient.SqlConnection(ConStr)
            Dim cmd As New System.Data.SqlClient.SqlCommand("prcUpdateSecugenFingerprintMatcher", con)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@back_ocr", back_ocr)
            cmd.Parameters.AddWithValue("@barcode", barcode)
            cmd.Parameters.AddWithValue("@result", result)
            cmd.Parameters.AddWithValue("@message", message)
            cmd.Parameters.AddWithValue("@rawdata", rawdata)
            cmd.Parameters.AddWithValue("@Photo", imgPhoto)

            con.Open()
            cmd.ExecuteNonQuery()
            con.Close()

            Return True
        Catch ex As Exception
            'strErrorMessage = ex.Message
            Return False
        End Try
    End Function

End Class
