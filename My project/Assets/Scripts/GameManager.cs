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
}
