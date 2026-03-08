using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildingData
{
    public string id;           // 建物ID（例: "farm_1"）
    public string name;         // 表示名（例: "農地"）
    public string description;  // 施設の説明
    public long cost;           // 建設費用
    public int constructionDays;// 建設にかかる日数 (1ターン=1日)

    // 建設後の恒常的な効果（必要に応じて拡張）
    public long incomeBonus;    // ターンごとの収入増加など
    public int populationBonus; // 人口増加ボーナスなど
}

[System.Serializable]
public class BuildingDataList
{
    public BuildingData[] buildings;
}

public class BuildingDataManager : MonoBehaviour
{
    public static BuildingDataManager Instance;
    public TextAsset jsonFile;

    private Dictionary<string, BuildingData> buildingDict = new Dictionary<string, BuildingData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadData()
    {
        if (jsonFile != null)
        {
            BuildingDataList dataList = JsonUtility.FromJson<BuildingDataList>(jsonFile.text);
            foreach (BuildingData data in dataList.buildings)
            {
                if (!buildingDict.ContainsKey(data.id))
                {
                    buildingDict.Add(data.id, data);
                }
            }
            Debug.Log($"[BuildingDataManager] {buildingDict.Count}個の建物データを読み込みました。");
        }
    }

    public BuildingData GetBuildingData(string id)
    {
        if (buildingDict.ContainsKey(id))
        {
            return buildingDict[id];
        }
        return null;
    }

    public List<BuildingData> GetAllBuildings()
    {
        return new List<BuildingData>(buildingDict.Values);
    }
}
