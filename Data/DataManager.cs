using System;
using System.Collections.Generic;
using System.IO;
using static System.Net.Mime.MediaTypeNames;


namespace Server.Data
{
    public interface ILoader<Key, Value>
    {
        public Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        public static Dictionary<int, Stat> StatDict { get; set; } = new Dictionary<int, Stat>();
        public static Dictionary<int, Skill> SkillDict { get; set; } = new Dictionary<int, Skill>();
        public static Dictionary<int, ItemData> ItemDict { get; set; } = new Dictionary<int, ItemData>();
        public static Dictionary<int, MonsterData> MonsterDict { get; set; } = new Dictionary<int, MonsterData>();
        public static Dictionary<int, Data.NpcData> NpcDict { get; private set; } = new Dictionary<int, Data.NpcData>();
        public static Dictionary<int, Data.QuestData> QuestDict { get; private set; } = new Dictionary<int, QuestData>();

        public static void LoadData()
        {
            //json 모드 로드
            StatDict = LoadJson<StatLoader, int, Stat>("StatData").MakeDict();
            SkillDict = LoadJson<SkillLoader, int, Skill>("SkillData").MakeDict();
            ItemDict = LoadJson<ItemLoader, int, ItemData>("ItemData").MakeDict();
            MonsterDict = LoadJson<MonsterLoader, int, MonsterData>("MonsterData").MakeDict();
            NpcDict = LoadJson<NpcLoader, int, NpcData>("NpcData").MakeDict();
            QuestDict = LoadJson<QuestLoader, int, QuestData>("QuestData").MakeDict();


        }
        public static Loader LoadJson<Loader,Key,Value>(string path) where Loader:ILoader<Key,Value> , new()//원하는 json파일을 로드 하자 
        {


            string text = File.ReadAllText($"{ConfigManager.Configuration.dataPath}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
           
       
        }
    }
}

