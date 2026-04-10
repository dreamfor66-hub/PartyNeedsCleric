using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageFlowManager
{
    readonly StageData stageData;
    readonly Queue<StageRoomType> recentRouteHistory = new();

    public StageFlowManager(StageData stageData)
    {
        this.stageData = stageData;
        this.stageData.EnsureProceduralDefaults();
    }

    public bool IsBossFloor(int floorNumber)
    {
        return stageData.runConfig.bossFloorInterval > 0
            && floorNumber > 0
            && floorNumber % stageData.runConfig.bossFloorInterval == 0;
    }

    public int GetTotalFloorCount()
    {
        return Mathf.Max(1, stageData.runConfig.totalFloors);
    }

    public StageRoomType ResolveRoomType(int floorNumber, StageRoomType? selectedRouteType)
    {
        if (IsBossFloor(floorNumber))
            return StageRoomType.Boss;

        if (selectedRouteType.HasValue && selectedRouteType.Value != StageRoomType.Boss)
            return selectedRouteType.Value;

        return PickWeightedRoomType(floorNumber);
    }

    public List<StageRouteChoice> BuildNextRouteChoices(int nextFloorNumber)
    {
        var routes = new List<StageRouteChoice>();
        if (nextFloorNumber > GetTotalFloorCount())
            return routes;

        if (IsBossFloor(nextFloorNumber))
        {
            routes.Add(new StageRouteChoice { roomType = StageRoomType.Boss, targetFloor = nextFloorNumber });
            return routes;
        }

        int desiredCount = Mathf.Clamp(stageData.runConfig.routeChoiceCount, 2, 3);
        var pool = GetAvailableWeightedChoices(nextFloorNumber);
        while (routes.Count < desiredCount && pool.Count > 0)
        {
            StageRoomType roomType = DrawRoomType(pool);
            pool.Remove(roomType);
            routes.Add(new StageRouteChoice { roomType = roomType, targetFloor = nextFloorNumber });
        }

        return routes;
    }

    public void RecordRouteExposure(IEnumerable<StageRouteChoice> routes)
    {
        foreach (var route in routes)
            RecordRouteExposure(route.roomType);
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
