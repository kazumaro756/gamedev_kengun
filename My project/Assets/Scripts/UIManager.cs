using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // 州情報表示用のラベル
    private Label stateNameLabel;
    private Label populationLabel;
    private Label ownerLabel;

    // タスク表示用の要素
    private VisualElement taskContainer;
    private Label reqNameLabel, taskTitleLabel, taskMessageLabel, taskReqLabel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        // 1. UI DocumentからUIのルート要素を一度だけ取得する
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // 2. 州情報表示用の要素を取得
        stateNameLabel = root.Q<Label>("StateNameLabel");
        populationLabel = root.Q<Label>("PopulationLabel");
        ownerLabel = root.Q<Label>("OwnerLabel");

        // 3. タスクパネル表示用の要素を取得（root変数を再利用）
        taskContainer = root.Q<VisualElement>("TaskContainer");
        reqNameLabel = root.Q<Label>("RequesterName");
        taskTitleLabel = root.Q<Label>("TaskTitle");
        taskMessageLabel = root.Q<Label>("TaskMessage");
        taskReqLabel = root.Q<Label>("TaskRequirement");

        // 4. ボタンのクリックイベント登録
        var closeButton = root.Q<Button>("CloseTaskButton");
        if (closeButton != null)
        {
            closeButton.clicked += () =>
            {
                if (taskContainer != null) taskContainer.style.display = DisplayStyle.None;
            };
        }
        // 【追加】テストボタンでID 1のタスクを表示
        var testBtn = root.Q<Button>("TaskTestButton");
        if (testBtn != null)
        {
            testBtn.clicked += () => ShowTask(1);
        }


    }

    // 州情報の更新
    public void UpdateProvinceUI(ProvinceData data)
    {
        if (stateNameLabel == null) return;

        if (data != null)
        {
            stateNameLabel.text = data.stateName;
            populationLabel.text = $"人口: {data.population:N0}人";
            ownerLabel.text = $"所有者: {data.owner}";
        }
        else
        {
            stateNameLabel.text = "未登録の領域";
            populationLabel.text = "人口: --";
            ownerLabel.text = "所有者: --";
        }
    }

    // タスクパネルの表示
    public void ShowTask(int taskId)
    {
        // 1. マネージャーがいるかチェック
        if (TaskDataManager.Instance == null)
        {
            Debug.LogError("TaskDataManagerがシーンに存在しません！");
            return;
        }

        var data = TaskDataManager.Instance.GetTaskById(taskId);

        // 2. 指定したIDのデータがあるか、UIパーツがちゃんと取得できているかチェック
        if (data != null && taskContainer != null && reqNameLabel != null)
        {
            reqNameLabel.text = data.requesterName;
            taskTitleLabel.text = data.title;
            taskMessageLabel.text = data.message;
            taskReqLabel.text = data.requirement;
            taskContainer.style.display = DisplayStyle.Flex;
        }
        else
        {
            Debug.LogWarning($"タスク表示に失敗しました。データ:{data != null}, UIパネル:{taskContainer != null}, ラベル:{reqNameLabel != null}");
        }
    }
}