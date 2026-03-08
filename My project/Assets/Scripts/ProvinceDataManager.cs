using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ProvinceData
{
    public string colorKey;
    public string stateName;
    public int population;
    public string owner;

    // --- 新規追加（内政用） ---
    public int maxSlots; // 最大建設スロット数
    public List<BuildingSlot> slots = new List<BuildingSlot>(); // 建設スロットリスト
}

[System.Serializable]
public class BuildingSlot
{
    public string buildingId;
    public bool isConstructed;   // 完成しているか
    public int remainingDays;    // 完成までの残り日数
}

[System.Serializable]
public class ProvinceDataList
{
    public ProvinceData[] states;
}

public class ProvinceDataManager : MonoBehaviour
{
    // どこからでも ProvinceDataManager.Instance でアクセスできるようにする魔法（シングルトン）
    public static ProvinceDataManager Instance;

    public TextAsset jsonFile;
    private Dictionary<string, ProvinceData> provinceDict = new Dictionary<string, ProvinceData>();

    public System.Action<ProvinceData> OnProvinceSelected;
    public System.Action<ProvinceData> OnProvinceUpdated; // データが更新された時のイベント

    void Awake()
    {
        // 自分が唯一のマネージャーであることを保証する
        if (Instance == null)
        {
            Instance = this;
            LoadData(); // 起動時に即座にデータを読み込む
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // JSONを読み込んで辞書を作る処理（元々GetColorOnClickにあったもの）
    private void LoadData()
    {
        if (jsonFile != null)
        {
            ProvinceDataList dataList = JsonUtility.FromJson<ProvinceDataList>(jsonFile.text);
            foreach (ProvinceData data in dataList.states)
            {
                if (!provinceDict.ContainsKey(data.colorKey))
                {
                    provinceDict.Add(data.colorKey, data);
                }
            }
            Debug.Log($"[DataManager] 合計 {provinceDict.Count} 個の州データを読み込みました。");
        }
        else
        {
            Debug.LogError("JSONファイルがセットされていません。");
        }
    }

    // 外部のスクリプトから色キーを渡されたら、該当する州データを返す関数
    public ProvinceData GetProvince(string colorKey)
    {
        if (provinceDict.ContainsKey(colorKey))
        {
            return provinceDict[colorKey];
        }
        return null; // 見つからなかったら空っぽを返す
    }

    public List<ProvinceData> GetAllProvinces()
    {
        return new List<ProvinceData>(provinceDict.Values);
    }

    // 州が選択されたときの処理（イベント発行）
    public void SelectProvince(string colorKey)
    {
        ProvinceData clickedProvince = GetProvince(colorKey);
        if (clickedProvince != null)
        {
            Debug.Log($"クリックされた州: {clickedProvince.stateName} / 人口: {clickedProvince.population} / 所有者: {clickedProvince.owner}");
            OnProvinceSelected?.Invoke(clickedProvince);
        }
        else
        {
            Debug.LogWarning($"未登録の色キーがクリックされました: {colorKey}");
        }
    }

    // 州のデータが更新された際（建設完了時など）に呼ぶ
    public void NotifyProvinceUpdated(ProvinceData province)
    {
        OnProvinceUpdated?.Invoke(province);
    }
}