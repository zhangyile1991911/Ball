using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using BallNet;
using System;
using System.IO;

namespace BallSrv
{
    public class BallServer : INetEventListener
    {
        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("OnPeerConnected");
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine("OnNetworkError");
            EventDispatch.Dispatch<SocketError>((int)EventID.OnUserConnect, socketError);
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("OnPeerDisconnected");
            EventDispatch.Dispatch<NetPeer>((int)EventID.OnUserDisconnect, peer);
        }

        public static void DebugData(string prefix,byte[] d)
        {
            StringWriter writer = new StringWriter();
            for(int  i = 0;i < d.Length;i++)
            {
                writer.Write(d[i] + ",");
            }
            Console.WriteLine(prefix+" DebugData = " +writer.GetStringBuilder().ToString());
        }

        public static string DebugDataStr(byte[] d)
        {
            StringWriter writer = new StringWriter();
            for (int i = 0; i < d.Length; i++)
            {
                writer.Write(d[i] + ",");
            }
            return writer.GetStringBuilder().ToString();
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            //Console.WriteLine("OnNetworkReceive");
            byte[] received_datas = reader.GetBytesWithLength();
            //DebugData("OnNetworkReceive", received_datas);
            PacketBase pb = PacketBase.Parser.ParseFrom(received_datas);
            MsgDispatch.Dispatch<PacketBase,NetPeer>((int)pb.EventId, pb,peer);
        }


        public void OnConnectionRequest(ConnectionRequest request)
        {
            Console.WriteLine("OnConnectionRequest");
            NetPeer acceptedPeer = request.Accept();
            EventDispatch.Dispatch<NetPeer>((int)EventID.OnUserConnect, acceptedPeer);
            Console.WriteLine("[Server] ConnectionRequest. Ep: {0}, Accepted: {1}",
                    request.RemoteEndPoint,
                    acceptedPeer != null);
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            Console.WriteLine("OnNetworkLatencyUpdate");
        }

        
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            
        }

        
    }
}
