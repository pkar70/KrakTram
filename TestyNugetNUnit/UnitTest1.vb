Imports NUnit.Framework
Imports pkar.MpkMain
Imports pkar.MpkWrap

Namespace TestyNugetNUnit

    Public Class Tests

        'Private _mpk As New MPK
        Private _mpk As New MPK_Merged

        <SetUp>
        Public Sub Setup()
        End Sub

        '<Test>
        'Public Sub Test1()
        '    Assert.Pass()
        'End Sub

        <Test>
        Async Function TestPrzystankiBus() As Task

            Dim przystanki As List(Of Przystanek) = Await _mpk.DownloadListaPrzystankowAsync '(False, True)

            Assert.IsNotNull(przystanki, "Lista przystanków BUS jest NULL")
            Assert.IsTrue(przystanki.Count > 0, "Brak przystanków BUS")

            Dim daunaPrzystanek As Przystanek = przystanki.FirstOrDefault(Function(p) p.name.Contains("Dauna"))

            Assert.IsTrue(daunaPrzystanek IsNot Nothing, "Brak przystanku 'Dauna' w liœcie przystanków BUS")
            ' Assert.IsTrue(daunaPrzystanek.shortName = "632", "Zmiana identyfikatora przystanku 'Dauna' w liœcie przystanków BUS - jest " & daunaPrzystanek.shortName)

        End Function

        <Test>
        Async Function TestPrzystankiTram() As Task

            Dim przystanki As List(Of Przystanek) = Await _mpk.DownloadListaPrzystankowAsync '(True, False)

            Assert.IsNotNull(przystanki, "Lista przystanków TRAM jest NULL")
            Assert.IsTrue(przystanki.Count > 0, "Brak przystanków TRAM")

            Dim daunaPrzystanek As Przystanek = przystanki.FirstOrDefault(Function(p) p.name.Contains("Dauna"))

            Assert.IsTrue(daunaPrzystanek IsNot Nothing, "Brak przystanku 'Dauna' w liœcie przystanków TRAM")
            'Assert.IsTrue(daunaPrzystanek.shortName = "632", "Zmiana identyfikatora przystanku 'Dauna' w liœcie przystanków TRAM - jest " & daunaPrzystanek.shortName)

        End Function

        <Test>
        Async Function TestOdjazdyDaunaTram() As Task

            Dim tabliczka As MpkTabliczka = Await _mpk.WczytajTabliczkeAsync(False, "dauna01") '"632")

            Assert.IsNotNull(tabliczka, "Lista odjazdów Dauna TRAM jest NULL")
            Assert.IsTrue(tabliczka.actual.Count > 0, "Brak odjazdów Dauna TRAM")
        End Function


        <Test>
        Async Function TestOdjazdyDaunaAll() As Task
            Dim oPrzyst As New Przystanek With {.Name = "Dauna", .Ami_Count = 4}
            Dim tabliczka As MpkTabliczka = Await _mpk.WczytajTabliczkeAsync(oPrzyst)

            Assert.IsNotNull(tabliczka, "Lista odjazdów Dauna (all) jest NULL")
            Assert.IsTrue(tabliczka.actual.Count > 0, "Brak odjazdów Dauna (all)")
        End Function


        <Test>
        Async Function TestOdjazdyDaunaBUS() As Task

            Dim tabliczka As MpkTabliczka = Await _mpk.WczytajTabliczkeAsync(True, "dauna03") '"632")

            Assert.IsNotNull(tabliczka, "Lista odjazdów Dauna BUS jest NULL")
            Assert.IsTrue(tabliczka.actual.Count > 0, "Brak odjazdów Dauna BUS")
        End Function

        <Test>
        Async Function TestZmiany() As Task

            Dim zmiany As List(Of MpkZmiana) = Await _mpk.DownloadZmianyAsync

            Assert.IsNotNull(zmiany, "Lista zmian NULL")
            Assert.IsTrue(zmiany.Count > 0, "Brak zmian linii")
        End Function

        <Test>
        Async Function TestTrasy() As Task

            Dim zmiany As List(Of String) = Await _mpk.DownloadTrasaLiniiAsync("173")

            Assert.IsNotNull(zmiany, "Trasa NULL")
            Assert.IsTrue(zmiany.Count > 0, "Pusta trasa 173")
        End Function





    End Class

End Namespace