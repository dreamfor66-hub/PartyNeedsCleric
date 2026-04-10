using System.Linq;
using UnityEngine;

public class RewardManager
{
    readonly StageData stageData;

    public RewardManager(StageData stageData)
    {
        this.stageData = stageData;
    }

    public void GrantReward(GeneratedRoom room)
    {
        var rewardProfile = stageData.rewardProfiles.FirstOrDefault(x => x != null && x.roomType == room.roomType);
        if (rewardProfile == null)
        {
            Debug.Log($"[Reward] {room.floorNumber}F {room.roomType}: reward profile not configured.");
            return;
        }

        Debug.Log($"[Reward] {room.floorNumber}F {room.roomType}: candidates={rewardProfile.candidateCount}, guaranteed={rewardProfile.guaranteedReward}");
    }
}
