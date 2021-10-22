' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


Imports System.Xml.Serialization
Imports Windows.ApplicationModel.DataTransfer
Imports Windows.Devices.Bluetooth
Imports Windows.Devices.Bluetooth.Advertisement
Imports Windows.Devices.Enumeration
Imports Windows.Storage
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>




Public NotInheritable Class MainPage
    Inherits Page


#Region "Formatka - events"


    Private Sub uiPage_Loaded(sender As Object, e As RoutedEventArgs)
        App.moLista = New ObservableCollection(Of Urzadzenie)
        'ListaItems.ItemsSource = moLista
        'Dim oItem = New Urzadzenie
        'oItem.Nazwa = "alamakota"
        'moLista.Add(oItem)

        'ItemyGrp.Source = From c In moLista Group By c.SortGrp Into Group
        uiStart.Content = "Start"
        uiStart.IsChecked = False

        If App.msImportBuffer <> "" Then ListImport(App.msImportBuffer)

        fromDispatch()

    End Sub

    Private Async Sub uiStart_Click(sender As Object, e As RoutedEventArgs) Handles uiStart.Click
        Select Case uiStart.Label
            Case "Start"
                If Not Await UruchomSkanowanie() Then
                    uiStart.IsChecked = False
                    Exit Sub
                End If
                uiStart.Label = "Stop"
                uiStart.IsChecked = True
            Case Else
                If App.moDevWatcher IsNot Nothing AndAlso App.moDevWatcher.Status = BluetoothLEAdvertisementWatcherStatus.Started Then App.moDevWatcher.Stop()
                uiStart.Label = "Start"
                uiStart.IsChecked = False
        End Select
    End Sub

    Private Sub GlobalObserwuj(bObserwuj As Boolean)
        For Each oItem In App.moLista
            oItem.Pilnowane = bObserwuj
            App.Zakoloruj(oItem)
        Next
    End Sub

    Private Sub uiSelectAll_Click(sender As Object, e As RoutedEventArgs) Handles uiSelectAll.Click
        GlobalObserwuj(True)
        fromDispatch()
    End Sub

    Private Sub uiSelectNone_Click(sender As Object, e As RoutedEventArgs) Handles uiSelectNone.Click
        GlobalObserwuj(False)
        fromDispatch()
    End Sub

    Private Sub uiListLoad_Click(sender As Object, e As RoutedEventArgs) Handles uiListLoad.Click
        App.msImportBuffer = ""
        Me.Frame.Navigate(GetType(ListaLoad))
        'Dim oPick = New Pickers.FileOpenPicker
        ''oPick.SuggestedStartLocation =
        ''ApplicationData.Current.RoamingFolder
        'oPick.FileTypeFilter.Add("txt")
        'Dim oFile = Await oPick.PickSingleFileAsync
        'If oFile Is Nothing Then Exit Sub

        'Dim sTxt = Await FileIO.ReadTextAsync(oFile)
        'ListImport(sTxt)
    End Sub
    Private Async Sub uiListSave_Click(sender As Object, e As RoutedEventArgs) Handles uiListSave.Click
        Dim sName = Await App.DialogBoxInput("resNazwaListy")
        If sName = "" Then Exit Sub

        Dim sTxt = ListExport()
        Dim oFile = Await App.GetRoamingFile(sName & ".txt", True)
        If oFile Is Nothing Then Exit Sub

        Await FileIO.WriteTextAsync(oFile, sTxt)
        Dim sListy = App.GetSettingsString("ListaList")
        sListy = sListy & "|" & sName
        App.SetSettingsString("ListaList", sListy)
    End Sub
    Private Async Sub uiListClear_Click(sender As Object, e As RoutedEventArgs) Handles uiListClear.Click
        If Not Await App.DialogBoxResYN("resAskConfirm") Then Exit Sub
        App.moLista.Clear()
        fromDispatch()
    End Sub

    Private Sub ListImport(sLista As String)
        Dim aArr = sLista.Split(vbCrLf)
        For Each oLine In aArr
            Dim aFld = oLine.Split("|")
            If aFld.GetUpperBound(0) = 1 Then
                Dim oNew = New Urzadzenie
                oNew.Nazwa = aFld(0)
                oNew.Adres = aFld(1)
                oNew.Pilnowane = True

                Dim bBylo = False       ' nie dodajemy, jesli juz jest (np. dwa razy wczytanie listy)
                For Each oItem In App.moLista
                    If oItem.Adres = aFld(1) Then
                        oItem.Pilnowane = True
                        bBylo = True
                        Exit For
                    End If
                Next
                If Not bBylo Then App.moLista.Add(oNew)
            End If
        Next
    End Sub

    Private Async Sub uiListImport_Click(sender As Object, e As RoutedEventArgs) Handles uiListImport.Click
        Dim oClipCont = Clipboard.GetContent
        Dim sTxt = Await oClipCont.GetTextAsync()
        If sTxt Is Nothing Then
            App.DialogBox("errorNoContens")
            Exit Sub
        End If

        ListImport(sTxt)
        fromDispatch()
    End Sub

    Private Function ListExport() As String
        Dim sTxt = ""
        For Each oItem In App.moLista
            sTxt = sTxt & oItem.Nazwa & "|" & oItem.Adres & vbCrLf
        Next
        Return sTxt
    End Function

    Private Sub uiListExport_Click(sender As Object, e As RoutedEventArgs) Handles uiListExport.Click
        Dim sTxt = ListExport()

        Dim oClipCont = New DataPackage
        oClipCont.RequestedOperation = DataPackageOperation.Copy
        oClipCont.SetText(sTxt)
        Clipboard.SetContent(oClipCont)

        App.DialogBoxRes("msgExportClip")

    End Sub

    Private Sub uiItem_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim oItem = TryCast(TryCast(sender, Grid).DataContext, Urzadzenie)
        oItem.Pilnowane = Not oItem.Pilnowane
        'App.Zakoloruj(oItem)
        fromDispatch()
    End Sub

