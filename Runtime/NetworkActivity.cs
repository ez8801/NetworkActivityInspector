using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using EZ.Network.Extensions;

namespace EZ.Network
{
    public class NetworkActivity
    {
        internal static event Action<PacketLog> webRequestRequested = null;
        internal static event Action webResponseReceived = null;

        public static bool IsLoggable()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        public static async Task OnRequest(UnityWebRequestAsyncOperation operation)
        {
            await OnRequest(operation, string.Empty);
        }

        public static async Task OnRequest(UnityWebRequestAsyncOperation operation, string arguments)
        {
            if (IsLoggable())
            {
                var webRequest = operation.webRequest;
                var packetLog = new PacketLog(webRequest, arguments);
                webRequestRequested?.Invoke(packetLog);
                await operation;
                packetLog.OnResponse(operation.webRequest, webRequest.downloadHandler.text);
                webResponseReceived?.Invoke();
            }
        }
    }
}