using System;
using ServerCore;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Protocol;
using Server.Object;
using System.Threading;

namespace Server.Game
{

    public struct Pos
    {
        public Pos(int y, int x) { Y = y; X = x; }
        public int Y;
        public int X;

        public static bool operator== (Pos pos1, Pos pos2)
        {
            return (pos1.Y == pos2.Y && pos1.X == pos2.X);
        }
        public static bool operator!= (Pos pos1,Pos pos2)
        {
            return !(pos1 == pos2);
        }
        public override int GetHashCode()
        {
            long value = (Y << 32 | X);
            return value.GetHashCode();//x와 y가 같으면 같은 해쉬코드 가짐 ! 
        }

        public override bool Equals(object obj)
        {
            Pos pos = (Pos)obj;
            return (Y == pos.Y && X == pos.X);
        }
    }

    public struct PQNode : IComparable<PQNode>
    {
        public int F;
        public int G;
        public int Y;
        public int X;

        public int CompareTo(PQNode other)
        {
            if (F == other.F)
                return 0;
            return F < other.F ? 1 : -1;
        }
    }
    //2차원 벡터 정의
    public struct Vector2Int
    {
        public int x;
        public int y;

        public static Vector2Int up { get { return new Vector2Int(0, 1); } }
        public static Vector2Int down { get { return new Vector2Int(0, -1); } }
        public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
        public static Vector2Int right { get { return new Vector2Int(1, 0); } }
        //+ - 정의
        public static Vector2Int operator+ (Vector2Int a,Vector2Int b)
        {
            return new Vector2Int(a.x + b.x, a.y + b.y);
        }
        public static Vector2Int operator- (Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x - b.x, a.y - b.y);
        }
        
        public Vector2Int(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
        public float Norm(Vector2Int a) { return (float)Math.Sqrt(SqNorm()); } //||norm|| 
        public int SqNorm() { return (x * x) + (y * y); } // ||norm||*2

        public int CellDist() { return Math.Abs(x) + Math.Abs(y); } // 자기 자신으로 연산 하자 
      
    }



    public class Map
    {

        public int MinX { get; set; }
        public int MaxX { get; set; }
        public int MinY { get; set; }
        public int MaxY { get; set; }

        public int SizeX { get { return MaxX - MinX + 1; } }
        public int SizeY { get { return MaxY - MinY + 1; } }

        bool[,] _collision; //맵 장애물 정보 
        GameObject[,] _objects; //플레이어 위치 정보 

