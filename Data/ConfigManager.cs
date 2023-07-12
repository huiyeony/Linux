using System;
using System.IO;

namespace Server.Data
{
    [Serializable]
    public class ServerConfig
    {
        public string dataPath;
    }
    //래핑 클래스 ! 
    public class ConfigManager
    {
        public static ServerConfig Configuration;//환경 설정 내용을 property 으로 가짐 

        //텍스트 파일을 클래스 객체로 만듬 
        public static void LoadJson()
        {
            string text = File.ReadAllText("./Data/Config.json");
            Configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfig>(text);
        }




    }
}

