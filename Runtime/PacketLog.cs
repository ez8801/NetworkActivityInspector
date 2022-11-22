using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using EZ.Json.Extensions;

namespace EZ.Network
{  
    internal class PacketLog
    {
        private static long autoincrement = 0;

        public string Url;
        public string Method;
        public Dictionary<string, string> RequestHeaders;
        public Dictionary<string, string> ResponseHeaders;
        public string RequestPayload;
        public string Response;
        public long StatusCode;
        public UnityWebRequest.Result Result;
        
        public string Error;
        public DateTime Date;
        public Protocol ProtocolFlags;
        public long Id;

        public bool IsHttpError => Result == UnityWebRequest.Result.ProtocolError;
        public bool IsNetworkError => Result == UnityWebRequest.Result.ConnectionError;

        public PacketLog()
        {
            Url = string.Empty;
            Method = string.Empty;
            RequestHeaders = new Dictionary<string, string>();
            ResponseHeaders = new Dictionary<string, string>();
            RequestPayload = string.Empty;
            Response = string.Empty;
            StatusCode = 0;
            Result = UnityWebRequest.Result.Success;
            Error = string.Empty;
            Date = DateTime.Now;
            ProtocolFlags = Protocol.Http;
        }

        public PacketLog(UnityWebRequest request, string arguments)
            : this()
        {
            Url = request.url;
            Method = request.method;
            RequestPayload = arguments;
            ProtocolFlags = Protocol.Http;
            RequestHeaders.Add("Content-Type", request.GetRequestHeader("Content-Type"));
            Id = ++autoincrement;
        }

        internal void OnResponse(UnityWebRequest request, string receivedText)
        {
            StatusCode = request.responseCode;
            Error = request.error;
            Response = receivedText;

            var headers = request.GetResponseHeaders();
            if (headers != null)
                ResponseHeaders = new Dictionary<string, string>(headers);
            else
                ResponseHeaders = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Url: {Url}");
            builder.AppendLine($"Status Code: {StatusCode}");
            builder.AppendLine($"Result: {Result}");
            builder.AppendLine($"Http Error: {IsHttpError}");
            builder.AppendLine($"Network Error: {IsNetworkError}");
            builder.AppendLine($"Error: {Error}");
            builder.AppendLine($"Date: {ResponseHeaders.GetValueOrDefault("Date", Date.ToString())}");
            builder.AppendLine();

            builder.AppendLine("Request Payload");
            builder.AppendLine(RequestPayload.ToPrettyJSONify());
            builder.AppendLine();

            builder.AppendLine("Response");
            builder.Append(Response.ToPrettyJSONify());
            return builder.ToString();
        }
    }
}