        public bool CanGo(Vector2Int cellPos) 
        {
            if (cellPos.x < MinX || cellPos.x > MaxX)
                return false;
            if (cellPos.y < MinY || cellPos.y > MaxY)
                return false;

            int x = cellPos.x - MinX;
            int y = MaxY - cellPos.y;


             return !_collision[y, x] && (_objects[y, x] == null);
        }
        public bool FindResources(int y,int x)
        {

            return (_objects[y,x] != null ) && _objects[y, x].GameObjectType == GameObjectType.Resource;
        }
        public GameObject Find(Vector2Int skillPos)
        {
            //스킬을 쓰려고 하는데 누가 있는지 체크 
            if (skillPos.x < MinX || skillPos.x > MaxX)//현재 좌표를 검증 
                return null;
            if (skillPos.y < MinY || skillPos.y > MaxY)//현재 좌표를 검증 
                return null;

            int x = skillPos.x - MinX;
            int y = MaxY - skillPos.y;

            return _objects[y, x];
            
        }
        public bool ApplyLeave(GameObject go, bool applyCollision = true)
        {

            //if (go.Room == null)
            //    return false;
            //if (go.Room.Map != this)
            //    return false;


            if (go.CellPos.x < MinX || go.CellPos.x > MaxX)//현재 좌표를 검증 
                return false;
            if (go.CellPos.y < MinY || go.CellPos.y > MaxY)//현재 좌표를 검증 
                return false;

            if(applyCollision)
            { 

                int x = go.CellPos.x - MinX;
                int y = MaxY - go.CellPos.y;
                _objects[y, x] = null;

            }


            //Zone
            GameRoom room = go.Room;
            Vector2Int cellPos = go.CellPos;
            room.GetZone(go.CellPos).Remove(go);


            return true;


        }
        public bool ApplyMove(GameObject go,Vector2Int dest, bool applyCollision = true)
        {

            if (go.Room == null)
                return false;
            //if (go.Room.Map != this)
            //    return false;


            if (go.CellPos.x < MinX || go.CellPos.x > MaxX)//현재 좌표를 검증 
                return false;
            if (go.CellPos.y < MinY || go.CellPos.y > MaxY)//현재 좌표를 검증 
                return false;

            

            //아이템은 갈수 있는걸로 설계! 
            if (applyCollision)
            {
                //원래 위치에서는 없애기!
                {

                    int x = go.CellPos.x - MinX;
                    int y = MaxY - go.CellPos.y;
                    _objects[y, x] = null;
                }


                {

                    int x = dest.x - MinX;//새로운 좌표
                    int y = MaxY - dest.y;
                    _objects[y, x] = go;
                }
            }


            //겜 오브제 타입 
            GameObjectType type = ObjectManager.GetObjTypeById(go.Info.Id);

            if(type == GameObjectType.Player)
            {
                Player p = go as Player;
                

                Zone now = p.Room.GetZone(p.CellPos);
                Zone after = p.Room.GetZone(dest);

                if (now != after)//존이동 
                {
                    now.Players.Remove(p);
                    after.Players.Add(p);
                }
            }
            else if(type == GameObjectType.Monster)
            {
                Monster m = go as Monster;

                Zone now = m.Room.GetZone(m.CellPos);
                Zone after = m.Room.GetZone(dest);
                if (now != after)//존이동 
                {
                    now.Monsters.Remove(m);
                    after.Monsters.Add(m);
                }

            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile p = go as Projectile;

                Zone now = p.Room.GetZone(p.CellPos);
                Zone after = p.Room.GetZone(dest);
                if (now != after)//존이동 
                {
                    now.Projectiles.Remove(p);
                    after.Projectiles.Add(p);
                }

            }
            else if (type == GameObjectType.Resource)
            {
                Resource r = go as Resource;

                Zone now = r.Room.GetZone(r.CellPos);
                Zone after = r.Room.GetZone(dest);
                if (now != after)//존이동 
                {
                    now.Resources.Remove(r);
                    after.Resources.Add(r);
                }

            }


            //실제 좌표 이동
            go.CellPos = new Vector2Int(dest.x, dest.y);
            return true;

        }
        public void LoadMap(int mapId, string mapPath = "../../../../Common/MapData") //맵의 파일 경로
        {
            

            string mapName = "Map_" + mapId.ToString("000");

            // Collision 관련 파일
            string text = File.ReadAllText($"{mapPath}/{mapName}.txt");
            StringReader reader = new StringReader(text);

            MinX = int.Parse(reader.ReadLine());
            MaxX = int.Parse(reader.ReadLine());
            MinY = int.Parse(reader.ReadLine());
            MaxY = int.Parse(reader.ReadLine());

            int xCount = MaxX - MinX + 1;
            int yCount = MaxY - MinY + 1;
            _collision = new bool[yCount, xCount];
            _objects = new GameObject[yCount, xCount];

            for (int y = 0; y < yCount; y++)
            {
                string line = reader.ReadLine();
                for (int x = 0; x < xCount; x++)
                {
                    _collision[y, x] = (line[x] == '1' ? true : false);
                }
            }
        }

 

        #region A* PathFinding

        // U D L R
        int[] _deltaY = new int[] { 1, -1, 0, 0 };
        int[] _deltaX = new int[] { 0, 0, -1, 1 };
        int[] _cost = new int[] { 10, 10, 10, 10 };

        public List<Vector2Int> FindPath(Vector2Int startCellPos, Vector2Int destCellPos, bool ignoreDestCollision = true)
        {
            List<Pos> path = new List<Pos>();

            // 점수 매기기
            // F = G + H
            // F = 최종 점수 (작을 수록 좋음, 경로에 따라 달라짐)
            // G = 시작점에서 해당 좌표까지 이동하는데 드는 비용 (작을 수록 좋음, 경로에 따라 달라짐)
            // H = 목적지에서 얼마나 가까운지 (작을 수록 좋음, 고정)

            // (y, x) 이미 방문했는지 여부 (방문 = closed 상태)
            HashSet<Pos> closedSet = new HashSet<Pos>();
            // (y, x) 가는 길을 한 번이라도 발견했는지
            // 발견X => MaxValue
            // 발견O => F = G + H
            Dictionary<Pos, int> open = new Dictionary<Pos, int>();

            Dictionary<Pos, Pos> parent = new Dictionary<Pos, Pos>();

            // 오픈리스트에 있는 정보들 중에서, 가장 좋은 후보를 빠르게 뽑아오기 위한 도구
            PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

            // world Pos -> map Pos  
            Pos pos = Cell2Pos(startCellPos);
            Pos dest = Cell2Pos(destCellPos);

            // 시작점 발견 (예약 진행)
            open.Add(pos, 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)));
            pq.Push(new PQNode() { F = 10 * (Math.Abs(dest.Y - pos.Y) + Math.Abs(dest.X - pos.X)), G = 0, Y = pos.Y, X = pos.X });
            parent.Add(pos, pos);

