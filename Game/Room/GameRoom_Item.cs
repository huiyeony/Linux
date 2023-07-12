using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using Server.Object;

namespace Server.Game
{
    public partial class GameRoom : JobSerializer
    {

        public void HandleEquipItem(Player player, CEquipItem equipPacket)
        {
            //메모리에 선 저장
            if (player == null)
                return;

            player.HandleEquipItem(equipPacket);
        }
   
    }
}
