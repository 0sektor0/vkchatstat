using System;
using System.IO;
using System.Net;
using System.Web;
using System.Text;
using vkchatstat.Types;
using Newtonsoft.Json.Linq;



namespace vkchatstat
{
    public class RequestSender
    {
        Token token;
        string api_version = "5.80";
        PaceController pcontroller;


        public RequestSender(Token token, int max_requests)
        {
            this.token = token;
            pcontroller = new PaceController(max_requests, 3000);
        }


        //TODO: fix pacecontroller
        public Result<T> Send<T>(ApiRequest request)
        {
            string data = "";

            request.prms["access_token"] = token.value;
            request.prms["v"] = api_version;

            foreach (string key in request.prms.Keys)
                data += $"{key}={request.prms[key]}&";

            //pcontroller.Sieze();
            return SendPost<T>(request.method, data.Remove(data.Length - 1, 1));
        }


        private Result<T> SendPost<T>(string method, string data)
        {
            string json;
            HttpWebRequest api_request = (HttpWebRequest)HttpWebRequest.Create($"https://api.vk.com/method/{method}");

            api_request.Method = "POST";
            using (Stream writer = api_request.GetRequestStream())
            {
                byte[] post_data = Encoding.UTF8.GetBytes(data);
                writer.Write(post_data, 0, post_data.Length);
            }

            HttpWebResponse apiRespose = (HttpWebResponse)api_request.GetResponse();
            using (StreamReader resp_stream = new StreamReader(apiRespose.GetResponseStream()))
                json = resp_stream.ReadToEnd();

            //Console.WriteLine(json);
            api_request.Abort();

            return Result<T>.FromJson(json);
        }


        private Result<T> SendGet<T>(string method, string data)
        {
            string json;
            HttpWebRequest api_request = (HttpWebRequest)HttpWebRequest.Create($"https://api.vk.com/method/{method}?{HttpUtility.UrlEncode(data)}");

            HttpWebResponse apiRespose = (HttpWebResponse)api_request.GetResponse();
            using (StreamReader resp_stream = new StreamReader(apiRespose.GetResponseStream()))
                json = resp_stream.ReadToEnd();

            api_request.Abort();
            return Result<T>.FromJson(json);            
        }
    }
}