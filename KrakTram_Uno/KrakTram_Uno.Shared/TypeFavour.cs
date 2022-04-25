using System;
using System.IO;
using System.Xml.Serialization;
using vb14 = VBlib.pkarlibmodule14;

// Kolejne wersje:
// GetSettingsString("favPlaces") i w nim XML
// LocalCacheFolder("favs.xml"), dalej XML
// 2022.01.31, LocalFolder("favs.json") plus import z pliku XML oraz ze zmiennej (WinSTORE, nie ma AntroSTORE!)
// 2022.04.xx, zmiana app wymuszona przez podział przystanku na słupki - 

// pozostałość po poprzednim formacie (XML), potrzebne dla Import

namespace KrakTram

{

    [XmlType("stop")]
    public class FavStopXML
    {
        [XmlAttribute()]
        public double Lat { get; set; }
        [XmlAttribute()]
        public double Lon { get; set; }
        [XmlAttribute()]
        public string Name { get; set; }
        [XmlAttribute()]
        public int maxOdl { get; set; }
    }

    /// <summary>Tylko na potrzeby migracji danych do JSON i VBLib</summary>

    public class FavStopListXML
    {
        private static System.Collections.ObjectModel.Collection<FavStopXML> Itemy = new System.Collections.ObjectModel.Collection<FavStopXML>();

        public static async System.Threading.Tasks.Task<bool> Load()
        {
            // ret=false gdy nie jest wczytane

            Windows.Storage.StorageFile oObj = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("favs.xml")) as Windows.Storage.StorageFile;
            if (oObj == null)
                return false;
            Windows.Storage.StorageFile oFile = oObj as Windows.Storage.StorageFile;

            try
            {
                XmlSerializer oSer = new XmlSerializer(typeof(System.Collections.ObjectModel.Collection<FavStopXML>));
                Stream oStream = await oFile.OpenStreamForReadAsync();
                System.Xml.XmlReader oXmlReader = System.Xml.XmlReader.Create(oStream);
                Itemy = oSer.Deserialize(oXmlReader) as System.Collections.ObjectModel.Collection<FavStopXML>;
                oXmlReader.Dispose();
            }
            catch
            {
                vb14.DialogBox("ERROR reading fav file?");
                return false;
            }

            foreach (var oXmlItem in Itemy)
            {
                App.oFavour.Add(oXmlItem.Name, oXmlItem.Lat, oXmlItem.Lon, oXmlItem.maxOdl);
            }
            return true;
        }

#if false
        public static async System.Threading.Tasks.Task<bool> Import()
        {
            string sOldVers = vb14.GetSettingsString("favPlaces");
            if (sOldVers.Length < 25)
            {
                vb14.SetSettingsString("favPlaces", "");
                return false;
            }

#if NETFX_CORE
            // skoro jest zmienna, to nalezy to zaimportowac - ale moze byc tylko na Windows...
            bool bError = false;
            var oXmlPlaces = new Windows.Data.Xml.Dom.XmlDocument();
            try
            {
                oXmlPlaces.LoadXml(sOldVers);
            }
            catch
            {
                bError = true;
            }
            if (bError)
            {
                await VBlib.pk.DialogBoxAsync("ERROR loading favourites list");
                return false;
            }

            foreach (Windows.Data.Xml.Dom.IXmlNode oPlace in oXmlPlaces.DocumentElement.SelectNodes("//place"))
            {
                App.oFavour.Add(oPlace.SelectSingleNode("@name").NodeValue.ToString(), (double)oPlace.SelectSingleNode("@lat").NodeValue, (double)oPlace.SelectSingleNode("@long").NodeValue, (int)oPlace.SelectSingleNode("@maxOdl").NodeValue);
            }

            VBlib.pk.SetSettingsString("favPlaces", "");
            await VBlib.pk.DialogBoxResAsync("resImportedOldFav");
            return true;
#else
            return false;   // ale to się nie zdarzy - bo wtedy nie może być OldFav :)
#endif
        }
#endif 


    }

}