            while (pq.Count > 0)
            {
                // 제일 좋은 후보를 찾는다
                PQNode pqNode = pq.Pop();
                // 동일한 좌표를 여러 경로로 찾아서, 더 빠른 경로로 인해서 이미 방문(closed)된 경우 스킵
                Pos node = new Pos(pqNode.Y, pqNode.X);
                if (closedSet.Contains(node))
                    continue;

                // 방문한다
                closedSet.Add(node);
                // 목적지 도착했으면 바로 종료
                if (node == dest)
                    break;

                // 상하좌우 등 이동할 수 있는 좌표인지 확인해서 예약(open)한다
                for (int i = 0; i < _deltaY.Length; i++)
                {
                    Pos next = new Pos(node.Y + _deltaY[i], node.X + _deltaX[i]);

                    //시작점에서 너무 빙빙 돌아서 가면 스킵
                    if (Math.Abs(pos.X - next.X) + Math.Abs(pos.Y - next.Y) > 10)
                        continue;
                    // 유효 범위를 벗어났으면 스킵
                    // 벽으로 막혀서 갈 수 없으면 스킵
                    if (!ignoreDestCollision || next.Y != dest.Y || next.X != dest.X)
                    {
                        if (CanGo(Pos2Cell(next)) == false) //(벽) + (게임오브젝트) 검사 
                            continue;
                    }

                    // 이미 방문한 곳이면 스킵
                    if (closedSet.Contains(next))
                        continue;

                    // 비용 계산
                    int g = 0;// node.G + _cost[i];
                    int h = 10 * ((dest.Y - next.Y) * (dest.Y - next.Y) + (dest.X - next.X) * (dest.X - next.X));
                    // 다른 경로에서 더 빠른 길 이미 찾았으면 스킵
                    int value = 0;
                    if (open.TryGetValue(next, out value) == false)
                        value = int.MaxValue;



                    if (value < g + h)
                        continue;

                    // 예약 진행
                    if (open.TryAdd(next, g + h) == false)
                        open[next] = g + h;
                    pq.Push(new PQNode() { F = g + h, G = g, Y = next.Y, X = next.X });
                    if (parent.TryAdd(next, node) == false)
                        parent[next] = node;
                }
            }

            return CalcCellPathFromParent(parent, dest);
        }

        List<Vector2Int> CalcCellPathFromParent(Dictionary<Pos,Pos> parent, Pos dest)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

           
            if (parent.ContainsKey(dest) == false)
            {
                Pos bestFromDest = new Pos();
                int bestDist = int.MaxValue;
                //최대한 목적지에서 가까운 곳을 새로운 목적지로 함
                foreach(Pos _pos in parent.Keys)
                {
                    //계속 갱신함 ! 
                    int dist = Math.Abs(dest.X - _pos.X) + Math.Abs(dest.Y - _pos.Y);
                    if (dist < bestDist)
                    {

                        bestDist = dist;//가장 가까운 거리 
                        bestFromDest = _pos;
                        
                    }
                }
                //새로운 목적지 설정 
                dest = bestFromDest;

            }
            Pos pos = dest;
            //목적지까지 가는 경로가 없어서 목적지의 부모가 없음 
            while (parent[pos] != pos)
            {
                cells.Add(Pos2Cell(pos));
                pos = parent[pos];
                
            }
            cells.Add(Pos2Cell(pos));
            cells.Reverse();

            return cells;
        }

        Pos Cell2Pos(Vector2Int cell)
        {
            // CellPos -> ArrayPos
            return new Pos(MaxY - cell.y, cell.x - MinX);
        }

        Vector2Int Pos2Cell(Pos pos)
        {
            // ArrayPos -> CellPos
            return new Vector2Int(pos.X + MinX, MaxY - pos.Y);
        }

        #endregion
    }
}

