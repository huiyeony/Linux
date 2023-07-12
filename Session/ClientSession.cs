using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Object;
using Server.Game;

namespace Server.Session
{
    public partial class ClientSession : PacketSession
    {
        public PlayerServerState ServerState { get; set; } = PlayerServerState.ServerStateLogin;//기본 상태 
        public Player MyPlayer { get; private set; }//담당하는 플레이어 정보 
        public int SessionId { get; set; }

        public void Send(IMessage packet)
        {
            string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
            MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
            ushort size = (ushort)packet.CalculateSize();
            byte[] sendBuffer = new byte[size + 4];
            Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
            Send(new ArraySegment<byte>(sendBuffer));
        }
        long _pingpongTick = 0;//클라쪽 답장이 온 시간 기록함 ! 
        public void Ping()
        {
            if(_pingpongTick > 0)
            {
                long delta = (System.Environment.TickCount64 - _pingpongTick);
                if(delta > 60 * 1000)//30초 이상 일 때 
                {
                    Console.WriteLine("Disconnected by pingpong");
                    Disconnect();
                    return;
                }

            }

            SPing pingPacket = new SPing();
            Send(pingPacket);

            //게임룸 로직에 끼워넣기 
            GameRoom room = RoomManager.Instance.Find(1);
            if(room != null)
                room.PushAfter(5000, Ping);//5초에 한번씩 계속 보내기

        }
        public void HandlePong()
        {
            _pingpongTick = System.Environment.TickCount64; //답장이 온 시간 기록함 !
        }
        public override void OnConnected(EndPoint endPoint)
        {
            //Console.WriteLine($"OnConnected : {endPoint}");

            
            SConnected connectedPacket = new SConnected();
            Send(connectedPacket); //연결 확인 패킷 보냄

            //게임룸 로직에 끼워넣기 
            GameRoom room = RoomManager.Instance.Find(1);
            if (room != null)
                room.Push(Ping);


        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            GameRoom room = RoomManager.Instance.Find(1);
            room.LeaveGame(MyPlayer.Info.Id);//즉시 실행 

            SessionManager.Instance.Remove(this);
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
