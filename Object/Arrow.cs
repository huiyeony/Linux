using System;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Object
{
    public class Arrow : Projectile 
    {
        public GameObject Owner { get; set; }//주인님 
        public Arrow()
        {
            GameObjectType = GameObjectType.Projectile;//발사체
            
        }

        long _nextTick = 0;

        public override void Update()
        {
            //발사체 위치
            //tick 관리
            if (Skill == null || Skill.projectileInfo == null || Owner == null || Room == null)
                return;


            if (_nextTick >= Environment.TickCount64)
                return;

            //json파일에서 가져오기
            long tick = (long)(1000 / Skill.projectileInfo.speed);
            _nextTick = Environment.TickCount64 + tick ;//다음 업데이트할 시간 
            
            Vector2Int destPos = GetFrontCellPos(Info.PositionInfo.MoveDir);

            //갈수 있는 곳이면 이동하고
            //갈수 없는 곳이면 충돌처리 & 소멸

            //todo :
            // 화살과 gameitem이 충돌처리되서 사라짐Map CanGo == false , go.
            //플레이어가 맵 정보에서 사라지지 않음 (Map CanGo == false xxxxApplyLeave호출xxxx ) 

            if(Room.Map.CanGo(destPos))
            {
                //
                Room.Map.ApplyMove(this, destPos, applyCollision: false);

                //패킷 보내기
                SMove movePacket = new SMove() { PositionInfo = new PositionInfo() };
                movePacket.Id = Info.Id;
                movePacket.PositionInfo.MergeFrom(Info.PositionInfo);

                GameRoom room = Room;//속한 방 
                room.Push(room.Broadcast,CellPos,movePacket);//모두에게 알리기

                Console.WriteLine($"arrow move!({movePacket.PositionInfo.PosX}) ({movePacket.PositionInfo.PosY})");
            }
            else
            {
                GameObject go = Room.Map.Find(destPos);
                GameRoom room = Room;//방 저장 
                if(go != null)
                {
                    go.OnDamaged(Owner,Skill.damage + Owner.TotalAttack);
                    room.Push(room.LeaveGame,Info.Id);

                }
                else
                {
                    //벽에 부딛힘
                    room.Push(room.LeaveGame,Info.Id);

                }
            }
        }

        public override GameObject GetOwner()
        {
            return Owner;
        }
    }
}

