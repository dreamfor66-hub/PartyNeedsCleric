using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomGenerator
{
    readonly StageData stageData;
    readonly EncounterGenerator encounterGenerator;

    public RoomGenerator(StageData stageData, EncounterGenerator encounterGenerator)
    {
        this.stageData = stageData;
        this.encounterGenerator = encounterGenerator;
    }

    public GeneratedRoom GenerateRoom(int floorNumber, StageRoomType roomType)
    {
        var template = SelectTemplate(roomType);
        var room = new GeneratedRoom
        {
            floorNumber = floorNumber,
            roomType = roomType,
            template = template,
            runtimeMap = CreateRuntimeMap(template)
        };

        if (IsCombatRoom(roomType))
            encounterGenerator.PopulateEncounter(room);

        return room;
    }

    public static bool IsCombatRoom(StageRoomType roomType)
    {
        return roomType == StageRoomType.NormalCombat
            || roomType == StageRoomType.Elite
            || roomType == StageRoomType.Boss;
    }

    RoomTemplateData SelectTemplate(StageRoomType roomType)
    {
        IEnumerable<RoomTemplateData> candidates = stageData.roomTemplates.Where(x => x != null);
        if (roomType == StageRoomType.Boss)
            candidates = candidates.Where(x => x.canUseForBoss);
        else
            candidates = candidates.Where(x => !x.canUseForBoss || x.templateType != RoomTemplateType.BossArena);

        var list = candidates.ToList();
        if (list.Count == 0)
            list = stageData.roomTemplates.Where(x => x != null).ToList();

        return list.Count > 0
            ? list[Random.Range(0, list.Count)]
            : RoomTemplateData.FromMapData(null, roomType == StageRoomType.Boss ? RoomTemplateType.BossArena : RoomTemplateType.SmallCircle, roomType == StageRoomType.Boss);
    }

    MapData CreateRuntimeMap(RoomTemplateData template)
    {
        var runtimeMap = ScriptableObject.CreateInstance<MapData>();
        runtimeMap.name = $"RuntimeMap_{template.templateId}";
        runtimeMap.size = template.mapSize;
        runtimeMap.playerStartPoints = template.playerStartPoints != null && template.playerStartPoints.Length > 0
            ? (Vector2[])template.playerStartPoints.Clone()
            : new[] { new Vector2(-48f, -180f), new Vector2(48f, -180f), new Vector2(0f, -224f) };
        runtimeMap.collisions = template.fixedObstacles != null ? new List<Vector2>(template.fixedObstacles) : new List<Vector2>();
        runtimeMap.useOuterBoundaryWalls = true;
        runtimeMap.waves = new List<WaveData>();
        return runtimeMap;
    }
}
