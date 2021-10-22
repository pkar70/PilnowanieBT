' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

Imports Windows.Storage
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class ListaLoad
    Inherits Page

    Public Class PlikListy
        Public Property sFileName As String
        Public Property bSelected As Boolean = False
    End Class

    Public moListaPlikow As ObservableCollection(Of PlikListy)

    Private Async Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        ' wczytanie zaznaczonych list
        Dim sBufor = ""

        Dim oFold = App.GetRoamingFolder(True)
        If oFold IsNot Nothing Then
            For Each oItem In moListaPlikow
                If oItem.bSelected Then
                    Dim oFile = Await oFold.GetFileAsync(oItem.sFileName)
                    Dim sFile = Await FileIO.ReadTextAsync(oFile)
                    sBufor = sBufor & sFile & vbCrLf
                End If
            Next
        End If

        App.msImportBuffer = sBufor
        Me.Frame.GoBack()
    End Sub

    Private Sub uiPage_Loaded(sender As Object, e As RoutedEventArgs)
        ' wypeln liste nazwami plikow

        Dim aListy = App.GetSettingsString("ListaList").Split("|")

        For Each sFile In aListy
            If sFile.Length > 1 Then
                Dim oNew = New PlikListy
                oNew.sFileName = sFile
            End If
        Next

        ListaItems.ItemsSource = From c In moListaPlikow

    End Sub
End Class
