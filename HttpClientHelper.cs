using System.Net;
using System.Text;

namespace Tron.Exchange {
    public static class HttpClientHelper {
        public static string Get(string url, int timeout = 12000) {
            var resp = Get((HttpWebRequest)WebRequest.Create(url), timeout);
            using (var s = resp.GetResponseStream()) {
                using (var sr = new StreamReader(s)) {
                    return sr.ReadToEnd();
                }
            }
        }

        private static HttpWebResponse Get(HttpWebRequest req, int timeout = 12000) {
            req.Method = "GET";
            req.ContentType = "application/json";
            req.Timeout = timeout;
            req.Accept = "application/json";
            req.Headers.Add("TRON-PRO-API-KEY", "bc7fee82-a7a3-449c-957f-7dd7e6475bf0");
            return (HttpWebResponse)req.GetResponse();
        }

        public static string Post(string url, string requestBody, Encoding encoding, int timeout = 12000) {
            var resp = Post((HttpWebRequest)WebRequest.Create(url), requestBody, encoding, timeout);

            using (var s = resp.GetResponseStream())
            using (var sr = new StreamReader(s)) {
                return sr.ReadToEnd();
            }
        }

        private static HttpWebResponse Post(HttpWebRequest req, string requestBody, Encoding encoding, int timeout = 12000) {
            var bs = encoding.GetBytes(requestBody);

            req.Method = "POST";
            req.ContentType = "application/json";
            req.ContentLength = bs.Length;
            req.Timeout = timeout;
            req.Accept = "application/json";
            req.Headers.Add("TRON-PRO-API-KEY", "bc7fee82-a7a3-449c-957f-7dd7e6475bf0");
            using (var s = req.GetRequestStream()) {
                s.Write(bs, 0, bs.Length);
            }

            return (HttpWebResponse)req.GetResponse();
        }
    }
}
