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
    private Label taskListTitle; // タスク一覧のタイトル
    private Label reqNameLabel, taskTitleLabel, taskMessageLabel, taskReqLabel;
    private Button acceptTaskButton, deliverTaskButton; // アクションボタン
    
    // 現在表示しているタスクのタブ状態
    private TaskState currentTaskTab = TaskState.New;
    // 現在詳細表示しているタスクのID
    private int currentDetailTaskId = -1;

    private Label fundsLabel, manpowerLabel;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += UpdateResourceUI;
        }

        if (ProvinceDataManager.Instance != null)
        {
            ProvinceDataManager.Instance.OnProvinceSelected += UpdateProvinceUI;
        }

        UpdateResourceUI();
    }

    void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceUI;
        }

        if (ProvinceDataManager.Instance != null)
        {
            ProvinceDataManager.Instance.OnProvinceSelected -= UpdateProvinceUI;
        }
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
        taskListTitle = root.Q<Label>("TaskListTitle");
        taskScrollView = root.Q<ScrollView>("TaskScrollView");

        // タブボタンの登録
        var tabNewTasksBtn = root.Q<Button>("TabNewTasksButton");
        if (tabNewTasksBtn != null) tabNewTasksBtn.clicked += () => SwitchTaskTab(TaskState.New);

        var tabAcceptedTasksBtn = root.Q<Button>("TabAcceptedTasksButton");
        if (tabAcceptedTasksBtn != null) tabAcceptedTasksBtn.clicked += () => SwitchTaskTab(TaskState.Accepted);

        var tabCompletedTasksBtn = root.Q<Button>("TabCompletedTasksButton");
        if (tabCompletedTasksBtn != null) tabCompletedTasksBtn.clicked += () => SwitchTaskTab(TaskState.Completed);

        // タスク詳細画面の要素
        taskDetailContainer = root.Q<VisualElement>("TaskDetailContainer");
        reqNameLabel = root.Q<Label>("RequesterName");
        taskTitleLabel = root.Q<Label>("TaskTitle");
        taskMessageLabel = root.Q<Label>("TaskMessage");
        taskReqLabel = root.Q<Label>("TaskRequirement");

        acceptTaskButton = root.Q<Button>("AcceptTaskButton");
        if (acceptTaskButton != null) acceptTaskButton.clicked += OnAcceptTaskClicked;

        deliverTaskButton = root.Q<Button>("DeliverTaskButton");
        if (deliverTaskButton != null) deliverTaskButton.clicked += OnDeliverTaskClicked;

        // ボタンの登録
        var openBtn = root.Q<Button>("OpenTaskListButton");
        if (openBtn != null) openBtn.clicked += ShowTaskList;

        var closeBtn = root.Q<Button>("CloseTaskListButton");
        if (closeBtn != null) closeBtn.clicked += () => taskListContainer.style.display = DisplayStyle.None;

        var backBtn = root.Q<Button>("BackToListButton");
        if (backBtn != null) backBtn.clicked += () =>
        {
            taskDetailContainer.style.display = DisplayStyle.None;
            ShowTaskList(); // 一覧に戻る時に再描画する
        };


        // OnEnable内に追加
        fundsLabel = root.Q<Label>("FundsLabel");
        manpowerLabel = root.Q<Label>("ManpowerLabel");
        UpdateResourceUI(); // 初回表示

    }

    private void SwitchTaskTab(TaskState state)
    {
        currentTaskTab = state;
        if (taskListTitle != null)
        {
            if (state == TaskState.New) taskListTitle.text = "新規タスク一覧";
            else if (state == TaskState.Accepted) taskListTitle.text = "受託済タスク一覧";
            else if (state == TaskState.Completed) taskListTitle.text = "完了済タスク一覧";
        }
        ShowTaskList();
    }

    public void ShowTaskList()
    {
        if (taskScrollView == null) return;
        taskScrollView.Clear();

        List<TaskData> activeTasks = TaskDataManager.Instance.GetTasksByState(currentTaskTab);
        foreach (var task in activeTasks)
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
            currentDetailTaskId = taskId;
            reqNameLabel.text = data.requesterName;
            taskTitleLabel.text = data.title;
            taskMessageLabel.text = data.message;
            taskReqLabel.text = data.requirement;

            // ボタンの表示状態をステータスによって切り替え
            if (acceptTaskButton != null)
                acceptTaskButton.style.display = data.state == TaskState.New ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (deliverTaskButton != null)
                deliverTaskButton.style.display = data.state == TaskState.Accepted ? DisplayStyle.Flex : DisplayStyle.None;

            taskListContainer.style.display = DisplayStyle.None;
            taskDetailContainer.style.display = DisplayStyle.Flex;
        }
    }

    private void OnAcceptTaskClicked()
    {
        if (currentDetailTaskId != -1)
        {
            TaskDataManager.Instance.AcceptTask(currentDetailTaskId);
            taskDetailContainer.style.display = DisplayStyle.None;
            
            // 受託後は受託済タブに切り替えて一覧を表示
            SwitchTaskTab(TaskState.Accepted);
        }
    }

    private void OnDeliverTaskClicked()
    {
        if (currentDetailTaskId != -1)
        {
            TaskDataManager.Instance.DeliverTask(currentDetailTaskId);
            taskDetailContainer.style.display = DisplayStyle.None;
            
            // 納品後はそのまま一覧（受託済タブ）に戻る
            ShowTaskList();
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
        if (ResourceManager.Instance == null) return;

        var res = ResourceManager.Instance.GetData();
        if (res == null) return;

        if (fundsLabel != null) fundsLabel.text = $"{res.funds:N0}";
        if (manpowerLabel != null) manpowerLabel.text = $"{res.manpower:N0}";
    }
}