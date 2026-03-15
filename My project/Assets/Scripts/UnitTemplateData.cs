using System.Collections.Generic;

[System.Serializable]
public class UnitTemplateRequirement
{
    public UnitType requiredType; // 何の兵科が必要か（例：InfantryCompany）
    public int coreCount;         // 必須枠（コアスロット）の数
    public int maxSupportCount;   // 任意枠（サポートスロット）の数
}

[System.Serializable]
public class UnitTemplateConfig
{
    public string templateId;     // テンプレートID (例: "TPL_INF_BAT")
    public string templateName;   // テンプレート名 (例: "歩兵大隊")
    public UnitType targetUnitType; // これを組んだ時に成れる部隊タイプ (例: Battalion)
    public List<UnitTemplateRequirement> requirements; // 要求リスト
}

[System.Serializable]
public class UnitTemplateConfigList
{
    public List<UnitTemplateConfig> templates;
}
