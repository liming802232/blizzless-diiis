﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DiIiS_NA.Core.Logging;

namespace DiIiS_NA.REST.Http
{
    public class HttpHeader
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string Host { get; set; }
        public string DeviceId { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public string AcceptLanguage { get; set; }
        public string Accept { get; set; }
        public string UserAgent { get; set; }
        public string Content { get; set; }
    }

    public enum HttpCode
    {
        OK = 200,
        Found = 302,
        BadRequest = 400,
        NotFound = 404,
        InternalServerError = 500,
        BadGateway = 502
    }

    public class HttpHelper
    {

        public static byte[] CreateResponseAlt(HttpCode httpCode, string content, bool closeConnection = false)
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            {
                sw.WriteLine($"HTTP/1.1 {(int)httpCode} {httpCode}");
                sw.WriteLine("Connection: close"); ;
                sw.WriteLine($"Content-Length: 0\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] CreateResponse(HttpCode httpCode, string content, bool closeConnection = false, string contentType = "application/json;charset=UTF-8")
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            {
                sw.WriteLine($"HTTP/1.1 {(int)httpCode} {httpCode}");
                sw.WriteLine($"Content-Length: {content.Length}");
                if (closeConnection)
                    sw.WriteLine("Connection: close");

                sw.WriteLine("Content-Type: " + contentType);
                sw.WriteLine();

                sw.WriteLine(content);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static HttpHeader ParseRequest(byte[] data, int length)
        {
            var headerValues = new Dictionary<string, object>();
            var header = new HttpHeader();

            using (var sr = new StreamReader(new MemoryStream(data, 0, length)))
            {
                var info = sr.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (info.Length != 3)
                    return null;

                headerValues.Add("method", info[0]);
                headerValues.Add("path", info[1]);
                headerValues.Add("type", info[2]);

                while (!sr.EndOfStream)
                {
                    try
                    {
                        info = sr.ReadLine().Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);

                        if (info.Length == 2)
                            headerValues.Add(info[0].Replace("-", "").ToLower(), info[1]);
                        else if (info.Length > 2)
                        {
                            var val = "";

                            info.Skip(1);

                            headerValues.Add(info[0].Replace("-", "").ToLower(), val);
                        }
                        else
                        {
                            // We are at content here.
                            var content = sr.ReadLine();

                            headerValues.Add("content", content);

                            // There shouldn't be anything after the content!
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }
            }

            var httpFields = typeof(HttpHeader).GetTypeInfo().GetProperties();

            foreach (var f in httpFields)
            {
                object val;

                if (headerValues.TryGetValue(f.Name.ToLower(), out val))
                {
                    if (f.PropertyType == typeof(int))
                        f.SetValue(header, Convert.ChangeType(Convert.ToInt32(val), f.PropertyType));
                    else
                        f.SetValue(header, Convert.ChangeType(val, f.PropertyType));
                }
            }

            return header;
        }
    }
}
