using System;
using System.Collections.Generic;

namespace Server.Game
{
    public class RoomManager
    {

        public static RoomManager Instance { get; private set; } = new RoomManager();//싱글 톤 패턴

        object _lock = new object();
        Dictionary<int, GameRoom> _rooms = new Dictionary<int, GameRoom>();//게임 룸들을 저장 하자 
        int _roomId = 1;


        public GameRoom Add()
        {
            GameRoom room = new GameRoom();
            room.Init(1, 40);//맵 끼우기 

            lock (_lock)//멀티쓰레드 환경 
            {

                room.RoomId = _roomId;
                _rooms.Add(_roomId, room);//딕셔너리에 저장
                _roomId++;

                return room;
            }

            

        }
        public bool Remove(int roomId)
        {
            lock (_lock)//멀티 쓰레드 환경 
            {
                return _rooms.Remove(roomId);
            }
        }

        public GameRoom Find(int roomId)
        {
            GameRoom room = null;
            lock (_lock)//멀티쓰레드 환경 
            {
                if (_rooms.TryGetValue(roomId, out room))//
                    return room;

                return null;//잘못된 접근 
            }

            

        }
    }
}

