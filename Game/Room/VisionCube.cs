using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Protocol;
using Server.Object;

namespace Server.Game
{
    public class VisionCube
    {
        public Player Owner { get; private set; }

        public HashSet<GameObject> _objects = new HashSet<GameObject>();

        public VisionCube(Player owner)
        {
            Owner = owner;//주인님 
        }
        public void Clear()
        {
            //오브젝트 목록들을 클리어 !
            _objects.Clear();
        }
        public void Update()
        {
            //예전에 없었는데 새롭게 생긴 오브젝트 spawn!
            GameRoom room = Owner.Room;
            
            HashSet<GameObject> newObjects = GatherObjects();
            if (newObjects == null) return;
            
            List<GameObject> added = newObjects.Except(_objects).ToList();
            if(added.Count > 0)
            {
                SSpawn spawnPacket = new SSpawn();
                foreach (GameObject go in added)
                {
                    //패킷 보내기 
                    ObjectInfo info = new ObjectInfo();
                    info.MergeFrom(go.Info);
                    spawnPacket.Infos.Add(info);
                }

                Owner.Session.Send(spawnPacket);
            }
            

            //예전에 있었는데 새롭게 생긴 오브젝트 despawn!
            List<GameObject> removed = _objects.Except(newObjects).ToList();
            if(removed.Count > 0)
            {
                SDespawn despawnPacket = new SDespawn();

                foreach (GameObject go in removed)
                {
                    //패킷 보내기 
                    ObjectInfo info = new ObjectInfo();
                    info.MergeFrom(go.Info);
                    despawnPacket.Infos.Add(info);

                }

                Owner.Session.Send(despawnPacket);
            }

            _objects = newObjects;//오브젝트목록 업뎃
            room.PushAfter(100, Update);
        }
        public HashSet<GameObject> GatherObjects()
        {
            if (Owner == null || Owner.Room == null)
                return null;

            HashSet<GameObject> objects = new HashSet<GameObject>(); 
            GameRoom room = Owner.Room;

            //시야각에 있는 오브젝트를 담아보자!
            Vector2Int cellPos = Owner.CellPos;
            List<Zone> zones = room.GetAdjacentZones(cellPos);
            
            foreach(Zone zone in zones)
            {
                foreach(Player p in zone.Players)
                {
                    //difference 계산
                    int dx = p.CellPos.x - cellPos.x;
                    int dy = p.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.ViewCells) continue;
                    if (Math.Abs(dy) > GameRoom.ViewCells) continue;

                    objects.Add(p);

                }
                foreach(Monster m in zone.Monsters)
                {
                    //difference 계산
                    int dx = m.CellPos.x - cellPos.x;
                    int dy = m.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.ViewCells) continue;
                    if (Math.Abs(dy) > GameRoom.ViewCells) continue;

                    objects.Add(m);
                }
                foreach (Projectile p in zone.Projectiles)
                {
                    //difference 계산
                    int dx = p.CellPos.x - cellPos.x;
                    int dy = p.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.ViewCells) continue;
                    if (Math.Abs(dy) > GameRoom.ViewCells) continue;

                    objects.Add(p);
                }
                foreach (Resource r in zone.Resources)
                {
                    //difference 계산
                    int dx = r.CellPos.x - cellPos.x;
                    int dy = r.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.ViewCells) continue;
                    if (Math.Abs(dy) > GameRoom.ViewCells) continue;

                    objects.Add(r);
                }
            }
            return objects;
        }
    }
}

