using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Drawing;
using System.Xml;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Net;
using System.IO;

public class Core
{
    public class TSQL
    {
        public static string ConnectionStringName = "MonopolyConnectionString";
        public static SqlConnection Connection = new SqlConnection(GetConnectionString());

        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        public static string RunQuery(string query)
        {
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
            Connection.Open();
            SqlCommand command = new SqlCommand(query, Connection);
            string result = "";
            try
            {
                result = command.ExecuteScalar().ToString() as string;
            }
            catch (Exception e)
            {
                result = "";
            }
            Connection.Close();
            return result;
        }

        public static void RunCommand(string query)
        {
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
            Connection.Open();
            SqlCommand command = new SqlCommand(query, Connection);
            command.ExecuteNonQuery();
            Connection.Close();
        }

        public static DataSet CreateDataSet(string query)
        {
            SqlDataAdapter dataAdapter = new SqlDataAdapter(query, Connection);
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);
            return dataSet;
        }

        public static string CheckInsert(bool useQuotes, string value)
        {
            string returnValue = value;
            if (value == "")
            {
                returnValue = "NULL";
            }
            else
            {
                if (useQuotes)
                {
                    returnValue = "'" + value + "'";
                }
            }
            return returnValue;
        }
    }

    public class Validation
    {
        public static List<string> ValidImageExtensions = new List<string>()
        {
            "jpg",
            "jpeg",
            "png",
            "gif"
        };

        public static Regex Email = new Regex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");

        public static bool IsDecimal(string str)
        {
            try
            {
                decimal.Parse(str);
                return true;
            }
            catch { return false; }
        }

        public static bool IsInt(string str)
        {
            try
            {
                int.Parse(str);
                return true;
            }
            catch { return false; }
        }

        public static bool IsDateTime(string str)
        {
            try
            {
                DateTime.Parse(str);
                return true;
            }
            catch { return false; }
        }

        public static bool IsValidImage(string fileName)
        {
            return ValidImageExtensions.Any(x => fileName.ToLower().Contains(x));
        }

        public static string GetValidImageExtensions()
        {
            return string.Join(", ", ValidImageExtensions.ToArray());
        }
    }

    public class Encryption
    {
        private static string Password = "password";

        public static string Encrypt(string Message)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();
            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Password));
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;
            byte[] DataToEncrypt = UTF8.GetBytes(Message);
            try
            {
                ICryptoTransform Encryptor = TDESAlgorithm.CreateEncryptor();
                Results = Encryptor.TransformFinalBlock(DataToEncrypt, 0, DataToEncrypt.Length);
            }
            finally
            {
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }
            return Convert.ToBase64String(Results);
        }

        public static string Decrypt(string Message)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();
            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Password));
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;
            byte[] DataToDecrypt = Convert.FromBase64String(Message.Replace(" ", "+"));
            try
            {
                ICryptoTransform Decryptor = TDESAlgorithm.CreateDecryptor();
                Results = Decryptor.TransformFinalBlock(DataToDecrypt, 0, DataToDecrypt.Length);
            }
            finally
            {
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }
            return UTF8.GetString(Results);
        }
    }

    public class Mail
    {
        public static string GoDaddyHost = "dedrelay.secureserver.net";

        public static Email FromEmail = new Email("noreply@mtjohnston.com");

        public class Email
        {
            private string address;
            public string Address
            {
                get { return address; }
                set { address = value; }
            }

            public Email(string address)
            {
                Address = address;
            }
        }

        public class Message
        {
            private List<Email> toList;
            public List<Email> ToList
            {
                get { return toList; }
                set { toList = value; }
            }

            private string subject;
            public string Subject
            {
                get { return subject; }
                set { subject = value; }
            }

            private Email from;
            public Email From
            {
                get { return from; }
                set { from = value; }
            }

            private string body;
            public string Body
            {
                get { return body; }
                set { body = value; }
            }

            private string host;
            public string Host
            {
                get { return host; }
                set { host = value; }
            }

            public Message()
            {
                From = FromEmail;
                ToList = new List<Email>();
                Subject = "";
                Body = "";
                Host = GoDaddyHost;
            }

            public void Send()
            {
                foreach (Email email in ToList)
                {
                    if (!Validation.Email.IsMatch(email.Address))
                    {
                        throw new Exception("Email invalid: " + email.Address);
                    }
                }
                if (!Validation.Email.IsMatch(From.Address))
                {
                    throw new Exception("Email invalid: " + From.Address);
                }
                else if (Body == "")
                {
                    throw new Exception("Mail Body left blank");
                }
                else
                {
                    System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
                    foreach (Email email in ToList)
                    {
                        message.To.Add(email.Address);
                    }
                    message.Subject = Subject;
                    message.From = new System.Net.Mail.MailAddress(From.Address);
                    message.Body = Body;
                    System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(Host);
                    try
                    {
                        smtp.Send(message);
                    }
                    catch { }
                }
            }
        }
    }

    public class URL
    {
        public static string GetFileName(string url)
        {
            return url.Remove(0, url.LastIndexOf("/") + 1);
        }
    }

    public class Math
    {
        public static int GetPercent(int value, int max)
        {
            double dValue = (double)value;
            double dMax = (double)max;
            double deci = dValue / dMax;
            return (int)System.Math.Round(deci * 100);
        }

        public class Random
        {
            public static int GenerateSeed()
            {
                string seed = DateTime.Now.ToLongTimeString();
                return int.Parse(seed.Substring(0, seed.IndexOf(" ")).Replace(":", "") + DateTime.Now.Millisecond.ToString());
            }

            public static string GenerateNumber(int digits)
            {
                System.Random random = new System.Random(GenerateSeed());
                string number = "";
                for (int digit = 1; digit <= digits; digit++)
                {
                    number += random.Next(0, 9).ToString();
                }
                return number;
            }
        }
    }

    public class Xml
    {
        public static List<XmlNode> GetChildrenAsQueryable(XmlNode node)
        {
            return node.ChildNodes.OfType<XmlNode>().ToList();
        }
    }

    public class HTML
    {
        public static string GetSource(string url, WebClient webClient = null)
        {
            WebClient client = webClient == null ? new WebClient() : webClient;
            return client.DownloadString(url);
        }
    }

    public class Images
    {
        public static System.Drawing.Image GetImage(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            return System.Drawing.Image.FromStream(response.GetResponseStream());
        }

        public static string BinaryToCSSURL(byte[] binary)
        {
            return "url('data:image/gif;base64," + Convert.ToBase64String(binary) + "')";
        }

        public static string BinaryToImgSrc(byte[] binary)
        {
            return "data:image/gif;base64," + Convert.ToBase64String(binary);
        }

        public static byte[] ImageToBinary(Image image)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }

        public static Image ResizeImageHeight(Image originalFile, int newHeight)
        {
            originalFile.RotateFlip(RotateFlipType.Rotate180FlipNone);
            originalFile.RotateFlip(RotateFlipType.Rotate180FlipNone);
            int newWidth = originalFile.Width * newHeight / originalFile.Height;
            return originalFile.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero);
        }

        public static Image ResizeImageWidth(Image originalFile, int newWidth)
        {
            originalFile.RotateFlip(RotateFlipType.Rotate180FlipNone);
            originalFile.RotateFlip(RotateFlipType.Rotate180FlipNone);
            int newHeight = originalFile.Height * newWidth / originalFile.Width;
            return originalFile.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero);
        }
    }

    public class Files
    {
        public struct CSV
        {
            public List<CSVLine> Lines;
        }
        public struct CSVLine
        {
            public int LineNumber;
            public List<string> Fields;
        }

        public static byte[] FileInfoToBinary(FileInfo fileInfo)
        {
            FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileInfo.Length);
        }

        public static byte[] StreamToBinary(Stream stream)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static CSV ParseCSV(string filePath, char seperator = ',')
        {
            CSV csv = new CSV();
            csv.Lines = new List<CSVLine>();
            int lineNumber = 1;
            string line = null;
            StreamReader file = new StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                CSVLine csvLine = new CSVLine();
                csvLine.LineNumber = lineNumber;
                csvLine.Fields = line.Split(seperator).ToList();
                csv.Lines.Add(csvLine);
                lineNumber++;
            }
            file.Close();
            return csv;
        }
    }

    public class Enums
    {
        public static T Parse<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }
    }

    public class RSS
    {
        public class Feed
        {
            public decimal Version;

            private RSSChannel channel = new RSSChannel();
            public RSSChannel Channel
            {
                get { return channel; }
                set { channel = value; }
            }

            public Feed(string url)
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(url);
                Version = decimal.Parse(xml.DocumentElement.Attributes["version"].InnerText);
                XmlElement xChannel = (XmlElement)xml.GetElementsByTagName("channel")[0];
                Channel.Title = xChannel.GetElementsByTagName("title")[0].InnerText;
                Channel.Link = xChannel.GetElementsByTagName("link")[0].InnerText;
                Channel.Description = xChannel.GetElementsByTagName("description")[0].InnerText;
                channel.Language = xChannel.GetElementsByTagName("language")[0].InnerText;
                channel.Copyright = xChannel.GetElementsByTagName("copyright")[0].InnerText;
                channel.PubDate = DateTime.Parse(xChannel.GetElementsByTagName("pubDate")[0].InnerText);
                channel.TTL = int.Parse(xChannel.GetElementsByTagName("ttl")[0].InnerText);
                XmlElement xImage = (XmlElement)xChannel.GetElementsByTagName("image")[0];
                channel.Image.URL = xImage.GetElementsByTagName("url")[0].InnerText;
                channel.Image.Title = xImage.GetElementsByTagName("title")[0].InnerText;
                channel.Image.Link = xImage.GetElementsByTagName("link")[0].InnerText;
                XmlNodeList xItems = xChannel.GetElementsByTagName("item");
                foreach (XmlNode xItemNode in xItems)
                {
                    XmlElement xItem = (XmlElement)xItemNode;
                    RSSChannel.RSSItem item = new RSSChannel.RSSItem();
                    item.Title = xItem.GetElementsByTagName("title")[0].InnerText;
                    item.Link = xItem.GetElementsByTagName("link")[0].InnerText;
                    item.Description = xItem.GetElementsByTagName("description")[0].InnerText;
                    item.Author = xItem.GetElementsByTagName("author")[0].InnerText;
                    item.Category = xItem.GetElementsByTagName("category")[0].InnerText;
                    item.PubDate = DateTime.Parse(xItem.GetElementsByTagName("pubDate")[0].InnerText);
                    item.Source = xItem.GetElementsByTagName("source")[0].InnerText;
                    channel.Items.Add(item);
                }
            }

            public class RSSChannel
            {
                public string Title;
                public string Description;
                public string Language;
                public string Copyright;
                public string Link;
                public int TTL;
                public DateTime PubDate;
                public RSSImage Image;
                public List<RSSItem> Items;

                public RSSChannel()
                {
                    Image = new RSSImage();
                    Items = new List<RSSItem>();
                }

                public struct RSSImage
                {
                    public string URL;
                    public string Title;
                    public string Link;
                }

                public struct RSSItem
                {
                    public string Title;
                    public string Link;
                    public string Description;
                    public string Author;
                    public string Category;
                    public string Source;
                    public DateTime PubDate;
                }
            }
        }
    }

    public class Dates
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }
}

public static class Extensions
{
    public static string URLDecode(this string value)
    {
        return value.Replace("%3A", ":").Replace("%2F", "/").Replace("%3F", "?").Replace("%3D", "=");
    }

    public static List<T> Shuffle<T>(this List<T> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }
}