using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace CommonMethods
{
    public class Common
    {
        #region Public-Methods

        public static string SerializeJson(object obj)
        {
            if (obj == null) return null;

            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = Int32.MaxValue;
            return ser.Serialize(obj);

            /*
            string json = JsonConvert.SerializeObject(
                obj,
                Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });
            
            return json;
            */
        }

        public static T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                ser.MaxJsonLength = Int32.MaxValue;
                return ser.Deserialize<T>(json);

                // return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine("");
                Console.WriteLine("Exception while deserializing:");
                Console.WriteLine(json);
                Console.WriteLine("");
                throw e;
            }
        }

        public static T DeserializeJson<T>(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            return DeserializeJson<T>(Encoding.UTF8.GetString(data));
        }

        public static void PrintException(string module, string method, Exception e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            var st = new StackTrace(e, true);
            var frame = st.GetFrame(0);
            int fileLine = frame.GetFileLineNumber();
            string filename = frame.GetFileName();

            string message =
                Environment.NewLine +
                "---" + Environment.NewLine +
                "An exception was encountered which triggered this message" + Environment.NewLine +
                "  Module     : " + module + Environment.NewLine +
                "  Method     : " + method + Environment.NewLine +
                "  Type       : " + e.GetType().ToString() + Environment.NewLine +
                "  Data       : " + e.Data + Environment.NewLine +
                "  Inner      : " + e.InnerException + Environment.NewLine +
                "  Message    : " + e.Message + Environment.NewLine +
                "  Source     : " + e.Source + Environment.NewLine +
                "  StackTrace : " + e.StackTrace + Environment.NewLine +
                "  Stack      : " + StackToString() + Environment.NewLine +
                "  Line       : " + fileLine + Environment.NewLine +
                "  File       : " + filename + Environment.NewLine +
                "  ToString   : " + e.ToString() + Environment.NewLine +
                "  Servername : " + Dns.GetHostName() + Environment.NewLine +
                "---";

            Console.WriteLine(message);
        }

        public static string StackToString()
        {
            string ret = "";

            StackTrace t = new StackTrace();
            for (int i = 0; i < t.FrameCount; i++)
            {
                if (i == 0)
                {
                    ret += t.GetFrame(i).GetMethod().Name;
                }
                else
                {
                    ret += " <= " + t.GetFrame(i).GetMethod().Name;
                }
            }

            return ret;
        }

        public static byte[] StreamToBytes(Stream input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.CanRead) throw new InvalidOperationException("Input stream is not readable");

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        public static double TotalMsFrom(DateTime start)
        {
            DateTime end = DateTime.Now.ToUniversalTime();
            TimeSpan total = (end - start);
            return total.TotalMilliseconds;
        }

        public static string CalculateMd5(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(data);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }

        public static string CalculateMd5(string data)
        {
            if (String.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            return CalculateMd5(Encoding.UTF8.GetBytes(data));
        }

        public static Dictionary<string, string> AddToDict(string key, string val, Dictionary<string, string> existing)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            if (existing == null)
            {
                ret.Add(key, val);
                return ret;
            }
            else
            {
                if (existing.ContainsKey(key))
                {
                    string tempVal = existing[key];
                    tempVal += "," + val;
                    existing.Remove(key);
                    existing.Add(key, tempVal);
                    return existing;
                }
                else
                {
                    existing.Add(key, val);
                    return existing;
                }
            }
        }

        public static Dictionary<string, object> FileToDictionary(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(filename);
            string fileContents = File.ReadAllText(filename);
            Dictionary<string, object> ret = DeserializeJson<Dictionary<string, object>>(fileContents);
            return ret;
        }
        
        #endregion

    }
}
