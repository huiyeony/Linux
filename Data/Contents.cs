using System;
using System.Collections.Generic;
using Google.Protobuf.Protocol;

namespace Server.Data
{
    #region Stat
    [Serializable]
    public class Stat
    {
        public int level;
        public int maxHp;
        public int attack;
        public int speed;
        public int exp;
        public int maxExp;
        public int mental;
    }

    [Serializable]
    public class StatLoader : ILoader<int, Stat>
    {
        public List<Stat> stats = new List<Stat>();

        public Dictionary<int, Stat> MakeDict()
        {
            Dictionary<int, Stat> dict = new Dictionary<int, Stat>();
            foreach (Stat stat in stats)
                dict.Add(stat.level, stat);
            return dict;
        }
    }
    #endregion

    #region Skill
    //스킬 + 데미지소스 & 데미지타입 
    [Serializable]
    public class Skill
    {
        public int id;
        public float cooldown;
        public int damage;
        public SkillType skillType;
        public ProjectileInfo projectileInfo;
        public int range;
    }
    [Serializable]
    public class ProjectileInfo
    {
        public float speed;
        public string dataPath;
    }
    [Serializable]
    public class SkillLoader : ILoader<int, Skill>
    {
        public List<Skill> skills = new List<Skill>();

        public Dictionary<int, Skill> MakeDict()
        {
            Dictionary<int, Skill> skillDict = new Dictionary<int, Skill>();

            foreach (Skill skill in skills)
            {
                skillDict.Add(skill.id, skill);

            }

            return skillDict;
        }
    }
    #endregion

    #region Monster
    [Serializable]
    public class RewardData
    {
        public int templateId;
        public int count;
        public int probability;
    }
    [Serializable]
    public class MonsterData
    {
        public int templateId;
        public string name;
        public StatInfo stat;
        public List<RewardData> rewardDatas;
    }

    [Serializable]
    public class MonsterLoader : ILoader<int, MonsterData>
    {
        public List<MonsterData> monsters = new List<MonsterData>();

        public Dictionary<int, MonsterData> MakeDict()
        {
            Dictionary<int, MonsterData> dict = new Dictionary<int, MonsterData>();
            foreach (MonsterData monster in monsters)
                dict.Add(monster.templateId, monster);
            return dict;
        }
    }
    #endregion


    #region item

    //~data : 템플릿 데이터 !!
    //~info : google protocol 형식 !!

    [Serializable]
    public class ItemData
    {
        public ItemType itemType;
        public int templateId;
        public string name;
        public string dataPath;
        public string prefabPath;

    }
    [Serializable]
    public class WeaponData : ItemData
    {
        public Google.Protobuf.Protocol.WeaponType weaponType;
        public int damage;

    }
    [Serializable]
    public class ArmorData : ItemData
    {
        public Google.Protobuf.Protocol.ArmorType armorType;
        public int defence;

    }
    [Serializable]
    public class ConsumableData : ItemData
    {

        public Google.Protobuf.Protocol.ConsumableType consumableType;
        public int maxCount;


    }
    public class ItemLoader : ILoader<int, ItemData>
    {
        public List<WeaponData> weaponDatas = new List<WeaponData>();
        public List<ArmorData> armorDatas = new List<ArmorData>();
        public List<ConsumableData> consumableDatas = new List<ConsumableData>();


        public Dictionary<int, ItemData> MakeDict()
        {
            Dictionary<int, ItemData> itemDict = new Dictionary<int, ItemData>();
            foreach (WeaponData weapon in weaponDatas)
            {
                weapon.itemType = ItemType.Weapon;//아이템 타입 지정    
                itemDict.Add(weapon.templateId, weapon);
            }
            foreach (ArmorData armor in armorDatas)
            {
                armor.itemType = ItemType.Armor;//아이템 타입 지정 
                itemDict.Add(armor.templateId, armor);
            }
            foreach (ConsumableData consumable in consumableDatas)
            {
                consumable.itemType = ItemType.Consumable;//아이템 타입 지정 
                itemDict.Add(consumable.templateId, consumable);
            }

            return itemDict;
        }

    }
    #endregion 


    #region npc
    [Serializable]
    public class NpcData
    {
        public int templateId;
        public int posX;
        public int posY;
        public string dataPath;
        
    }
    [Serializable]
    public class NpcLoader : ILoader<int, NpcData>
    {
        public List<NpcData> npcDatas = new List<NpcData>();

        public Dictionary<int, NpcData> MakeDict()
        {
            Dictionary<int, NpcData> dict = new Dictionary<int, NpcData>();
            foreach(NpcData npc in npcDatas)
            {
                dict.Add(npc.templateId, npc);
            }

            return dict;
        }
    }


    #endregion

    #region Quest 
    [Serializable]
    public class QuestData
    {
        public int templateId;
        public string questName;
        public int nonePlayerId;
    }
    [Serializable]
    public class QuestLoader : ILoader<int, QuestData>
    {
        public List<QuestData> quests = new List<QuestData>();
        public Dictionary<int, QuestData> MakeDict()
        {
            Dictionary<int, QuestData> dict = new Dictionary<int, QuestData>();
            foreach (QuestData quest in quests)
            {
                dict.Add(quest.templateId, quest);
            }

            return dict;
        }
    }
    #endregion
}

