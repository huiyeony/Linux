using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Object;
using Server.Util;
using ServerCore;
using static Server.DB.DataModel;
namespace Server.Session
{
    public partial class ClientSession : PacketSession
    {
        //AccountDbId 메모리에 저장
        public int AccountDbId { get; private set; }
        //보유 캐릭터 목록을 메모리에서 들고 있음 
        public List<LobbyPlayerInfo> LobbyPlayers { get; set; } =  new List<LobbyPlayerInfo>();
        
        public void HandleLogin(CLogin loginPacket)
       {
            //로그인 상태인지 확인
            if (ServerState != PlayerServerState.ServerStateLogin)
                return;

            //데이터베이스 에서 계정 정보 가져오기
            using (AppDbContext db = new AppDbContext())
            {
                try
                {
                   
                    AccountDb findAccount = db.Accounts
                        .Include(a => a.PlayerDbs)
                        .Where(a => a.AccountDbName == loginPacket.UniqueId).FirstOrDefault(); //없으면 null 반환
                    if (findAccount != null)//아하 이미 등록된 기기 !
                    {

                        AccountDbId = findAccount.AccountDbId; //AccountDbId 저장

                        LobbyPlayers.Clear();//혹시 중복 될까 

                        SLogin loginOkPacket = new SLogin() { LoginOk = 1 };
                        //db정보로 로비 플레이어 정보 갱신  
                        foreach (PlayerDb playerDb in findAccount.PlayerDbs) 
                        {
                            LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
                            {
                                PlayerDbId = playerDb.PlayerDbId,
                                Name = playerDb.PlayerDbName,
                                StatInfo = new StatInfo()
                                {
                                    Level = playerDb.Level,
                                    Hp = playerDb.Hp,
                                    MaxHp = playerDb.MaxHp,
                                    Attack = playerDb.Attack,
                                    Speed = playerDb.Speed,
                                    Exp = playerDb.Exp,
                                    Mental = playerDb.Mental

                                }
                            };


                            LobbyPlayers.Add(lobbyPlayer);//메모리에도 들고 있는다

                            loginOkPacket.Players.Add(lobbyPlayer);//패킷에 추가한다
                        }
                        Send(loginOkPacket);

                        //로비로 이동 
                        ServerState = PlayerServerState.ServerStateLobby;
                    }
                    else//등록되지 않은 기기 !
                    {
                        AccountDb newAccount = new AccountDb() { AccountDbName = loginPacket.UniqueId };//새로운 계정 생성 
                        db.Accounts.Add(newAccount);
                        db.SaveChangesEx();//db에 저장

                        //AccountDbId 메모리에 저장
                        AccountDbId = newAccount.AccountDbId;

                        SLogin okPacket = new SLogin() { LoginOk = 1 };//로그인 수락 패킷 보냄
                        Send(okPacket);

                        //로비로 이동 
                        ServerState = PlayerServerState.ServerStateLobby;

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                


            }
       }

        
        public void HandleCreatePlayer(CCreatePlayer createPacket)
        {
            if (ServerState != PlayerServerState.ServerStateLobby)//로비에서만 플레이어 선택함 
                return;

            //이런 이름의 캐릭터 만들어줘 !
            //중복 이름 체크

            using(AppDbContext db = new AppDbContext())
            {
                Stat stat = null;
                DataManager.StatDict.TryGetValue(1, out stat);
                if (stat == null) return;

                //클라에서 요청한 이름 
                PlayerDb findPlayer = db.Players
                    .Where(p => p.PlayerDbName == createPacket.Name)
                    .FirstOrDefault();

                if(findPlayer != null)//이름이 중복 됨 
                {
                    Send(new SCreatePlayer());//빈 플레이어 보냄 
                }
                else
                {
                    //데이터베이스에 저장
                    PlayerDb newPlayer = new PlayerDb()
                    {
                        PlayerDbName = createPacket.Name,//요청한 이름 
                        AccountDbId = AccountDbId,//메모리에서 저장

                        Level = stat.level,
                        Hp = stat.maxHp,
                        MaxHp = stat.maxHp,
                        Attack = stat.attack,
                        Speed = stat.speed,
                        Exp = 0,
                        MaxExp = stat.maxExp,
                        Mental = stat.mental,// !max 정신력으로 시작함 
                        Days = 0,
                        Time = 0//인게임 속 시간 
                    };

                    db.Players.Add(newPlayer);
                    db.SaveChangesEx();




                    //메모리에 저장
                    LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
                    {
                        PlayerDbId = newPlayer.PlayerDbId,//데이터 베이스 아이디 전송 
                        Name = createPacket.Name,
                        StatInfo = new StatInfo()
                        {
                            Level = stat.level,
                            Hp = stat.maxHp,
                            MaxHp = stat.maxHp,
                            Attack = stat.attack,
                            Speed = stat.speed,
                            Exp = 0,
                            MaxExp = stat.maxExp,
                            Mental = stat.mental
                        }
                    };
                    LobbyPlayers.Add(lobbyPlayer);

                    //클라에 패킷 보내기
                    SCreatePlayer createOkPacket = new SCreatePlayer() { Player = new LobbyPlayerInfo() };
                    createOkPacket.Player.MergeFrom(lobbyPlayer);

                    Send(createOkPacket);
                }

            }


        }
        public void HandleEnterGame(CEnterGame enterPacket)
        {
            //클라) 이 캐릭터 이름으로 들여보내줘 !
            //이 캐릭터를 정말 가지고 있는지 검증

            if (ServerState != PlayerServerState.ServerStateLobby)
                return;

            LobbyPlayerInfo lobbyPlayer = LobbyPlayers.Find(p => p.Name == enterPacket.Name);

            if (lobbyPlayer == null)
                return;

            //playerDb <-> lobbyPlayer <-> Player <-> MyPlayerController 
            //이제 입장 시켜줌
            MyPlayer = ObjectManager.Instance.Add<Player>();

            //myPlayer 정보 복붙 
            {
                //Db Id 저장 
                MyPlayer.PlayerDbId = lobbyPlayer.PlayerDbId;
                //아이디는 플레이어 매니저에서 관리 
                MyPlayer.Info.Name = $"player_{MyPlayer.Info.Id}";

                MyPlayer.Info.PositionInfo.State = CreatureState.Idle;
                MyPlayer.Info.PositionInfo.MoveDir = MoveDir.Down;
                MyPlayer.Info.PositionInfo.PosX = -5;
                MyPlayer.Info.PositionInfo.PosY = 0;

                MyPlayer.Info.StatInfo.MergeFrom(lobbyPlayer.StatInfo);


                
                SItemList itemListPacket = new SItemList();
                

                using (AppDbContext db = new AppDbContext())
                {
                    PlayerDb playerDb = db.Players.Where(p => p.PlayerDbId == lobbyPlayer.PlayerDbId)
                        .FirstOrDefault();
                    //임시로 아이템 몇개 넣어보자 
                    {
                        ItemDb itemDb = new ItemDb()
                        {
                            TemplateId = 1,
                            Count = 1,
                            Owner = playerDb,
                            Slot = 0
                        };
                        ItemDb itemDb2 = new ItemDb()
                        {
                            TemplateId = 10,
                            Count = 1,
                            Owner = playerDb,
                            Slot = 1
                        };
                        ItemDb itemDb3 = new ItemDb()
                        {
                            TemplateId = 11,
                            Count = 1,
                            Owner = playerDb,
                            Slot = 2
                        };
                        db.Items.Add(itemDb);
                        db.Items.Add(itemDb2);
                        db.Items.Add(itemDb3);

                        db.SaveChangesEx();

                    }
                    

                    List<ItemDb> items = db.Items.Where(i => i.OwnerDbId == lobbyPlayer.PlayerDbId)
                                                .ToList();

                    //db의 아이템 -> ( 서버 /클라 ) 인벤토리에 백업 !!!!! 
                    foreach (ItemDb itemDb in items)
                    {

                        // 서버쪽에 백업 
                        Item myItem = Item.MakeItem(itemDb);
                        MyPlayer.Inven.Add(myItem);

                        //클라쪽에 백업 
                        ItemInfo itemDbInfo = new ItemInfo()
                        {

                            TemplateId = myItem.templateId,
                            ItemDbId = myItem.itemDbId,
                            Count = myItem.count,
                            Slot = myItem.slot

                        };
                        itemListPacket.ItemInfos.Add(itemDbInfo);
                    }



                }
                //클라쪽에 보낸다 
                Send(itemListPacket);
            }
            
            ServerState = PlayerServerState.ServerStateGame;//인 게임 상태 
            MyPlayer.Session = this;
            //입장 
            GameRoom room = RoomManager.Instance.Find(1);
            room.Push(room.EnterGame, MyPlayer, true);//지정 스폰 


        }
    }
}

