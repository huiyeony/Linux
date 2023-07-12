using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.DB;
using Server.Game;
using Server.Object;
using Server.Session;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PacketHandler
{


    public static void CLoginHandler(PacketSession session, IMessage packet)
    {
       
        CLogin loginPacket = packet as CLogin;
        ClientSession clientSession = session as ClientSession;

        //db에 연결
        clientSession.HandleLogin(loginPacket);


    }
    
    public static void CCreatePlayerHandler(PacketSession session, IMessage packet)
    {

        CCreatePlayer createPacket = packet as CCreatePlayer;
        ClientSession clientSession = session as ClientSession;

        clientSession.HandleCreatePlayer(createPacket);
    }
    
    public static void CEnterGameHandler(PacketSession session, IMessage packet)
    {
        CEnterGame enterPacket = packet as CEnterGame;
        ClientSession clientSession = session as ClientSession;

        clientSession.HandleEnterGame(enterPacket);

    }
    public static void CEquipItemHandler(PacketSession session,IMessage packet)
    {
        //(아이템)을 (착용/미착용) 하겠음
        CEquipItem equipPacket = packet as CEquipItem;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;
        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleEquipItem, player, equipPacket);
    }

    public static void CMoveHandler(PacketSession session, IMessage packet)
    {
        //클라에서 좌표 이동을 요청
        CMove movePacket = packet as CMove;
        ClientSession clientSession = session as ClientSession;

        //null 체크  
        Player myPlayer = clientSession.MyPlayer;
        if (myPlayer == null)
            return;

        GameRoom room = myPlayer.Room;
        if (room == null)
            return;

        //락 걸기 
        room.Push(room.HandleMove, myPlayer, movePacket);


    }
    public static void CSkillHandler(PacketSession session, IMessage packet)
    {
        CSkill skillPacket = packet as CSkill;
        ClientSession clientSession = session as ClientSession;

        //null 체크  
        Player myPlayer = clientSession.MyPlayer;
        if (myPlayer == null)
            return;

        GameRoom room = myPlayer.Room;
        if (room == null)
            return;

        room.Push(room.HandleSkill, myPlayer, skillPacket);
    }

    public static void CPongHandler(PacketSession session,IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        clientSession.HandlePong();
    }
}
