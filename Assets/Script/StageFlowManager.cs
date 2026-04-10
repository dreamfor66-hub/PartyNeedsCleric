using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageFlowManager
{
    readonly StageData stageData;
    readonly Queue<StageRoomType> recentRouteHistory = new();
    readonly List<StageMapNode> visitedNodes = new();
    StageMap stageMap;
    StageMapNode currentNode;
    int nextNodeId;

    public StageFlowManager(StageData stageData)
    {
        this.stageData = stageData;
        this.stageData.EnsureProceduralDefaults();
    }

    public int GetTotalFloorCount()
    {
        return Mathf.Max(1, stageData.runConfig.totalFloors);
    }

    public StageMap GetOrCreateMap()
    {
        if (stageMap == null)
            stageMap = BuildMap();
        return stageMap;
    }

    public StageMapNode GetCurrentNode()
    {
        return currentNode;
    }

    public IReadOnlyList<StageMapNode> GetVisitedNodes()
    {
        return visitedNodes;
    }

    public IReadOnlyList<StageMapNode> GetReachableNodes()
    {
        if (stageMap == null)
            stageMap = BuildMap();

        if (currentNode == null)
            return stageMap.startNodes;

        return currentNode.nextNodes;
    }

    public bool CanSelectNode(StageMapNode node)
    {
        if (node == null)
            return false;

        var reachable = GetReachableNodes();
        return reachable.Contains(node);
    }

    public bool SelectNode(StageMapNode node)
    {
        if (!CanSelectNode(node))
            return false;

        currentNode = node;
        if (!visitedNodes.Contains(node))
            visitedNodes.Add(node);
        RecordRouteExposure(node.roomType);
        return true;
    }

    public bool HasReachedBoss()
    {
        return currentNode != null && currentNode.IsBoss;
    }

    public bool HasAnyReachableNode()
    {
        return GetReachableNodes().Count > 0;
    }

    StageMap BuildMap()
    {
        nextNodeId = 1;
        var map = new StageMap();
        int totalFloors = GetTotalFloorCount();
        int width = Mathf.Max(3, stageData.runConfig.mapWidth);
        int pathCount = Mathf.Max(stageData.runConfig.startNodeCount, stageData.runConfig.pathCount);
        int startNodeCount = Mathf.Clamp(stageData.runConfig.startNodeCount, 1, width);
        int maxNodesPerFloor = Mathf.Clamp(stageData.runConfig.maxNodesPerFloor, 1, width);

        var usedStarts = PickDistinctColumns(startNodeCount, width);
        var pathStarts = new List<int>(pathCount);
        for (int i = 0; i < pathCount; i++)
        {
            if (i < usedStarts.Count)
                pathStarts.Add(usedStarts[i]);
            else
                pathStarts.Add(usedStarts[Random.Range(0, usedStarts.Count)]);
        }

        var nodeGrid = new Dictionary<(int floor, int column), StageMapNode>();
        map.entryNode = new StageMapNode
        {
            id = nextNodeId++,
            floorNumber = 0,
            column = width / 2,
            roomType = StageRoomType.NormalCombat,
            isEntry = true,
            uiPosition = new Vector2(0.5f, 0f)
        };
        map.nodes.Add(map.entryNode);

        for (int i = 0; i < pathStarts.Count; i++)
        {
            int currentColumn = pathStarts[i];
            StageMapNode previous = map.entryNode;

            for (int floor = 1; floor <= totalFloors; floor++)
            {
                if (floor == totalFloors)
                    currentColumn = Mathf.Clamp(width / 2, 0, width - 1);

                int floorNodeLimit = GetFloorNodeLimit(floor, totalFloors, startNodeCount, maxNodesPerFloor);
                var node = GetOrCreateNode(map, nodeGrid, floor, currentColumn, width, totalFloors, floorNodeLimit);
                if (previous != null)
                    Connect(previous, node);
                previous = node;

                if (floor < totalFloors - 1)
                    currentColumn = GetNextColumn(currentColumn, width);
            }
        }

        map.startNodes.AddRange(map.GetNodesOnFloor(1).OrderBy(x => x.column));
        map.bossNode = map.GetNodesOnFloor(totalFloors).FirstOrDefault(x => x.IsBoss);
        currentNode = null;
        visitedNodes.Clear();
        return map;
    }

    int GetFloorNodeLimit(int floor, int totalFloors, int startNodeCount, int maxNodesPerFloor)
    {
        if (floor <= 1)
            return startNodeCount;
        if (floor >= totalFloors)
            return 1;
        return maxNodesPerFloor;
    }

    StageMapNode GetOrCreateNode(StageMap map, Dictionary<(int floor, int column), StageMapNode> nodeGrid, int floor, int column, int width, int totalFloors, int floorNodeLimit)
    {
        var key = (floor, column);
        if (nodeGrid.TryGetValue(key, out var existing))
            return existing;

        var floorNodes = map.GetNodesOnFloor(floor).OrderBy(x => x.column).ToList();
        if (floorNodes.Count >= floorNodeLimit)
            return GetClosestNode(floorNodes, column);

        var node = new StageMapNode
        {
            id = nextNodeId++,
            floorNumber = floor,
            column = column,
            roomType = ResolveRoomTypeForFloor(floor, totalFloors),
            uiPosition = new Vector2(
                floor >= totalFloors ? 0.5f : CalculateXPosition(column, width),
                CalculateYPosition(floor, totalFloors))
        };

        nodeGrid[key] = node;
        map.nodes.Add(node);
        return node;
    }

    static StageMapNode GetClosestNode(List<StageMapNode> floorNodes, int targetColumn)
    {
        StageMapNode best = floorNodes[0];
        int bestDistance = Mathf.Abs(best.column - targetColumn);

        for (int i = 1; i < floorNodes.Count; i++)
        {
            int distance = Mathf.Abs(floorNodes[i].column - targetColumn);
            if (distance < bestDistance)
            {
                best = floorNodes[i];
                bestDistance = distance;
            }
        }

        return best;
    }

    StageRoomType ResolveRoomTypeForFloor(int floorNumber, int totalFloors)
    {
        if (floorNumber >= totalFloors)
            return StageRoomType.Boss;

        return PickWeightedRoomType(floorNumber);
    }

    int GetNextColumn(int currentColumn, int width)
    {
        int directionRoll = Random.Range(0, 3) - 1;
        return Mathf.Clamp(currentColumn + directionRoll, 0, width - 1);
    }

    List<int> PickDistinctColumns(int count, int width)
    {
        var pool = Enumerable.Range(0, width).ToList();
        var result = new List<int>(count);
        while (result.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        result.Sort();
        return result;
    }

    static void Connect(StageMapNode from, StageMapNode to)
    {
        if (!from.nextNodes.Contains(to))
            from.nextNodes.Add(to);
        if (!to.previousNodes.Contains(from))
            to.previousNodes.Add(from);
    }

    static float CalculateXPosition(int column, int width)
    {
        return width <= 1 ? 0.5f : (float)column / (width - 1);
    }

    static float CalculateYPosition(int floorNumber, int totalFloors)
    {
        return totalFloors <= 1 ? 0f : (float)(floorNumber - 1) / (totalFloors - 1);
    }

    public void RecordRouteExposure(StageRoomType roomType)
    {
        recentRouteHistory.Enqueue(roomType);
        while (recentRouteHistory.Count > Mathf.Max(1, stageData.runConfig.recentRouteHistorySize))
            recentRouteHistory.Dequeue();
    }

    StageRoomType PickWeightedRoomType(int floorNumber)
    {
        var weightedChoices = GetAvailableWeightedChoices(floorNumber);
        if (weightedChoices.Count == 0)
            return StageRoomType.NormalCombat;

        return DrawRoomType(weightedChoices);
    }

    Dictionary<StageRoomType, float> GetAvailableWeightedChoices(int floorNumber)
    {
        var weightData = stageData.roomTypeWeights
            .FirstOrDefault(x => x != null && floorNumber >= x.floorMin && floorNumber <= x.floorMax);

        var result = new Dictionary<StageRoomType, float>();
        var allowedRoomTypes = new[]
        {
            StageRoomType.NormalCombat,
            StageRoomType.Elite,
            StageRoomType.Recovery,
            StageRoomType.SkillReward,
            StageRoomType.UpgradeReward,
            StageRoomType.RelicReward
        };

        foreach (var roomType in allowedRoomTypes)
        {
            float weight = weightData != null ? weightData.GetWeight(roomType) : 0f;
            if (weight <= 0f)
                continue;

            int repeatCount = recentRouteHistory.Count(x => x == roomType);
            float penalty = Mathf.Pow(1f - stageData.runConfig.repeatedRoutePenalty, repeatCount);
            result[roomType] = Mathf.Max(0.01f, weight * penalty);
        }

        if (result.Count == 0)
            result[StageRoomType.NormalCombat] = 1f;

        return result;
    }

    static StageRoomType DrawRoomType(Dictionary<StageRoomType, float> weightedChoices)
    {
        float total = weightedChoices.Values.Sum();
        float roll = Random.value * total;

        foreach (var pair in weightedChoices)
        {
            roll -= pair.Value;
            if (roll <= 0f)
                return pair.Key;
        }

        return weightedChoices.Keys.Last();
    }
}
