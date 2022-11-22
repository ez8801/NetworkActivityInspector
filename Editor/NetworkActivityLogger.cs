using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace EZ.Network.Editor
{
    class NetworkActivityLogger
    {
        static List<PacketLog> _logs = new List<PacketLog>();

        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            NetworkActivity.webRequestRequested += OnRequested;
        }

        public static List<PacketLog> Filter(Protocol protocolFlags)
        {
            return _logs.Where(x => (x.ProtocolFlags & protocolFlags) != 0).ToList();
        }

        static void OnRequested(PacketLog packetLog)
        {
            _logs.Add(packetLog);
        }

        public static void Clear()
        {
            _logs.Clear();
        }
    }
}