using System;
using System.Collections.Generic;
using Google.Protobuf.Protocol;

namespace Server.Object
{
    public class ObjectManager//(모든) 게임오브젝트  생성 관리 
    {
        public static ObjectManager Instance { get; private set; } = new ObjectManager();//싱글톤 패턴

        object _lock = new object();
        Dictionary<int, Player> _players = new Dictionary<int, Player>();

        //고유 아이디 규칙 !!!!!!
        //[UNUSED(1)][TYPE(7)][ID(24)]
        int _count = 0;//아이디 규칙 

        public T Add<T>() where T: GameObject, new() //게임 오브젝트의 자식 클래스만 가능 
        {
            T go = new T();
            go.Info = new ObjectInfo() { PositionInfo = new PositionInfo(), StatInfo = new StatInfo() };
            go.Info.Id = GenerateId(go.GameObjectType);//고유 아이디 부여

            //오브젝트가 플레이어라면 ? 
            GameObjectType type = GetObjTypeById(go.Info.Id);
            if(type == GameObjectType.Player)
            {
                _players.Add(go.Info.Id, go as Player);
            }

            return go;
        }
        public static GameObjectType GetObjTypeById(int objectId)
        {
            return (GameObjectType)(objectId >> 24 & 0x7F);
        }
        public int GenerateId(GameObjectType type)
        {
            lock (_lock)
            {
                return ((int)type << 24 | _count++);//1증가 
            }
        }
      
        public bool Remove(int objectId)
        {
            lock (_lock)//멀티 쓰레드 환경 
            {
                //플레이어 인가 ?
                if(GetObjTypeById(objectId) == GameObjectType.Player)
                    return _players.Remove(objectId);

                return false;//플레이어 아님 
            }
        }

        public Player Find(int objectId)
        {
            
            lock (_lock)//멀티쓰레드 환경 
            {
                //플레이어 인가 ?
                Player p = null;
                if (GetObjTypeById(objectId) == GameObjectType.Player)
                {
                    _players.TryGetValue(objectId, out p);
                    return p;
                }
                return null;
                    

            }

        }
    }
}

