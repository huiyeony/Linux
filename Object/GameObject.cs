using System;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace Server.Object
{
    public class GameObject
    {
        public virtual int TotalAttack { get { return Stat.Attack ; } } 
        public virtual int TotalDefence { get { return 0; } }

        public GameObjectType GameObjectType { get; protected set; } = GameObjectType.None;//초기값 


        public GameRoom Room { get; set; } //속해 있는 게임룸 정보
        //현재 경험치 0 
        public virtual int Exp
        {
            get { return Info.StatInfo.Exp; }
            set
            {
                //최대 경험치를 넘어서는 안됨
                Stat.Exp = Math.Min(value, Stat.MaxExp);
            }

        }
        //Info <-> PosInfo && StatInfo 
        public ObjectInfo Info { get; set; } = new ObjectInfo()
        {
            PositionInfo = new PositionInfo(),
            StatInfo = new StatInfo()
        };
        public CreatureState State//Info 접근자  
        {
            get { return Info.PositionInfo.State; }
            set {
                
                Info.PositionInfo.State = value;
            }
        }
        public StatInfo Stat
        {
            get
            {
                return Info.StatInfo;
            }
            set
            {
                if (Info.StatInfo.Equals(value))
                    return;
                
                Info.StatInfo = value;
            }
        }
        public int Hp
        {
            get { return Stat.Hp; }
            set
            {
                if (Stat.Hp == value)
                    return;
                //보정값 적용 
                Stat.Hp = Math.Max(value, 0);
                Console.WriteLine($"({Stat.Hp})");
            }
        }
        public Vector2Int CellPos//info 접근자 
        {
            get
            {
                return new Vector2Int(Info.PositionInfo.PosX, Info.PositionInfo.PosY);
            }

            set
            {
                Info.PositionInfo.PosX = value.x;
                Info.PositionInfo.PosY = value.y;
            }
        }
        public MoveDir Dir
        {
            get { return Info.PositionInfo.MoveDir; }
            set
            {
                Info.PositionInfo.MoveDir = value;
            }
        }
        public virtual GameObject GetOwner()
        {
            return this;//주인님 반환 
        }
        public virtual void Update()
        {
            // 
        }
        public MoveDir GetDirFromVec(Vector2Int dir)
        {
            if (dir.x > 0)
                return MoveDir.Right;
            else if (dir.x < 0)
                return MoveDir.Left;
            else if (dir.y > 0)
                return MoveDir.Up;
            else
                return MoveDir.Down;
        }

        public virtual void OnDamaged(GameObject attacker,int damage)
        {

            if (Room == null)
                return;
            GameRoom room = Room;//속한 방

            //! 공격받은 상태로 바꾸기

            Hp -= damage;

            SChangeHp changePacket = new SChangeHp();
            changePacket.Id = Info.Id;
            changePacket.Hp = Stat.Hp;
            room.Push(room.Broadcast,CellPos, changePacket);//모두에게 알린다 

            if (Stat.Hp <= 0)
                OnDead(attacker);


        }
        public virtual void OnDead(GameObject attacker)
        {
            
            //
        }
        public Vector2Int GetFrontCellPos(MoveDir dir) //바라보고 있는 앞 좌표 
        {
            
            Vector2Int cellPos = CellPos;

            switch (dir)
            {
                case MoveDir.Up:
                    cellPos += Vector2Int.up;
                    break;
                case MoveDir.Down:
                    cellPos += Vector2Int.down;
                    break;
                case MoveDir.Left:
                    cellPos += Vector2Int.left;
                    break;
                case MoveDir.Right:
                    cellPos += Vector2Int.right;
                    break;
            }

            return cellPos;
        }
    

        

    }
}

