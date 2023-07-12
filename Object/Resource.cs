using System;
using Google.Protobuf.Protocol;
using Server.DB;
using Server.Game;

namespace Server.Object
{
    public class Resource : GameObject
    {
        public int TemplateId
        {
            get {  return Info.TemplateId;}
            set { Info.TemplateId = value;}
        }
        public int count { get; set; }

        public Resource()
        {
            GameObjectType = GameObjectType.Resource;//오브젝트 타입 
        }
        public void Init(int id)
        {
            TemplateId = id;
        }

        public override void Update()
        {
            if (Room == null)
                return;


            Vector2Int destPos = CellPos;//나의 좌표 
        
            //갈수 없는 곳이면 아이템 획득 하고  & 소멸

            if (!Room.Map.CanGo(destPos))
            {
                GameObject go = Room.Map.Find(destPos);
                GameRoom room = Room;//방 저장 
                if (go != null)
                {
                    //db에 저장
                    Player player = go as Player;
                    if (player == null)
                        return;//몬스터와 충돌
                    
                    if (player.Inven.GetEmptySlot() == -1)
                        return;

                    room.Push(room.LeaveGame, Info.Id);
                    DbTransaction.Instance.SavePlayerReward(player, TemplateId,count, room);
                    

                }
                else
                {
                    //벽에 부딛힘
                    room.Push(room.LeaveGame, Info.Id);

                }
            }
        }
    }
}

