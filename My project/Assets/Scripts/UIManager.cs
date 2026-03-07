using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic; // 【重要】Listを使うために必要

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // GetColorOnClickが参照しているプロパティの修正
    // 「一覧」か「詳細」のどちらかが開いていればTrueを返すように変更
    public bool IsTaskPanelActive =>
        (taskListContainer != null && taskListContainer.resolvedStyle.display != DisplayStyle.None) ||
        (taskDetailContainer != null && taskDetailContainer.resolvedStyle.display != DisplayStyle.None);

    private Label stateNameLabel, populationLabel, ownerLabel;
    private VisualElement taskListContainer, taskDetailContainer;
    private ScrollView taskScrollView;
    private Label reqNameLabel, taskTitleLabel, taskMessageLabel, taskReqLabel;

    private Label fundsLabel, manpowerLabel;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 州情報
        stateNameLabel = root.Q<Label>("StateNameLabel");
        populationLabel = root.Q<Label>("PopulationLabel");
        ownerLabel = root.Q<Label>("OwnerLabel");

        // タスク一覧画面の要素
        taskListContainer = root.Q<VisualElement>("TaskListContainer");
        taskScrollView = root.Q<ScrollView>("TaskScrollView");

        // タスク詳細画面の要素
        taskDetailContainer = root.Q<VisualElement>("TaskDetailContainer");
        reqNameLabel = root.Q<Label>("RequesterName");
        taskTitleLabel = root.Q<Label>("TaskTitle");
        taskMessageLabel = root.Q<Label>("TaskMessage");
        taskReqLabel = root.Q<Label>("TaskRequirement");

        // ボタンの登録
        var openBtn = root.Q<Button>("OpenTaskListButton");
        if (openBtn != null) openBtn.clicked += ShowTaskList;

        var closeBtn = root.Q<Button>("CloseTaskListButton");
        if (closeBtn != null) closeBtn.clicked += () => taskListContainer.style.display = DisplayStyle.None;

        var backBtn = root.Q<Button>("BackToListButton");
        if (backBtn != null) backBtn.clicked += () =>
        {
            taskDetailContainer.style.display = DisplayStyle.None;
            taskListContainer.style.display = DisplayStyle.Flex;
        };


        // OnEnable内に追加
        fundsLabel = root.Q<Label>("FundsLabel");
        manpowerLabel = root.Q<Label>("ManpowerLabel");
        UpdateResourceUI(); // 初回表示

    }

    public void ShowTaskList()
    {
        if (taskScrollView == null) return;
        taskScrollView.Clear();

        List<TaskData> allTasks = TaskDataManager.Instance.GetAllTasks();
        foreach (var task in allTasks)
        {
            Button taskItem = new Button();
            taskItem.text = $"【{task.requesterName}】 {task.title}";
            taskItem.style.height = 40;
            taskItem.clicked += () => ShowTaskDetail(task.id);
            taskScrollView.Add(taskItem);
        }
        taskListContainer.style.display = DisplayStyle.Flex;
    }

    private void ShowTaskDetail(int taskId)
    {
        var data = TaskDataManager.Instance.GetTaskById(taskId);
        if (data != null)
        {
            reqNameLabel.text = data.requesterName;
            taskTitleLabel.text = data.title;
            taskMessageLabel.text = data.message;
            taskReqLabel.text = data.requirement;

            taskListContainer.style.display = DisplayStyle.None;
            taskDetailContainer.style.display = DisplayStyle.Flex;
        }
    }

    public void UpdateProvinceUI(ProvinceData data)
    {
        if (stateNameLabel == null) return;
        stateNameLabel.text = data?.stateName ?? "未選択";
        populationLabel.text = data != null ? $"人口: {data.population:N0}人" : "人口: --";
        ownerLabel.text = data != null ? $"所有者: {data.owner}" : "所有者: --";
    }


    public void UpdateResourceUI()
    {
        var res = ResourceManager.Instance.GetData();
        if (fundsLabel != null) fundsLabel.text = $"{res.funds:N0}";
        if (manpowerLabel != null) manpowerLabel.text = $"{res.manpower:N0}";
    }
}