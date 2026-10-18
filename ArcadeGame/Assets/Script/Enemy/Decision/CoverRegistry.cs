using System;
using System.Collections.Generic;
using UnityEngine;

public static class CoverRegistry
{
    private const float CellSize = 10f;
    private const int InitialCellCapacity = 8;

    private static readonly Dictionary<Vector2Int, List<CoverPoint>> Cells = new Dictionary<Vector2Int, List<CoverPoint>>();

    //해제할 때는 현재 위치가 아니라 등록했던 구역을 사용
    private static readonly Dictionary<CoverPoint, Vector2Int> Membership = new Dictionary<CoverPoint, Vector2Int>();

    public static int Count => Membership.Count;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        Cells.Clear();
        Membership.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterLoaedPoints()
    {
        //시작 시 한 번만 실행
        CoverPoint[] points = UnityEngine.Object.FindObjectsByType<CoverPoint>(FindObjectsSortMode.None);
        for(int i = 0; i < points.Length; i++)
        {
            CoverPoint point = points[i];

            if(point == null || !point.isActiveAndEnabled) continue;

            point.ResetReservationForSession();
            Register(point);
        }
    }
    public static void Register(CoverPoint point)
    {
        if(point == null || !point.isActiveAndEnabled) return;
        Vector2Int cell = GetCell(point.Position);
        if(Membership.TryGetValue(point, out Vector2Int previousCell))
        {
            if(previousCell == cell) 
                return;
            Unregister(point);

        }
        if(!Cells.TryGetValue(cell, out List<CoverPoint> points))
        {
            points = new List<CoverPoint>(InitialCellCapacity);
            Cells.Add(cell, points);
        }
        points.Add(point);
        Membership.Add(point, cell);
    }
    
    public static void Unregister(CoverPoint point)
    {
        if(point == null || !Membership.TryGetValue(point, out Vector2Int cell))
            return;
        if(Cells.TryGetValue(cell, out List<CoverPoint> points))
        {
            points.Remove(point);
            if(points.Count == 0)
                Cells.Remove(cell);
        }
        Membership.Remove(point);
    }
    
    public static int QueryNearbyNonAlloc(Vector3 origin, float radius, EnemyController requester, CoverPoint[] results, float[] distancesSquared, out bool truncated)
    {
        truncated = false;
        if(results == null || results.Length == 0) 
            return 0;
        
        //이전 검색 결과의 참조를 제거
        Array.Clear(results, 0, results.Length);

        if(distancesSquared == null || distancesSquared.Length < results.Length || requester == null || radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            return 0;
        int minX = Mathf.FloorToInt((origin.x - radius) / CellSize);
        int maxX = Mathf.FloorToInt((origin.x + radius) / CellSize);
        int minZ = Mathf.FloorToInt((origin.z - radius) / CellSize);
        int maxZ = Mathf.FloorToInt((origin.z + radius) / CellSize);

        float radiusSquared = radius * radius;
        int count = 0;
        for(int x = minX; x <= maxX; x++)
        {
            for(int z = minZ; z <= maxZ; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if(!Cells.TryGetValue(cell, out List<CoverPoint> points)) 
                    continue;
                
                for(int i = 0; i < points.Count; i++)
                {
                    CoverPoint point = points[i];

                    if(point == null || !point.IsAvailableFor(requester))
                        continue;
                    
                    float distanceSquared = (point.Position - origin).sqrMagnitude;

                    if(distanceSquared > radiusSquared)
                        continue;
                    if(count == results.Length)
                    {
                        truncated = true;

                        // 가장 먼 후보보다 멀면 제외
                        if(distanceSquared >= distancesSquared[count - 1])
                            continue;
                    }
                    //가까운 순서로 삽입
                    int insertIndex = Mathf.Min(count, results.Length - 1);

                    while(insertIndex > 0 && distancesSquared[insertIndex - 1] > distanceSquared)
                    {
                        results[insertIndex] = results[insertIndex - 1];

                        distancesSquared[insertIndex] = distancesSquared[insertIndex - 1];
                        insertIndex--;
                    }
                    results[insertIndex] = point;
                    distancesSquared[insertIndex] = distanceSquared;

                    if(count < results.Length)
                        count++;
                }
            }
        }
        return count;    
    }
    private static Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / CellSize),Mathf.FloorToInt(position.z / CellSize));
    }
}
