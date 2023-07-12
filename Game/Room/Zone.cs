using System;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using Server.Object;

namespace Server.Game
{
    public class Zone
    {
        public int IndexY { get; private set; }
        public int IndexX { get; private set; }

        //게임룸 정보 쪼개서! 들고 있기 
        public HashSet<Player> Players = new HashSet<Player>();
        public HashSet<Monster> Monsters = new HashSet<Monster>();
        public HashSet<Projectile> Projectiles = new HashSet<Projectile>();
        public HashSet<Resource> Resources = new HashSet<Resource>();

        public Zone(int dy, int dx)
        {
            //몇번째 존인지 기억 
            IndexY = dy;
            IndexX = dx;
        }

        public Player FindOnePlayer(Func<Player,bool> condition)
        {
            foreach(Player p in Players)
            {
                if (condition.Invoke(p))
                    return p;
            }
            return null;
        }
        public List<Player> FindOnePlayers(Func<Player, bool> condition)
        {
            List<Player> players = new List<Player>();
            foreach (Player p in Players)
            {
                if (condition.Invoke(p))
                    players.Add(p);
            }
            return players;
        }
        public void Remove(GameObject go)
        {
            //존 목록에서 삭제 
            GameObjectType type = ObjectManager.GetObjTypeById(go.Info.Id);
            switch (type)
            {
                case GameObjectType.Player:
                    Players.Remove((Player)go);
                    break;
                case GameObjectType.Monster:
                    Monsters.Remove((Monster)go);
                    break;
                case GameObjectType.Projectile:
                    Projectiles.Remove((Projectile)go);
                    break;
                case GameObjectType.Resource:
                    Resources.Remove((Resource)go);
                    break;
            }
        }
    }
}

