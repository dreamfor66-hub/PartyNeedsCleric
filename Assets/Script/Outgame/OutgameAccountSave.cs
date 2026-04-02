using System;
using System.Collections.Generic;

[Serializable]
public class OutgameAccountSave
{
    public int accountLevel = 1;
    // commander
    public string currentCommanderId;
    public List<CommanderSkillPresetSave> commanderPresets = new List<CommanderSkillPresetSave>();

    // equipment (instance-based)
    public List<Equipment> ownedEquipments = new List<Equipment>(); // 중복 허용
    public Equipment[] equippedEquipment = new Equipment[6];  // slot -> Equipment.instanceId

    public List<Character> ownedCharacters = new List<Character>(); // 중복 허용(원하면)
    public Character[] equippedCharacters = new Character[3];       // party slots (0~2)
}

[Serializable]
public class CommanderSkillPresetSave
{
    public string commanderId;
    public string[] skillIds = new string[6];
}
