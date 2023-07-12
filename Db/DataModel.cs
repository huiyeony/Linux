using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.DB
{
    public class DataModel
    {
        //계정
        [Table("Account")]
        public class AccountDb
        {
            public int AccountDbId { get; set; }
            public string AccountDbName { get; set; }

            public ICollection<PlayerDb> PlayerDbs { get; set; }
        }
        //캐릭터 플레이어
        [Table("Player")]
        public class PlayerDb
        {
            public int PlayerDbId { get; set; }
            public string PlayerDbName { get; set; }

            [ForeignKey("Account")]
            public int AccountDbId { get; set; }
            public AccountDb Account { get; set; }
            
            public List<ItemDb> Items { get; set; }

            //게임속 시간
            public int Days { get; set; }
            public float Time { get; set; }

            //stat 정보 
            public int Level { get; set; }
            public int Hp { get; set; }
            public int MaxHp { get; set; }
            public int Attack { get; set; }
            public float Speed { get; set; }
            public int Exp { get; set; }
            public int MaxExp { get; set; }
            public int Mental { get; set; }
        }
        [Table("Item")]
        public class ItemDb
        {
            public int ItemDbId { get; set; }
            public int TemplateId { get; set; }//사용자 지정 id ?
            public int Count { get; set; }
            public int Slot { get; set; }
            public bool Equipped { get; set; }

            [ForeignKey("Owner")]
            public int? OwnerDbId { get; set; }
            public PlayerDb Owner { get; set; }
            
        }
        [Table("Quest")]
        public class QuestDb
        {
            public int QuestDbId { get; set; }
            public int TemplateId { get; set; }
            public int Slot { get; set; }

            [ForeignKey("Owner")]
            public int? OwnerDbId { get; set; }
            public PlayerDb Owner { get; set; }
        }
    }
}

