using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YellowPagesCrawler
{
    public class Cache
    {
        private const string PATH_FILE = "Cache.xml";

        private XmlDocument XML;

        private bool loaded = false;
        public bool Loaded
        {
            get { return loaded; }
        }

        public string UrlLocation;
        public string ZipCodes;
        public string City;
        public string State;
        public int TrendListIndex;
        public int TrendIndex;

        public Cache()
        {
            XML = new XmlDocument();
            if (File.Exists(PATH_FILE))
            {
                XML.Load(PATH_FILE);
                UrlLocation = XML.DocumentElement.Attributes["urllocation"].InnerText;
                ZipCodes = XML.DocumentElement.Attributes["zipcodes"].InnerText;
                City = XML.DocumentElement.Attributes["city"].InnerText;
                State = XML.DocumentElement.Attributes["state"].InnerText;
                TrendListIndex = int.Parse(XML.DocumentElement.Attributes["trendlistindex"].InnerText);
                TrendIndex = int.Parse(XML.DocumentElement.Attributes["trendindex"].InnerText);
                loaded = true;
            }
            else
            {
                TrendIndex = 0;
                TrendListIndex = 0;
            }
        }

        public void Init(string urlLocation, string zipCodes, string city, string state)
        {
            XML.DocumentElement.Attributes["urllocation"].InnerText = urlLocation;
            XML.DocumentElement.Attributes["zipcodes"].InnerText = zipCodes;
            XML.DocumentElement.Attributes["city"].InnerText = city;
            XML.DocumentElement.Attributes["state"].InnerText = state;
            XML.DocumentElement.Attributes["trendindex"].InnerText = "0";
            XML.DocumentElement.Attributes["trendlistindex"].InnerText = "0";
        }

        public void UpdateTrendIndex(int trendIndex)
        {
            XML.DocumentElement.Attributes["trendindex"].InnerText = trendIndex.ToString();
        }

        public void UpdateTrendListIndex(int trendListIndex)
        {
            XML.DocumentElement.Attributes["trendlistindex"].InnerText = trendListIndex.ToString();
        }

        public void Save()
        {
            XML.Save(PATH_FILE);
        }

        public static void Create(string urlLocation, string zipCodes, string city, string state)
        {
            if (!File.Exists(PATH_FILE))
            {
                XmlDocument xml = new XmlDocument();
                XmlDeclaration declaration = xml.CreateXmlDeclaration("1.0", null, null);
                xml.AppendChild(declaration);
                XmlElement root = xml.CreateElement("root");
                XmlAttribute xUrlLocation = xml.CreateAttribute("urllocation");
                xUrlLocation.InnerText = urlLocation;
                root.SetAttributeNode(xUrlLocation);
                XmlAttribute xZipCodes = xml.CreateAttribute("zipcodes");
                xZipCodes.InnerText = zipCodes;
                root.SetAttributeNode(xZipCodes);
                XmlAttribute xCity = xml.CreateAttribute("city");
                xCity.InnerText = city;
                root.SetAttributeNode(xCity);
                XmlAttribute xState = xml.CreateAttribute("state");
                xState.InnerText = state;
                root.SetAttributeNode(xState);
                XmlAttribute trendListIndex = xml.CreateAttribute("trendlistindex");
                trendListIndex.InnerText = "0";
                root.SetAttributeNode(trendListIndex);
                XmlAttribute trendIndex = xml.CreateAttribute("trendindex");
                trendIndex.InnerText = "0";
                root.SetAttributeNode(trendIndex);
                xml.AppendChild(root);
                xml.Save(PATH_FILE);
            }
            else
            {
                Cache cache = new Cache();
                cache.Init(urlLocation, zipCodes, city, state);
                cache.Save();
            }
        }
    }
}
