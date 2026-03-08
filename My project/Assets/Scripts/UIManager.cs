using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic; // 【重要】Listを使うために必要

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // GetColorOnClickが参照しているプロパティの修正
    // 「一覧」か「詳細」か「州管理」のどれかが開いていればTrueを返すように変更
    public bool IsTaskPanelActive =>
        (taskListContainer != null && taskListContainer.resolvedStyle.display != DisplayStyle.None) ||
        (taskDetailContainer != null && taskDetailContainer.resolvedStyle.display != DisplayStyle.None) ||
        (provinceMgmtContainer != null && provinceMgmtContainer.resolvedStyle.display != DisplayStyle.None);

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

    // --- 州管理パネル・建設用 ---
    private VisualElement provinceMgmtContainer;
    private Label mgmtStateNameLabel, mgmtInfoLabel;
    private VisualElement buildingSlotsContainer;
    private VisualElement constructionPopup;
    private ScrollView buildingListScrollView;
    private string selectedProvinceKeyForBuilding; // 建設対象の州キー

    private Label fundsLabel, manpowerLabel;
    
    // --- 時間・ターン制御 ---
    private Label dateLabel;
    private Button nextDayButton;

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
            ProvinceDataManager.Instance.OnProvinceSelected += OpenProvinceMgmtPanel;
            ProvinceDataManager.Instance.OnProvinceUpdated += UpdateProvinceMgmtPanel; // 建設時の更新用
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDateChanged += UpdateDateUI;
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
            ProvinceDataManager.Instance.OnProvinceUpdated -= UpdateProvinceMgmtPanel;
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDateChanged -= UpdateDateUI;
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

        // --- 州管理パネルの要素 ---
        provinceMgmtContainer = root.Q<VisualElement>("ProvinceManagementContainer");
        mgmtStateNameLabel = root.Q<Label>("MgmtStateNameLabel");
        mgmtInfoLabel = root.Q<Label>("MgmtInfoLabel");
        buildingSlotsContainer = root.Q<VisualElement>("BuildingSlotsContainer");
        constructionPopup = root.Q<VisualElement>("ConstructionPopup");
        buildingListScrollView = root.Q<ScrollView>("BuildingListScrollView");

        var closeMgmtBtn = root.Q<Button>("CloseMgmtButton");
        if (closeMgmtBtn != null) closeMgmtBtn.clicked += () => provinceMgmtContainer.style.display = DisplayStyle.None;

        var closePopupBtn = root.Q<Button>("ClosePopupButton");
        if (closePopupBtn != null) closePopupBtn.clicked += () => constructionPopup.style.display = DisplayStyle.None;
        
        // OnEnable内に追加
        fundsLabel = root.Q<Label>("FundsLabel");
        manpowerLabel = root.Q<Label>("ManpowerLabel");
        
        // --- トップバー（日付・ターン送り） ---
        dateLabel = root.Q<Label>("DateLabel");
        nextDayButton = root.Q<Button>("NextDayButton");
        
        if (nextDayButton != null)
        {
            nextDayButton.clicked += () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ProcessNextDay();
            };
        }

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
        
        // 他のパネルを閉じる
        if (provinceMgmtContainer != null) provinceMgmtContainer.style.display = DisplayStyle.None;
        if (taskDetailContainer != null) taskDetailContainer.style.display = DisplayStyle.None;

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

    // --- 州管理パネル関係 ---

    public void OpenProvinceMgmtPanel(ProvinceData data)
    {
        if (data == null || provinceMgmtContainer == null) return;
        
        // 他のパネルを閉じる
        if (taskListContainer != null) taskListContainer.style.display = DisplayStyle.None;
        if (taskDetailContainer != null) taskDetailContainer.style.display = DisplayStyle.None;

        selectedProvinceKeyForBuilding = data.colorKey;
        provinceMgmtContainer.style.display = DisplayStyle.Flex;
        UpdateProvinceMgmtPanel(data);
    }

    private void UpdateProvinceMgmtPanel(ProvinceData data)
    {
        if (data == null || data.colorKey != selectedProvinceKeyForBuilding) return;

        if (mgmtStateNameLabel != null) mgmtStateNameLabel.text = $"州管理パネル：{data.stateName}";
        if (mgmtInfoLabel != null) mgmtInfoLabel.text = $"人口: {data.population:N0}人 / 所有者: {data.owner}";

        if (buildingSlotsContainer != null)
        {
            buildingSlotsContainer.Clear();
            
            // 建設済み・建設中の建物の表示
            for (int i = 0; i < data.slots.Count; i++)
            {
                var slot = data.slots[i];
                BuildingData bData = BuildingDataManager.Instance.GetBuildingData(slot.buildingId);
                
                Button slotBtn = new Button();
                if (bData != null)
                {
                    if (slot.isConstructed)
                    {
                        slotBtn.text = $"{bData.name}\n収入+{bData.incomeBonus}";
                        slotBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.8f)); // 完成済みは青系
                    }
                    else
                    {
                        slotBtn.text = $"{bData.name} 建設中\n(残り: {slot.remainingDays}日)";
                        slotBtn.style.backgroundColor = new StyleColor(new Color(0.8f, 0.6f, 0.2f)); // 建設中はオレンジ系
                    }
                }
                else
                {
                    slotBtn.text = "不明な建物";
                }
                slotBtn.style.width = 120;
                slotBtn.style.height = 60;
                buildingSlotsContainer.Add(slotBtn);
            }

            // 空きスロットの表示
            int emptySlots = data.maxSlots - data.slots.Count;
            for (int i = 0; i < emptySlots; i++)
            {
                Button emptyBtn = new Button();
                emptyBtn.text = "空きスロット\n[+]建設する";
                emptyBtn.style.width = 120;
                emptyBtn.style.height = 60;
                emptyBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.2f));
                emptyBtn.clicked += ShowConstructionPopup;
                buildingSlotsContainer.Add(emptyBtn);
            }
        }
    }

    private void ShowConstructionPopup()
    {
        if (constructionPopup == null || buildingListScrollView == null) return;

        buildingListScrollView.Clear();
        List<BuildingData> allBuildings = BuildingDataManager.Instance.GetAllBuildings();

        foreach (var building in allBuildings)
        {
            Button buildBtn = new Button();
            buildBtn.text = $"{building.name} (費用: {building.cost})\n{building.description}";
            buildBtn.style.height = 60;
            buildBtn.style.marginBottom = 5;
            
            // 資金不足の場合はボタンを無効化（グレーアウト）する
            long currentFunds = ResourceManager.Instance.GetData().funds;
            if (currentFunds < building.cost)
            {
                buildBtn.SetEnabled(false);
                buildBtn.text += "\n<color=red>※資金不足</color>";
            }

            buildBtn.clicked += () =>
            {
                constructionPopup.style.display = DisplayStyle.None;
                ConstructionManager.Instance.Build(selectedProvinceKeyForBuilding, building.id);
            };
            buildingListScrollView.Add(buildBtn);
        }

        constructionPopup.style.display = DisplayStyle.Flex;
    }


    public void UpdateResourceUI()
    {
        if (ResourceManager.Instance == null) return;

        var res = ResourceManager.Instance.GetData();
        if (res == null) return;

        if (fundsLabel != null) fundsLabel.text = $"{res.funds:N0}";
        if (manpowerLabel != null) manpowerLabel.text = $"{res.manpower:N0}";
    }

    private void UpdateDateUI(int year, int month, int day)
    {
        if (dateLabel != null)
        {
            dateLabel.text = $"{year}年 {month}月 {day}日";
        }
    }
}