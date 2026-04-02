using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayableDataTable", menuName = "DataTable/PlayableDataTable")]
public class PlayableDataTable : ScriptableObject
{
    [Header("지휘관")]
    public List<CharacterData> commanders = new List<CharacterData>();

    [Header("동료")]
    public List<CharacterData> characters = new List<CharacterData>();

    [Header("스킬")]
    public List<SkillData> skills = new List<SkillData>();

    [Header("동료 전용 스킬 풀 - Active(Manual/Auto)")]
    public List<SkillData> companionActiveSkills = new List<SkillData>();

    [Header("동료 전용 스킬 풀 - Passive")]
    public List<SkillData> companionPassiveSkills = new List<SkillData>();

    [Header("동료 전용 장비 풀")]
    public List<EquipmentData> companionEquipmentPool = new List<EquipmentData>();
}
