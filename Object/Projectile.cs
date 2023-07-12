using System;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Object
{
    public class Projectile : GameObject
    {
        public Skill Skill { get; set; }

        public Projectile()
        {
            //오브젝트 타입 
            GameObjectType = GameObjectType.None;
        }

        public override void Update()
        {

        }
    }
}

