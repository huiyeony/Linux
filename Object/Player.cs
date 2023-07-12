using System;
using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Session;
using Server.Util;
using static Server.DB.DataModel;

namespace Server.Object
{
    public class Player : GameObject
    {

        public VisionCube Vision { get;  set; }//시야각! 
        public static int WeaponAttack { get; set; }
        public static int ArmorDefence { get; set; }


        public override int TotalAttack { get { return Stat.Attack + WeaponAttack; } } 
        public override int TotalDefence { get { return ArmorDefence; } } 


        public Inventory Inven { get; } = new Inventory();

        public int PlayerDbId {get;set;}//db 아이디 
        public ClientSession Session { get; set; }//전담 세션 저장


        public Player()
        {
            //오브젝트 타입 
            GameObjectType = GameObjectType.Player;
            //비전큐브 ! 
            Vision = new VisionCube(this);
          
        }
        public override int Exp
        {
            get { return base.Exp; }
            set
            {

                Stat.Exp = Math.Min(value,Stat.MaxExp);
                
            }
        }
        public override void OnDamaged(GameObject attacker, int damage)
        {

            damage = Math.Max(damage - ArmorDefence, 0);//방어구 
           
            base.OnDamaged(attacker,damage);

        }
        public void HandleEquipItem(CEquipItem equipPacket)
        {
            Item equipItem = Inven.Get(equipPacket.ItemDbId);
            if (equipItem == null)//없는 아이템 ?
                return;


            //착용중인 아이템 버리기 
            if (equipPacket.Equipped)
            {

                ItemType itemType = equipItem.itemType;
                //벗게될 아이템 
                Item unEquipItem = null;

                switch (itemType)
                {
                    //-공격 -방어 
                    case ItemType.Weapon:
                        unEquipItem = Inven.Find(i => i.equipped == true && i.itemType == itemType && ((Weapon)i).weaponType == ((Weapon)equipItem).weaponType);
                        if (unEquipItem != null && unEquipItem.equipped == false)//클라에 알려줌
                        {
                            HandleRemoveItem(unEquipItem.itemDbId);
                        }
                        break;
                    case ItemType.Armor:
                        unEquipItem = Inven.Find(i => i.equipped == true && i.itemType == itemType && ((Armor)i).armorType == ((Armor)equipItem).armorType);
                        if (unEquipItem != null && unEquipItem.equipped == false)//클라에 삭제 요청 
                        {
                            HandleRemoveItem(unEquipItem.itemDbId);
                        }
                        break;
                }

            }

            //아이템 착용 or 착용 해제 
            {
                equipItem.equipped = equipPacket.Equipped;
                
            }

            //db에 요청 
            DbTransaction.Instance.EquipItem(this, equipItem);

            //클라에 알려줌
            SEquipItem equipOkPacket = new SEquipItem()
            {
                ItemDbId = equipItem.itemDbId,
                Equipped = equipItem.equipped

            };
            Session.Send(equipOkPacket);

            RefreshStat();


        }
        public void HandleRemoveItem(int itemDbId)
        {
            Item removeItem = Inven.Get(itemDbId);
            if (removeItem == null)//없는 아이템 ?
                return;

            //인벤 정보에서 삭제 
            bool success = Inven.Remove(itemDbId);

            //db에 요청
            DbTransaction.Instance.RemoveItem(this, removeItem);

            SRemoveItem removeOkPacket = new SRemoveItem();
            removeOkPacket.ItemDbId = removeItem.itemDbId;
            Session.Send(removeOkPacket);
        }
        public void RefreshStat()
        {
            WeaponAttack = 0;
            ArmorDefence = 0;

            foreach(Item item in Inven.Items.Values)
            {
                if (item.equipped == false) continue;
                switch (item.itemType)
                {
                    case ItemType.Weapon:
                        WeaponAttack += ((Weapon)item).damage;
                        break;
                    case ItemType.Armor:
                        ArmorDefence += ((Armor)item).defence;
                        break;
                }
            }
        }
        
        public override void OnDead(GameObject attacker)
        {
            Info.PositionInfo.State = CreatureState.Dead;//죽었음 

            //죽었다는 사실을 모두에게 알리기
            SDie diePacket = new SDie();
            diePacket.Id = Info.Id;
            diePacket.AttackerId = attacker.Info.Id;

            GameRoom room = Room;//속한 룸
            room.Broadcast(CellPos, diePacket);//예외처리 job

            //룸에서 나가기
            room.Push(room.LeaveGame,Info.Id);//예외처리 job

            //onDead -> lobby -> 처음부터 다시 시작 
            // [ 로비 ]
            //시작하려면 화면을 터치하세요 (깜빡깜빡 )
            // ( START )
            // 
            
            
            //비전큐브 ! 
            Vision.Clear();

            //같은 캐릭터로 재입장
            Info.PositionInfo.State = CreatureState.Idle;
            Info.PositionInfo.MoveDir = MoveDir.Down;
            Info.PositionInfo.PosX = 0;
            Info.PositionInfo.PosY = 0;

            Info.StatInfo.Hp = Info.StatInfo.MaxHp;//hp회복
            

            room.Push(room.EnterGame,this,false);//지정 스폰 
        }
        public void OnLeave()
        {
            //hp를 저장
            //todo
            GameRoom room = Room;
            DbTransaction.Instance.SavePlayerHp(this,room);



        }

    }
}