#End Region

    Public Sub fromDispatch()
        For Each oItem In App.moLista
            App.Zakoloruj(oItem)
        Next
        ListaItems.ItemsSource = From c In App.moLista
    End Sub

    Public Async Sub toDispatch()
        Await ListaItems.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, AddressOf fromDispatch)
    End Sub

    Private Async Sub BTwatch_Received(sender As BluetoothLEAdvertisementWatcher, oArgs As BluetoothLEAdvertisementReceivedEventArgs)
        Debug.WriteLine("Received: address=" & oArgs.BluetoothAddress & ", RSSI=" & oArgs.RawSignalStrengthInDBm)

        Dim oDev As BluetoothLEDevice
        Dim iRSSI = oArgs.RawSignalStrengthInDBm
        Dim sNoName = App.GetLangString("msgNoName")

        For Each oItem In App.moLista
            If oItem.Adres = oArgs.BluetoothAddress Then
                oItem.RSSIcurrent = iRSSI
                oItem.RSSImax = Math.Max(oItem.RSSImax, iRSSI)
                oItem.RSSImin = Math.Min(oItem.RSSImin, iRSSI)
                If oItem.Nazwa = sNoName Then
                    ' sprobuj dodac nazwe
                    oDev = Await BluetoothLEDevice.FromBluetoothAddressAsync(oArgs.BluetoothAddress)
                    If oDev.Name IsNot Nothing AndAlso oDev.Name <> "" Then
                        oItem.Nazwa = oDev.Name
                        toDispatch()
                    End If
                    ' jesli sie udalo, redraw
                End If
                Exit Sub
            End If
        Next

        ' nie ma na liscie, to nalezy go dodac

        Dim oNew = New Urzadzenie
        oNew.Nazwa = sNoName  ' na razie, dopoki nie znajdzie
        oNew.Adres = oArgs.BluetoothAddress
        oNew.RSSIcurrent = iRSSI
        oNew.RSSImax = iRSSI
        oNew.RSSImin = iRSSI
        oDev = Await BluetoothLEDevice.FromBluetoothAddressAsync(oArgs.BluetoothAddress)
        If oDev.Name IsNot Nothing AndAlso oDev.Name <> "" Then oNew.Nazwa = oDev.Name

        toDispatch()

    End Sub

    Private Async Function UruchomSkanowanie() As Task(Of Boolean)
        If Not Await App.IsNetBTavailable(True) Then Return False

        App.moDevWatcher = New BluetoothLEAdvertisementWatcher
        App.moDevWatcher.ScanningMode = 0   ' tylko czeka, 1: żąda wysłania adv

        AddHandler App.moDevWatcher.Received, AddressOf BTwatch_Received
        App.moDevWatcher.Start()
        Return True
        ' o wersji background: https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/ble-beacon

    End Function


End Class
