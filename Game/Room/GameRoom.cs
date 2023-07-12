using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Compiler;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Object;

namespace Server.Game
{
    public partial class GameRoom : JobSerializer
    {
        object _lock = new object();
        public int RoomId { get; set; } //룸 고유 번호

        Dictionary<int, Player> _players = new Dictionary<int, Player>();//유저
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();//몬스터 
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();//발사체 
        Dictionary<int, Resource> _resources = new Dictionary<int, Resource>();

        public Map Map { get; private set; } = new Map();//맵에 대한 정보를 로드할 때마다 갈아 끼움 !!

        //zone
        //ㅁ ㅁ ㅁ 
        //ㅁ ㅁ ㅁ 
        //ㅁ ㅁ ㅁ
        public const int ViewCells = 20;
        public Zone[,] Zones { get; private set; }
        public int ZoneCells { get; private set; }

        //cellPos -> x축 , y축
        //zone,pos -> 행렬

        //ㅁ ㅁ ㅁ
        //ㅁ ㅁ ㅁ 
        //ㅁ ㅁ ㅁ

        public Zone GetZone(Vector2Int cellPos)
        {
            int indexX = (cellPos.x - Map.MinX) / ZoneCells;
            int indexY = (Map.MaxY - cellPos.y) / ZoneCells;

            return GetZone(indexY, indexX);
        }
        public Zone GetZone(int indexY,int indexX)
        {
            if (indexY < 0 || indexY >= Zones.GetLength(0))
                return null;
            if (indexX < 0 || indexX >= Zones.GetLength(1))
                return null;

            return Zones[indexY, indexX];
        }
        public void Init(int mapId, int zoneCells)
        {
            Map.LoadMap(mapId);//맵 정보 갈아 끼우기

            //Zone
            ZoneCells = zoneCells;
            int countY = (Map.SizeY + zoneCells - 1) / ZoneCells;
            int countX = (Map.SizeX + zoneCells - 1) / ZoneCells;

            Zones = new Zone[countY, countX];//존의 개수 

            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {
                    Zones[y, x] = new Zone(y, x);//존 생성 
                }
            }
        }
        public void Update()//발사체 위치 업데이트 
        {
            lock (_lock)
            {
                //어떤 문에 닿으면 승리함 
                foreach (Monster m in _monsters.Values)
                {
                    m.Update();// 발사체 위치 업데이트 
                }
         
                foreach (Projectile p in _projectiles.Values)
                {
                    p.Update();
                }
                foreach(Resource r in _resources.Values)
                {
                    r.Update();
                }
             


                Flush();
            }
        }

        Random _rand = new Random();
        public void EnterGame(GameObject newObject, bool randomPos)//게임 룸에 입장 
        {
            if (newObject == null) return;

            if (randomPos)
            {
                Vector2Int randSpawn = new Vector2Int();

                randSpawn.x = _rand.Next(Map.MinX, Map.MaxX + 1);
                randSpawn.y = _rand.Next(Map.MinY, Map.MaxY + 1);

                //단 아이템위치에는 갈 수 있음 !  
                if (Map.Find(randSpawn) == null)
                    newObject.CellPos = randSpawn;
            }

            //오브젝트 타입 
            GameObjectType type = ObjectManager.GetObjTypeById(newObject.Info.Id);

            lock (_lock)
            {
                Vector2Int cellPos = new Vector2Int();

                if (type == GameObjectType.Player)
                {
                    Player newPlayer = newObject as Player;
                    if (newPlayer == null)//잘못된 정보
                        return;


                    _players.Add(newPlayer.Info.Id, newPlayer);//목록에 추가
                    newPlayer.Room = this;//게임 방 저장 

                    Map.ApplyMove(newPlayer, new Vector2Int(newPlayer.CellPos.x, newPlayer.CellPos.y));//갈 수 없는 구역에 추가 
                    //zone
                    cellPos = newPlayer.CellPos;
                    GetZone(cellPos).Players.Add(newPlayer);



                    SEnterGame enterPkt = new SEnterGame();//뉴비에게 자신의 정보 알려주기
                    enterPkt.Info = newPlayer.Info;

                    newPlayer.Session.Send(enterPkt);

                    //시야각 업데이트 !
                    newPlayer.Vision.Update();// enter 패킷 -> spawn 패킷 

                }
                else if (type == GameObjectType.Monster)//몬스터가 입장 
                {
                    Monster monster = newObject as Monster;

                    _monsters.Add(monster.Info.Id, monster);//사전에 추가
                    monster.Room = this;
                    Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));//갈 수 없는 구역에 추가 
                    //zone
                    GetZone(monster.CellPos).Monsters.Add(monster);
                    cellPos = monster.CellPos;
                }
                else if (type == GameObjectType.Projectile)//발사체가 입장 
                {
                    Projectile projectile = newObject as Projectile;
                    _projectiles.Add(projectile.Info.Id, projectile);//사전에 추가 
                    projectile.Room = this;

                    Map.ApplyMove(projectile, new Vector2Int(projectile.CellPos.x, projectile.CellPos.y), applyCollision: false);
                    //zone
                    GetZone(projectile.CellPos).Projectiles.Add(projectile);
                    cellPos = projectile.CellPos;
                }

