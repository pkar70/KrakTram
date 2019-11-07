using System;
//using System.Collections.Generic;
using System.IO;
//using System.Threading.Tasks;
//using Microsoft.VisualBasic;
using System.Xml.Serialization;

[XmlType("stop")]
public partial class FavStop
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

public partial class FavStopList
{
    private System.Collections.ObjectModel.Collection<FavStop> Itemy = new System.Collections.ObjectModel.Collection<FavStop>();
    private bool bDirty = false;

    public void Add(string sName, double dLat, double dLon, int iMaxOld)
    {

        // jesli jest (np. tram), to nie dodawaj (np. bus)
        foreach (FavStop oNode in Itemy)
        {
            if ((oNode.Name ?? "") == (sName ?? ""))
                return;
        }

        // nie ma, to dodaj
        var oNew = new FavStop();
        oNew.Lat = dLat;
        oNew.Lon = dLon;
        oNew.Name = sName;
        oNew.maxOdl = iMaxOld;
        Itemy.Add(oNew);
        bDirty = true;
    }

    public void Del(string sName)
    {
        foreach (FavStop oItem in Itemy)
        {
            if ((oItem.Name ?? "") == (sName ?? ""))
            {
                Itemy.Remove(oItem);
                return;
            }
        }
    }

    public async void InitPkar()
    {
        Add("domek", 50.0198, 19.9785, 500);
        Add("meiselsa", 50.0513642, 19.9432361, 600);
        Add("franc3", 50.059781, 19.9339632, 500);
        Add("widok", 50.0789713, 19.8816113, 500);
        await Save(false);
        p.k.SetSettingsBool("pkarmode", true, true); // roaming data
    }

    // Load
    private async System.Threading.Tasks.Task<bool> Load()
    {
        // ret=false gdy nie jest wczytane

        Windows.Storage.StorageFile oObj = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("favs.xml")) as Windows.Storage.StorageFile;
        if (oObj == null)
            return false;
        var oFile = oObj as Windows.Storage.StorageFile;

        var oSer = new XmlSerializer(typeof(System.Collections.ObjectModel.Collection<FavStop>));
        Stream oStream = await oFile.OpenStreamForReadAsync();
        System.Xml.XmlReader oXmlReader = System.Xml.XmlReader.Create(oStream);
        Itemy = oSer.Deserialize(oXmlReader) as System.Collections.ObjectModel.Collection<FavStop>;
        oXmlReader.Dispose();
        bDirty = false;
        return true;
    }

    public async System.Threading.Tasks.Task Save(bool bForce)
    {
        if (!bForce & !bDirty)
            return;

        Windows.Storage.StorageFile oFile = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync("favs.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting);

        if (oFile == null)
            return;

        var oSer = new XmlSerializer(typeof(System.Collections.ObjectModel.Collection<FavStop>));
        Stream oStream = await oFile.OpenStreamForWriteAsync();
        oSer.Serialize(oStream, Itemy);
        oStream.Dispose();   // == fclose
        bDirty = false;
    }

    private async System.Threading.Tasks.Task<bool> Import()
    {
        string sOldVers = p.k.GetSettingsString("favPlaces");
        if (sOldVers.Length < 25)
        {
            p.k.SetSettingsString("favPlaces", "");
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
            await p.k.DialogBox("ERROR loading favourites list");
            return false;
        }

        foreach (Windows.Data.Xml.Dom.IXmlNode oPlace in oXmlPlaces.DocumentElement.SelectNodes("//place"))
            Add(oPlace.SelectSingleNode("@name").NodeValue.ToString(), (double)oPlace.SelectSingleNode("@lat").NodeValue, (double)oPlace.SelectSingleNode("@long").NodeValue, (int)oPlace.SelectSingleNode("@maxOdl").NodeValue );

        await Save(true);
        p.k.SetSettingsString("favPlaces", "");
        await p.k.DialogBoxRes("resImportedOldFav");
        return true;
#else
        return false;   // ale to się nie zdarzy - bo wtedy nie może być OldFav :)
#endif
    }


    public async System.Threading.Tasks.Task LoadOrImport()
    {
        if (await Load())
            return;
        await Import();
    }

    public System.Collections.ObjectModel.Collection<FavStop> GetList()
    {
        return Itemy;
    }
}
