using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Util;
using static Server.DB.DataModel;

namespace Server.Object
{
    public class Monster :GameObject
    {
        public override int TotalAttack { get { return Stat.Attack + Skill.damage; } }
        public override int TotalDefence { get { return 0; } }//몬스터는 방어 0 
        public Skill Skill { get; private set; }//스킬 정보 저장 
        public int TemplateId { get; private set; }

        public int _searchRange = 10;
        public Monster()
        {
            GameObjectType = GameObjectType.Monster;//오브젝트 타입

        }
        public void Init(int templateId)
        {
            MonsterData monData = null;
            DataManager.MonsterDict.TryGetValue(templateId, out monData); // ~~ 몬스터가 되어라 

            TemplateId = monData.templateId; //id
            Info.Name = monData.name;//name
            Info.StatInfo.MergeFrom(monData.stat);//stat
            Info.StatInfo.Hp = monData.stat.MaxHp;

            Info.PositionInfo.MoveDir = MoveDir.Down;
            Info.PositionInfo.State = CreatureState.Idle;
            

            CellPos = new Vector2Int(0, 0);

            Skill skill = null;
            DataManager.SkillDict.TryGetValue(1, out skill); 
            if (skill != null)
                Skill = skill;

        }

        public override void Update()
        {
           
            switch (State)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Moving:
                    UpdateMoving();
                    break;
                case CreatureState.Skill:
                    UpdateSkill();
                    break;
                case CreatureState.Dead:
                    UpdateDead();
                    break;
              

            }
        }
        Player _target = null;
        long _nextTick = 0;
        public virtual void UpdateIdle()
        {
            //매틱마다 -> 한 칸 이동 
            if (_nextTick > System.Environment.TickCount64)
                return;
            int moveTick = 50;//
            _nextTick = System.Environment.TickCount64 + moveTick;

            //모든 플레이어x  근접 존에 있는 플레이어o 
            Player player = Room.FindClosestPlayer(CellPos, _searchRange);

            if (player == null)//주변에 플레이어가 없음 
                return;

            _target = player;//타겟으로 저장 
            //쫒아가자
            State = CreatureState.Moving;

        }
        long _nextMoveTick = 0;
        public virtual void UpdateMoving()
        {
            //매틱마다 -> 한 칸 이동 
            if (_nextMoveTick > System.Environment.TickCount64)
                return;
            int moveTick = (int)(1000 / Stat.Speed);// 1초에 몬스터가 이동하는 거리 => speed 
            _nextMoveTick = System.Environment.TickCount64 + moveTick;

            //타겟의 유효성 검증 
            if (_target == null || _target.Room != Room)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            //장애물을 고려 x 경로
            Vector2Int dir = _target.CellPos - CellPos;
            int cellDist = (dir).CellDist();
            if (cellDist == 0 || cellDist > 20)
            {
                _target = null;
                State = CreatureState.Idle;
                //알려쥼 
                BroadcastMove();
                return;
            }
            //장애물을 고려한 경로 !!
            List<Vector2Int>  path = Room.Map.FindPath(CellPos, _target.CellPos);
            if (path.Count < 2 || path.Count > 20)
            {
                _target = null;
                State = CreatureState.Idle;
                //알려줌 
                BroadcastMove();
                return;
            }
            
            //스킬 상태로 갈지 판정
            //스킬 사정거리  && 직선 거리에 위치 함
                
            if (dir.CellDist() <=  Skill.range && (dir.x == 0 || dir.y == 0) )//일직선 !
            {
                //서서 공격하는 애니메이션 수정 필요 !
                State = CreatureState.Skill;
                //알려쥼 
                BroadcastMove();
                return;
            }

            
            Info.PositionInfo.MoveDir = GetDirFromVec(path[1] - CellPos);//종점 - 시점 
            //못가는 지역에도 추가
            if(Room.Map.ApplyMove(this, path[1]) == false)//갈 수 없는 지역이라면 
            {
                
                _target = null;
                State = CreatureState.Idle;
              
            }

            //알려줌
            BroadcastMove();
        }
        public void BroadcastMove()
        {

            //알려줌
            SMove movePacket = new SMove();
            movePacket.Id = Info.Id;
            movePacket.PositionInfo = Info.PositionInfo;

            GameRoom room = Room;//속한 방 
            room.Push(room.Broadcast,CellPos ,movePacket);
        }

       
        long _nextTime = 0;
        public virtual void UpdateSkill()
        {
            if(_nextTime == 0)
            {
                //유효한 타겟인지
                if (_target == null || Skill == null || _target.Room != Room || _target.Stat.Hp <= 0)
                {
                    _target = null;
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }
                //스킬 사정거리 && 직선 거리 임 
                Vector2Int dir = _target.CellPos - CellPos;

                
                GameRoom room = Room;
                
                bool skillOk = dir.CellDist() <= 1 && (dir.x == 0 || dir.y == 0);
                if (skillOk == false)
                {
                
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }
                //스킬 타게팅 바라봄
                MoveDir targetDir = GetDirFromVec(_target.CellPos - CellPos);
                if (Info.PositionInfo.MoveDir != targetDir)
                {
                    Info.PositionInfo.MoveDir = targetDir;
                    //알려쥼 
                    BroadcastMove();
                }

                

                //스킬 정보 가져옴 
                Skill skill = null;
                DataManager.SkillDict.TryGetValue(Skill.id, out skill);



                //모두에게 알림
                SSkill skillPacket = new SSkill() { SkillInfo = new SkillInfo() };
                skillPacket.Id = Info.Id;
                skillPacket.SkillInfo.SkillId = skill.id;
                //알려쥼
                room.Push(room.Broadcast, CellPos, skillPacket);


                //데미지 판정
                _target.OnDamaged(this, TotalAttack);

                //스킬 사용 시간
                int tick = (int)(1000 * 1.5);//
                _nextTime = Environment.TickCount64 + tick;


              
            }
            //쿨타임 체크  
            if (_nextTime > Environment.TickCount64)
            {
                return;
            }
                

            _nextTime = 0;//쿨타임 생성 




        }
        
        
        