                else if (type == GameObjectType.Resource)
                {

                    Resource resource = newObject as Resource;
                    _resources.Add(resource.Info.Id, resource);
                    resource.Room = this;
                    //아이템 자리 예외!
                    Map.ApplyMove(resource, new Vector2Int(resource.CellPos.x, resource.CellPos.y), applyCollision: false);
                    //zone 
                    GetZone(resource.CellPos).Resources.Add(resource);
                    cellPos = resource.CellPos;
                }
             
                SSpawn spawnPacket = new SSpawn();
                spawnPacket.Infos.Add(newObject.Info);
                Broadcast(cellPos, spawnPacket);

            }



        }
        public void LeaveGame(int objectId)//게임 룸에서 퇴장 + 위치 정보도 null 
        {
            GameObjectType type = ObjectManager.GetObjTypeById(objectId);
            GameObject go = null;
            lock (_lock)
            {
                Vector2Int cellPos = new Vector2Int();
                if (type == GameObjectType.Player)
                {

                    Player player = null;
                    //이 캐릭터를 리스트에서 삭제 

                    if (_players.Remove(objectId, out player) == false)
                        return;


                    //갈수없는 지역에서  삭제
                    Map.ApplyLeave(player);


                    //나가기 전에 hp저장
                    player.OnLeave();
                    player.Room = null;
                    go = player;

                    //본인한테 정보 전송
                    {
                        SLeaveGame leavePacket = new SLeaveGame() { Id = player.Info.Id };
                        player.Session.Send(leavePacket);
                    }
                }
                else if (type == GameObjectType.Monster)
                {
                    Monster monster = null;
                    //리스트에서 삭제 
                    if (_monsters.Remove(objectId, out monster) == false)
                        return;

                    //갈수 없는 지역에서 삭제 
                    Map.ApplyLeave(monster);

                    go = monster;
                    monster.Room = null;
                }
                else if (type == GameObjectType.Projectile)
                {
                    Projectile projectile = null;
                    if (_projectiles.Remove(objectId, out projectile) == false)
                        return;
                    //갈수 없는 지역에서 삭제

                    Map.ApplyLeave(projectile, applyCollision: false);


                    projectile.Room = null;
                    go = projectile;
                }
                else if (type == GameObjectType.Resource)
                {
                    Resource resource = null;
                    if (_resources.Remove(objectId, out resource) == false)
                        return;

                    Map.ApplyLeave(resource, applyCollision: false);

                    resource.Room = null;
                    go = resource;
                }


                SDespawn despawnPacket = new SDespawn();
                despawnPacket.Infos.Add(go.Info);
                Broadcast(cellPos, despawnPacket);

            }
        }

        public void Broadcast(Vector2Int cellPos, IMessage packet)
        {
            lock (_lock)
            {


                // 주변 존에 있는 사람에게만 뿌리자!
                //todo : 시야각 안 
                List<Zone> zones = GetAdjacentZones(cellPos);
                foreach (Zone zone in zones)
                {
                    foreach (Player p in zone.Players)
                    {
                        int dx = cellPos.x - p.CellPos.x;
                        int dy = cellPos.y - p.CellPos.y;

                        if (Math.Abs(dx) > ViewCells) continue;
                        if (Math.Abs(dy) > ViewCells) continue;


                        p.Session.Send(packet);
                    }

                }



            }
        }
        public List<Zone> GetAdjacentZones(Vector2Int cellPos, int range = ViewCells)
        {
            HashSet<Zone> zones = new HashSet<Zone>();//중복 x

            //ㅁㅁㅁㅁㅁㅁ
            //ㅁㅁㅁㅁㅁㅁ
            //ㅁㅁㅁㅁㅁㅁ
            int minX = cellPos.x - range;
            int maxX = cellPos.x + range;
            int minY = cellPos.y - range;
            int maxY = cellPos.y + range;

            Vector2Int leftTop = new Vector2Int(minX, maxY);
            Vector2Int rightBot = new Vector2Int(maxX, minY);

            int minIndexY = (Map.MaxY - leftTop.y) / ZoneCells;
            int minIndexX = (leftTop.x - Map.MinX) / ZoneCells;
            int maxIndexY = (Map.MaxY - rightBot.y) / ZoneCells;
            int maxIndexX = (rightBot.x - Map.MinX) / ZoneCells;

            for(int y = minIndexY; y <= maxIndexY; y++)
            {
                for(int x = minIndexX; x <= maxIndexX; x++)
                {
                    Zone zone = GetZone(y, x);
                    if (zone != null)
                        zones.Add(zone);
                }
            }


            return zones.ToList();
        }
        public void HandleMove(GameObject myObject, CMove movePacket)
        {
            lock (_lock)
            {

                PositionInfo movePosition = movePacket.PositionInfo;//클라에서 원하는 좌표 
                ObjectInfo info = myObject.Info;//현재 좌표 

                //todo : 검증
                Map.ApplyMove(myObject, new Vector2Int(movePosition.PosX, movePosition.PosY));


                //서버에 있는 정보 업데이트
                myObject.Info.PositionInfo = movePosition;

                //같은 방에 있는 사람들에게 뿌려줌
                SMove resMovePacket = new SMove();
                resMovePacket.Id = info.Id;
                resMovePacket.PositionInfo = info.PositionInfo;

                Broadcast(myObject.CellPos, resMovePacket);
            }

        }
        public void HandleSkill(Player player, CSkill skillPacket)
        {
            lock (_lock)
            {
                //todo :스킬 사용 가능 여부 판단 

                //Idle 상태에서만 스킬 사용
                ObjectInfo info = player.Info;
                if (info.PositionInfo.State != CreatureState.Idle)
                    return;


                // TODO : 스킬 사용 가능 여부 체크
                info.PositionInfo.State = CreatureState.Skill;
                SSkill skill = new SSkill() { SkillInfo = new SkillInfo() };
                skill.Id = info.Id;
                skill.SkillInfo.SkillId = skillPacket.SkillInfo.SkillId;
                Broadcast(player.CellPos, skill);

                Data.Skill skillData = null;
                if (DataManager.SkillDict.TryGetValue(skillPacket.SkillInfo.SkillId, out skillData) == false)
                    return;

                switch (skillData.skillType)
                {
                    case SkillType.Punch:
                        {
                            Vector2Int skillPos = player.GetFrontCellPos(info.PositionInfo.MoveDir);
                            GameObject target = Map.Find(skillPos);
                            if (target != null)
                            {
                                target.OnDamaged(player, player.TotalAttack);
                                Console.WriteLine("Hit GameObject !");
                            }
                        }
                        break;
                    case SkillType.Arrow:
                        {
                            Arrow arrow = ObjectManager.Instance.Add<Arrow>();

                            //json 파일 
                            arrow.Skill = skillData;
                            arrow.Owner = player;
                            arrow.Room = this;
                            arrow.Info.PositionInfo.State = CreatureState.Moving;//움직이는 중
                            arrow.Info.PositionInfo.MoveDir = info.PositionInfo.MoveDir;//내가 보는 방향 
                            arrow.CellPos = player.CellPos;//주인님과 같은 위치

                            //화살 스폰시 모두에게 알려주자 
                            arrow.Stat.Speed = 15;

                            EnterGame(arrow, false);//게임룸 입장 
                        }
                        break;
                }




            }

        }
    
        
         Player FindPlayer(Func<GameObject,bool> condition)
         {
            foreach(Player p in _players.Values)
            {
                if (condition.Invoke(p))
                    return p;
            }
            return null;

        }
        public Player FindClosestPlayer(Vector2Int cellPos, int range)
        {
            List<Zone> zones = GetAdjacentZones(cellPos, range);
            //근접 존에 있는 모든 플레이어 
            List<Player> players = new List<Player>();
            foreach(Zone zone in zones)
            {
                foreach(Player player in zone.Players)
                {
                    players.Add(player);
                }
            }
            //직선거리를 기준으로 소팅 
            players.Sort((left, right) =>
            {
                int leftCellDist = (left.CellPos - cellPos).CellDist();
                int rightCellDist = (right.CellPos - cellPos).CellDist();
                return leftCellDist - rightCellDist;
            });

            //실제 경로 기준으로 소팅
            foreach(Player player in players)
            {
                List<Vector2Int> path = Map.FindPath(cellPos, player.CellPos, true);
                if (path.Count < 2 || path.Count > range)
                    continue;
                return player;
            }
            return null;
        }


       

    }
}
