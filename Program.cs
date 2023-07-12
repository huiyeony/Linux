using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Object;
using Server.Session;
using Server.Util;
using ServerCore;

namespace Server
{
    class Program
    {
        static Listener _listener = new Listener();

        static void ExecuteTickRoom(int tickAfter, GameRoom room)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = tickAfter;
            timer.Elapsed += ((s,e) => { room.Update(); });
            timer.AutoReset = true;
            timer.Enabled = true;
        }
        static void Main(string[] args)
        {
            
            //json파일 로드
            ConfigManager.LoadJson();
            DataManager.LoadData();

            var d = DataManager.SkillDict;
            //Db에 연결
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    Console.WriteLine("Db Initialized");
                    
                    //db에 저장 
                    db.SaveChangesEx();

                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
            GameRoom room = RoomManager.Instance.Add();//첫번째 게임 룸 생성
            ExecuteTickRoom(1, room);

            // DNS (Domain Name System)
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 8888);

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");

            //지정된 위치에 몬스터 생성

            //for (int i = 0; i < 10 ; i++)
            {


                Monster monster = ObjectManager.Instance.Add<Monster>();
                monster.Init(1);//템플릿 아이디 1 인 몬스터가 되어라

                room.Push(room.EnterGame, monster, true);//랜덤 위치 스폰 
            }

            while (true)
            {

                DbTransaction.Instance.Flush();

                Thread.Sleep(100);

            }
        }
    }
}
