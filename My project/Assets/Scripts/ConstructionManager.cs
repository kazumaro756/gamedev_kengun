using UnityEngine;

public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 建設を実行する
    public void Build(string stateKey, string buildingId)
    {
        ProvinceData province = ProvinceDataManager.Instance.GetProvince(stateKey);
        if (province == null)
        {
            Debug.LogError($"[Construction] 無効な州キー: {stateKey}");
            return;
        }

        BuildingData building = BuildingDataManager.Instance.GetBuildingData(buildingId);
        if (building == null)
        {
            Debug.LogError($"[Construction] 無効な建物ID: {buildingId}");
            return;
        }

        // スロット空きチェック
        if (province.slots.Count >= province.maxSlots)
        {
            Debug.LogWarning($"[Construction] 州 {province.stateName} には空きスロットがありません。");
            return;
        }

        // 資金チェック
        if (ResourceManager.Instance.GetData().funds < building.cost)
        {
            Debug.LogWarning($"[Construction] 資金が足りません。必要: {building.cost}");
            return;
        }

        // 資金を消費して建設を実行
        ResourceManager.Instance.AddResources(-building.cost, 0);

        // 州に建設予定建物を追加
        BuildingSlot newSlot = new BuildingSlot
        {
            buildingId = building.id,
            isConstructed = false,
            remainingDays = building.constructionDays // JSONから読み込んだ日数
        };
        province.slots.Add(newSlot);

        // UIを更新
        ProvinceDataManager.Instance.NotifyProvinceUpdated(province);

        Debug.Log($"[Construction] {province.stateName} で {building.name} の建設を開始！ (完成まで {building.constructionDays} 日)");
    }

    // 1ターン（1日）経過したときの処理
    public void ProcessConstructionTurn()
    {
        // 全州のスロットを走査して建設日数を減らす
        foreach (var province in ProvinceDataManager.Instance.GetAllProvinces())
        {
            bool wasUpdated = false;

            // class なので直接書き換えても元のインスタンスが書き換わる
            for (int i = 0; i < province.slots.Count; i++)
            {
                var slot = province.slots[i];
                if (!slot.isConstructed && slot.remainingDays > 0)
                {
                    slot.remainingDays--;
                    wasUpdated = true;

                    if (slot.remainingDays <= 0)
                    {
                        var bData = BuildingDataManager.Instance.GetBuildingData(slot.buildingId);
                        slot.isConstructed = true;
                        slot.remainingDays = 0;
                        Debug.Log($"[Construction] {province.stateName} で {bData?.name ?? "建物"} の建設が完了しました！");
                    }
                }
            }

            // 何らかの進捗があればUIを更新する
            if (wasUpdated)
            {
                ProvinceDataManager.Instance.NotifyProvinceUpdated(province);
            }
        }
    }
}
