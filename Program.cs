using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace YellowPagesCrawler
{
    class Program
    {
        private static int TrendIndex;
        private static int TrendCount;
        private static int TrendListIndex;
        private static int TrendListCount;
        private static int AddedBusinessCount;
        private static bool IsContinuation;
        private static string UrlLocation;
        private static string City;
        private static string State;
        private static string Trend;
        private static string[] ZipCodes;
        private static Random TimerRandom;
        private static WebClient WebClient;
        private static List<string> TrendUrls;
        private static List<string> TrendListUrls;
        private static List<CacheBusiness> CachedBusinesses;

        static void Main(string[] args)
        {
            Init();
            GetTrendLists();
            Console.WriteLine("Processing Trends...");
            if (!IsContinuation)
            {
                ProcessTrends();
                TrendListIndex++;
            }
            ProcessTrendLists();

            Console.WriteLine("Press any key to end.");
            Console.ReadKey();
        }

        private static void Init()
        {
            TimerRandom = new Random(Core.Math.Random.GenerateSeed());
            WebClient = new System.Net.WebClient();

            Cache cache = new Cache();
            if (cache.TrendListIndex > 0 || cache.TrendIndex > 0)
            {
                Console.WriteLine("Start process from last stop? Y/N");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    UrlLocation = cache.UrlLocation;
                    ZipCodes = cache.ZipCodes.Split(',');
                    City = cache.City;
                    State = cache.State;
                    TrendListIndex = cache.TrendListIndex;
                    TrendIndex = cache.TrendIndex;
                    IsContinuation = true;
                }
                else
                {
                    GetLocationInfo();
                    TrendListIndex = 1;
                    TrendIndex = 1;
                }
            }
            else
            {
                GetLocationInfo();
                TrendListIndex = 1;
                TrendIndex = 1;
            }
            Console.WriteLine("Getting cached businesses...");
            SQLDataContext sql = new SQLDataContext();
            CachedBusinesses = (from b in sql.Businesses
                                join a in sql.Addresses on b.AddressID1 equals a.ID
                                where ZipCodes.Contains(a.Zip)
                                select new CacheBusiness
                                {
                                    CompanyName = b.CompanyName,
                                    Phone = b.Phone,
                                    Zip = a.Zip
                                }).ToList();
        }

        private static void GetLocationInfo()
        {
            Console.WriteLine("Enter YellowPages Url Location:");
            UrlLocation = Console.ReadLine();

            Console.WriteLine("Enter comma-delimited Zip Code(s):");
            string zipCodes = Console.ReadLine();
            ZipCodes = zipCodes.Split(',');

            Console.WriteLine("City:");
            City = Console.ReadLine();

            Console.WriteLine("State:");
            State = Console.ReadLine();

            Cache.Create(UrlLocation, zipCodes, City, State);
        }

        private static void GetTrendLists()
        {
            Console.WriteLine("Getting trend lists...");
            string url = string.Format("http://www.yellowpages.com/{0}/trends/1", UrlLocation);
            TrendListUrls = new List<string>();
            TrendUrls = new List<string>();

            string src = Core.HTML.GetSource(url, WebClient);
            src = src.Remove(0, src.IndexOf("<ul class=\"categories-list\">"));
            //src = src.Substring(0, src.IndexOf("<div id=\"global-footer\">"));

            string trends = src.Substring(0, src.IndexOf("</div>"));
            AddAnchorUrlsToList(trends, ref TrendUrls);
            TrendCount = TrendUrls.Count;

            src = src.Remove(0, src.IndexOf("<div class=\"page-navigation\">"));
            AddAnchorUrlsToList(src, ref TrendListUrls);
            TrendListCount = TrendListUrls.Count;
            Console.WriteLine("Got {0} trend lists.", TrendListCount);
        }

        private static void GetTrends(string url)
        {
            TrendUrls.Clear();
            RandomSleep();
            string src = Core.HTML.GetSource(url, WebClient);
            src = src.Remove(0, src.IndexOf("<ul class=\"categories-list\">"));
            string trends = src.Substring(0, src.IndexOf("</div>"));
            AddAnchorUrlsToList(trends, ref TrendUrls);
            TrendCount = TrendUrls.Count;
        }

        private static void ProcessTrends()
        {
            SQLDataContext sql = new SQLDataContext();
            foreach (string trendUrl in TrendUrls)
            {
                RandomSleep();
                Trend = trendUrl.Remove(0, trendUrl.LastIndexOf("/") + 1);
                string url = "http://www.yellowpages.com" + trendUrl;
                try { ProcessTrend(url, sql); } catch { }
                sql.SubmitChanges();
                Console.WriteLine("Processed trend {0} +{1} (T:{2}/{3}, L:{4}/{5})",
                    Trend,
                    AddedBusinessCount,
                    TrendIndex,
                    TrendCount,
                    TrendListIndex,
                    TrendListCount);
                TrendIndex++;

                Cache cache = new Cache();
                cache.UpdateTrendIndex(TrendIndex);
                cache.Save();
            }
        }

        private static void ProcessTrend(string url, SQLDataContext sql)
        {
            AddedBusinessCount = 0;
            string companyName = null,
                address = null,
                zip = null,
                phone = null;
            string src = Core.HTML.GetSource(url, WebClient);
            while (src.Contains("<div class=\"srp-business-name\">"))
            {
                src = src.Remove(0, src.IndexOf("<div class=\"srp-business-name\">"));
                src = src.Remove(0, src.IndexOf("title=\"") + 7);
                companyName = src.Substring(0, src.IndexOf("\""));

                src = src.Remove(0, src.IndexOf("<span class=\"street-address\">"));
                src = src.Remove(0, src.IndexOf(">") + 1);
                address = src.Substring(0, src.IndexOf("<"));

                src = src.Remove(0, src.IndexOf("<span class=\"postal-code\">"));
                src = src.Remove(0, src.IndexOf(">") + 1);
                zip = src.Substring(0, src.IndexOf("<"));

                src = src.Remove(0, src.IndexOf("<span class=\"business-phone phone\">"));
                src = src.Remove(0, src.IndexOf(">") + 1);
                phone = src.Substring(0, src.IndexOf("<"));

                if (GetCacheBusiness(companyName, zip, phone) == null && ZipCodes.Contains(zip))
                {
                    BusinessTemp businessTemp = new BusinessTemp();
                    businessTemp.CompanyName = companyName;
                    businessTemp.Phone = phone;
                    businessTemp.BusinessTrend = Trend;
                    businessTemp.Street = address;
                    businessTemp.City = City;
                    businessTemp.State = State;
                    businessTemp.Zip = zip;
                    sql.BusinessTemps.InsertOnSubmit(businessTemp);
                    CachedBusinesses.Add(new CacheBusiness
                    {
                        CompanyName = companyName,
                        Phone = phone,
                        Zip = zip
                    });
                    AddedBusinessCount++;
                }
            }
        }

        private static void ProcessTrendLists()
        {
            for (int i = TrendListIndex - 1; i < TrendListUrls.Count; i++)
            {
                string url = "http://www.yellowpages.com" + TrendListUrls[i];
                GetTrends(url);
                ProcessTrends();
                TrendListIndex++;

                Cache cache = new Cache();
                cache.UpdateTrendListIndex(TrendListIndex);
                cache.Save();
            }
        }

        private static void RandomSleep()
        {
            Thread.Sleep(TimerRandom.Next(30, 60) * 1000);
        }

        private static void AddAnchorUrlsToList(string src, ref List<string> list)
        {
            while (src.Contains("<a"))
            {
                src = src.Remove(0, src.IndexOf("<a"));
                src = src.Remove(0, src.IndexOf("href=\"") + 6);
                list.Add(src.Substring(0, src.IndexOf("\"")));
            }
        }

        private static CacheBusiness GetCacheBusiness(string companyName, string zip, string phone)
        {
            return CachedBusinesses.FirstOrDefault(b => b.CompanyName == companyName && b.Zip == zip && b.Phone == phone);
        }

        private class CacheBusiness
        {
            public string CompanyName;
            public string Zip;
            public string Phone;
        }
    }
}
