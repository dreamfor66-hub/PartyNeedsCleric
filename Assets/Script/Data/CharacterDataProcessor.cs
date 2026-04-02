using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CharacterDataProcessor : OdinPropertyProcessor<CharacterData>
{
    public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos)
    {
        var target = (CharacterData)this.Property.ValueEntry.WeakSmartValue;
        if (target == null) return;

        // Commander가 아닌 경우 처리할 필요 없음
        if (target.characterType != CharacterType.Commander)
            return;

        // Commander일 때 숨기고 싶은 필드 목록
        string[] hideList = new string[]
        {
            "sprites",
            "rangeRadius",
            "radius",
            "mass",
            "moveSpeed",
            "baseHp",
            "baseHitData",
            "baseBulletData",
            "bulletCooldown",
        };

        // 목록에 있는 필드 제거
        propertyInfos.RemoveAll(p => System.Array.Exists(hideList, h => p.PropertyName == h));
    }
}