        public override void OnDead(GameObject attacker)
        {
            _target = null;//타겟 없애기 
            GameRoom room = Room;//속한 룸
            //죽은 자리 기억 
            int posX = CellPos.x;
            int posY = CellPos.y;

            Info.PositionInfo.State = CreatureState.Dead;//죽었음 

            //죽었다는 사실을 모두에게 알리기
            SDie diePacket = new SDie();
            diePacket.Id = Info.Id;
            diePacket.AttackerId = attacker.Info.Id;


            room.Map.ApplyLeave(this);
            room.Broadcast(CellPos,diePacket);//예외처리 job

            //룸에서 나가기
            room.Push(room.LeaveGame,Info.Id);

            //같은 캐릭터로 재입장
            Info.PositionInfo.State = CreatureState.Idle;
            Info.PositionInfo.MoveDir = MoveDir.Down;
            Info.PositionInfo.PosX = -5;
            Info.PositionInfo.PosY = -5;

            Info.StatInfo.Hp = Info.StatInfo.MaxHp;//hp회복 

            room.Push(room.EnterGame, this, false);

            //todo : 아이템 드롭
            //플레이어가 몬스터 킬 
            GameObjectType type = attacker.GetOwner().GameObjectType;
            if (type == GameObjectType.Player)
            {

                Player player = attacker as Player;
                RewardData rw = DropReward();
                if (rw == null)
                    return;

                //x기존 코드에 버그가 생기면xxxx
                Resource resource = ObjectManager.Instance.Add<Resource>();//id 발급 
                resource.CellPos = new Vector2Int(posX, posY);
                resource.Init(rw.templateId);

                room.EnterGame(resource,false);//지정위치 스폰 




            }

        }
        public RewardData DropReward()
        {
            MonsterData monData = null;
            DataManager.MonsterDict.TryGetValue(TemplateId, out monData);

            //10 10 10 10 10
            int rand = new Random().Next(1, 101);// 1 ~ 101까지 랜덤한 숫자 
            int sum = 0;
            foreach (RewardData reward in monData.rewardDatas)
            {
                sum += reward.probability;
                if (rand <= sum) // [1-10] [11-20] .. [41-50] [꽝 ]  
                    return reward;
            }
            return null;//꽝 
        }
        public virtual void UpdateDead()
        {

        }

      

        
    }
}

