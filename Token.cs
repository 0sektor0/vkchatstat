using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;



namespace vkchatstat
{
    public class Token
    {
        public DateTime expired_time;
        public bool is_group = false;
        string _value;

        public string value
        {
            get
            {
                if (is_alive)
                    return _value;
                else
                    throw new Exception("Token has expired");
            }
        }
        public bool is_alive
        {
            get
            {
                if (is_group)
                    return true;
                if (DateTime.UtcNow < expired_time)
                    return true;
                else
                    return false;
            }
        }



        private Token(string token, int expires_in)
        {
            _value = token;
            expired_time = DateTime.UtcNow.AddSeconds(expires_in).AddMinutes(-10);
            is_group = expires_in <= 0;
        }


        public Token() {}


        public static Token Auth(string login, string password, int scope)
        {
            string html;
            string post_data;
            string[] res = null;
            HttpWebRequest request;
            HttpWebResponse response;
            CookieContainer cookie_container = new CookieContainer();

            //переходим на страницу авторизации
            request = (HttpWebRequest)HttpWebRequest.Create($"https://oauth.vk.com/authorize?client_id=5635484&redirect_uri=https://oauth.vk.com/blank.html&scope={scope}&response_type=token&v=5.80&display=wap");
            request.AllowAutoRedirect = false;
            request.CookieContainer = cookie_container;
            response = (HttpWebResponse)request.GetResponse();

            //считывем код страницы 
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                html = reader.ReadToEnd();
            //составляем пост данные и выдираем csrf токены
            post_data = $"email={login}&pass={password}";
            foreach (Match m in Regex.Matches(html, @"\B<input\stype=""hidden""\sname=""(.+)""\svalue=""(.+)"""))
                post_data += $"&{m.Groups[1]}={m.Groups[2]}";

            //отправляем логин и пароль
            request = (HttpWebRequest)HttpWebRequest.Create("https://login.vk.com/?act=login&soft=1");
            request.CookieContainer = cookie_container;
            request.AllowAutoRedirect = false;
            request.Method = "POST";
            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                writer.Write(post_data);
            response = GetResponse302(request);

            if (response.Cookies.Count == 0)
                throw new Exception("Invalid login or password");

            //переходим по Location в ответе
            request = (HttpWebRequest)HttpWebRequest.Create(response.Headers["Location"]);
            request.CookieContainer = cookie_container;
            request.AllowAutoRedirect = false;
            response = GetResponse302(request);

            //и еще раз
            request = (HttpWebRequest)HttpWebRequest.Create(response.Headers["Location"]);
            request.CookieContainer = cookie_container;
            request.AllowAutoRedirect = false;
            response = GetResponse302(request);

            res = response.Headers["Location"].Split('=', '&');
            return new Token(res[1], Convert.ToInt32(res[3]));
        }


        private static HttpWebResponse GetResponse302(HttpWebRequest request)
        {
            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
                return response;
            }
            catch (WebException e)
            {
                if (e.Message.Contains("302"))
                {
                    response = (HttpWebResponse)e.Response;
                    return response;
                }

                throw e;
            }
        }
    }
}
