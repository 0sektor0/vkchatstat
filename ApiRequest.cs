using System;
using System.Collections.Generic;



namespace vkchatstat
{
    public class ApiRequest
    {
        public string method;
        public Dictionary<string, string> prms;



        public ApiRequest(string method, Dictionary<string, string> prms)
        {
            this.method = method;
            this.prms = new Dictionary<string, string>(prms);
        }


        public ApiRequest(string request)
        {
            if(request.Length == 0)
                throw new InvlidVkApiRequestException("request cannot be empty");

            if(request.Contains('/')) 
                throw new InvlidVkApiRequestException("request cannot contain / symbol");

            string[] arguments = request.Split('?');
            prms = new Dictionary<string, string>();
            method = arguments[0];

            switch(arguments.Length)
            {
                case 1:
                    return;

                case 2:
                    prms = ParseRequestParams(arguments[1]);
                    break;

                default:
                    throw new InvlidVkApiRequestException("request cannot contain few ? symbols");
            }
        }


        private Dictionary<string, string> ParseRequestParams(string params_string)
        {
            Dictionary<string, string> parsed_params = new Dictionary<string, string>();  
            string[] prms = params_string.Split('&');

            foreach(string prm in prms)
            {
                string[] pair = prm.Split('=');
                switch(pair.Length)
                {
                    case 2:
                        parsed_params[pair[0]] = pair[1];
                        break;

                    default:
                        throw new InvlidVkApiRequestException($"invalid param value pair {prm}");
                }
            } 

            return parsed_params;
        }
    }


    public class InvlidVkApiRequestException : Exception
    {
        public InvlidVkApiRequestException()
        {
        }

        public InvlidVkApiRequestException(string message) : base(message)
        {
        }

        public InvlidVkApiRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}