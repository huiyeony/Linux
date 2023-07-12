using System;
using System.Linq;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;
using Server.Object;
using Server.Util;
using static Server.DB.DataModel;

namespace Server.DB
{
    public class DbTransaction : JobSerializer
    {

        public static DbTransaction Instance { get; private set; } = new DbTransaction();

        //room
        public void SavePlayerHp(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            //db 
            Instance.Push(SavePlayerHpDb, player, room);


        }
        public void SavePlayerHpDb(Player player,GameRoom room)
        {
            using (AppDbContext db = new AppDbContext())
            {
                PlayerDb playerDb = db.Players.Find(player.PlayerDbId);
                playerDb.Hp = player.Stat.Hp;
                bool success = db.SaveChangesEx();

                if (success)
                {
                    room.Push(SavePlayerHpRoom, player);
                }


            }
        }
        public void SavePlayerHpRoom(Player player)
        {
            Console.WriteLine($"save Hp({player.Stat.Hp})");
        }

        public void SavePlayerReward(Player player,int templateId, int count,GameRoom room)
        {
            if (player == null ||  room == null)
                return;
            //room

            //살짝 문제가 있음 ..
            //1)메모리 빈슬롯 확인 
            //2)db에 요청 ok
            //3)메모리에 저장 
            ItemDb itemDb = new ItemDb()
            {
                TemplateId = templateId,
                Count = count,
                Slot = player.Inven.GetEmptySlot(),
                OwnerDbId = player.PlayerDbId,
                Equipped = false
            };
            //db연결

            using (AppDbContext db = new AppDbContext())
            {
                db.Items.Add(itemDb);
                bool success = db.SaveChangesEx();

                if (success)
                {
                    room.Push(() =>
                    {
                        Item newItem = Item.MakeItem(itemDb);
                        player.Inven.Add(newItem);//인벤토리에 저장
                        //클라에 전송
                        ItemInfo itemInfo = new ItemInfo()
                        {
                            ItemDbId = newItem.itemDbId,
                            TemplateId = newItem.templateId,
                            Count = newItem.count,
                            Slot = newItem.slot,
                            Equipped = newItem.equipped
                        };
                        SAddItem addPacket = new SAddItem();
                        addPacket.ItemInfos.Add(itemInfo);
                        
                        player.Session.Send(addPacket);
                    });


                }
            }
        }

        public void EquipItem(Player player,Item item)
        {
            using(AppDbContext db = new AppDbContext())
            {
                ItemDb itemDb = new ItemDb()
                {
                    ItemDbId = item.itemDbId,
                    Equipped = item.equipped

                };
                db.Entry(itemDb).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                db.Entry(itemDb).Property("Equipped").IsModified = true;//한개만 수정 !

                bool success = db.SaveChangesEx();
                if (!success)
                {
                    //저장 실패 
                }
            }
        }
        public void RemoveItem(Player player, Item item)
        {
            using(AppDbContext db = new AppDbContext())
            {
                ItemDb itemDb = db.Items.Where(i => i.ItemDbId == item.itemDbId)
                    .FirstOrDefault();
                if (itemDb == null) return;
                db.Items.Remove(itemDb);

                bool success = db.SaveChangesEx();
                if (!success)
                {
                    //저장 실패 
                }

            }
        }

   
    }
}

