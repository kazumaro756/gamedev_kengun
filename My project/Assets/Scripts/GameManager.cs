using UnityEngine;

// ゲーム全体のルールや、Manager間の連携を取り持つクラス
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // イベントの購読（タスクが納品された時の報酬処理など）
        if (TaskDataManager.Instance != null)
        {
            TaskDataManager.Instance.OnTaskDelivered += HandleTaskDelivered;
        }
    }

    void OnDestroy()
    {
        // イベントの解除
        if (TaskDataManager.Instance != null)
        {
            TaskDataManager.Instance.OnTaskDelivered -= HandleTaskDelivered;
        }
    }

    // タスク納品時に呼ばれる処理
    private void HandleTaskDelivered(int taskId)
    {
        // ゆくゆくはタスクデータ（JSON）自体に報酬の情報を持たせて、それを受け取る形にするのが理想です。
        // （例: TaskDataManager.Instance.GetTaskById(taskId).rewardFunds 等）
        // 今回は一時的なロジックとして、固定で以下のようにしています。
        if (ResourceManager.Instance != null)
        {
            Debug.Log($"[GameManager] タスク[{taskId}]の報酬を付与します。");
            ResourceManager.Instance.AddResources(1000, -100);
        }
    }

    // --- ターン（1日）経過処理 ---
    public void ProcessNextDay()
    {
        // 1. 日付を進める
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.AdvanceDay();
        }

        // 2. 建設を進める
        if (ConstructionManager.Instance != null)
        {
            ConstructionManager.Instance.ProcessConstructionTurn();
        }

        // 3. 資源・人口の効果を計算する（毎日は細かすぎるかもしれないが、とりあえず毎日加算仕様とする）
        if (ProvinceDataManager.Instance != null && BuildingDataManager.Instance != null && ResourceManager.Instance != null)
        {
            long totalIncome = 0;
            int totalPopGrowth = 0;

            foreach (var data in ProvinceDataManager.Instance.GetAllProvinces())
            {
                foreach (var slot in data.slots)
                {
                    // 完成している建物のみ効果を発揮
                    if (slot.isConstructed)
                    {
                        var building = BuildingDataManager.Instance.GetBuildingData(slot.buildingId);
                        if (building != null)
                        {
                            totalIncome += building.incomeBonus;
                            totalPopGrowth += building.populationBonus;
                        }
                    }
                }
                
                // 人口増加を適用
                if (totalPopGrowth > 0)
                {
                    data.population += totalPopGrowth;
                }
                
                // 建設進行や人口増加の結果をUIに反映するため、変更の有無に関わらず通知を飛ばす
                ProvinceDataManager.Instance.NotifyProvinceUpdated(data);
            }

            if (totalIncome > 0)
            {
                ResourceManager.Instance.AddResources(totalIncome, 0);
            }
        }
    }
}
