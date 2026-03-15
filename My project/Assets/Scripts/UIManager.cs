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
        (provinceMgmtContainer != null && provinceMgmtContainer.resolvedStyle.display != DisplayStyle.None) ||
        (unitMgmtContainer != null && unitMgmtContainer.resolvedStyle.display != DisplayStyle.None) ||
        (nodeOOBContainer != null && nodeOOBContainer.resolvedStyle.display != DisplayStyle.None);

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

    // --- 部隊管理パネル ---
    private VisualElement unitMgmtContainer;
    private ScrollView unitTreeScrollView;

    // --- インタラクティブノードOOBパネル ---
    private VisualElement nodeOOBContainer;
    private ScrollView nodePaletteScrollView;
    private VisualElement nodeCanvasArea;
    private VisualElement nodeCanvasContent;
    private VisualElement nodesLayer;

    // パレットからのドラッグ＆ドロップ用状態変数
    private bool isDraggingFromPalette = false;
    private string draggedTemplateName;
    private string draggedTemplateId;
    private UnitType draggedTemplateType;
    private VisualElement dragGhost;

    // キャンバスのパニング（視点移動）用変数
    private bool isPanningCanvas = false;
    private Vector2 lastPanMousePos;

    // ワイヤー結線操作用の状態変数
    private VisualElement wireLayer;
    private bool isDraggingWire = false;
    private OOBNodeElement wireSourceNode;
    private OOBNodeElement currentWireDropTarget;
    private VisualElement wireGhost;

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

        // --- 部隊管理関係 ---
        var openUnitBtn = root.Q<Button>("OpenUnitMgmtButton");
        if (openUnitBtn != null) openUnitBtn.clicked += ShowUnitManagement;

        unitMgmtContainer = root.Q<VisualElement>("UnitManagementContainer");
        unitTreeScrollView = root.Q<ScrollView>("UnitTreeScrollView");
        
        var closeUnitBtn = root.Q<Button>("CloseUnitMgmtButton");
        if (closeUnitBtn != null) closeUnitBtn.clicked += () => unitMgmtContainer.style.display = DisplayStyle.None;

        // --- インタラクティブノードOOB関係 ---
        var openNodeOOBBtn = root.Q<Button>("OpenNodeOOBButton");
        if (openNodeOOBBtn != null) openNodeOOBBtn.clicked += ShowNodeOOBManagement;

        nodeOOBContainer = root.Q<VisualElement>("NodeOOBContainer");
        nodePaletteScrollView = root.Q<ScrollView>("NodePaletteScrollView");
        nodeCanvasArea = root.Q<VisualElement>("NodeCanvasArea");
        nodeCanvasContent = root.Q<VisualElement>("NodeCanvasContent");
        nodesLayer = root.Q<VisualElement>("NodesLayer");
        wireLayer = root.Q<VisualElement>("WireLayer");

        var closeNodeOOBBtn = root.Q<Button>("CloseNodeOOBButton");
        if (closeNodeOOBBtn != null) closeNodeOOBBtn.clicked += () => nodeOOBContainer.style.display = DisplayStyle.None;

        // Canvas領域の右上に「自動整列」ボタンを動的に追加
        if (nodeCanvasArea != null)
        {
            Button autoArrangeBtn = new Button();
            autoArrangeBtn.text = "自動整列";
            autoArrangeBtn.style.position = Position.Absolute;
            autoArrangeBtn.style.top = 10;
            autoArrangeBtn.style.right = 10;
            autoArrangeBtn.style.width = 90;
            autoArrangeBtn.style.height = 30;
            autoArrangeBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.2f));
            autoArrangeBtn.style.color = Color.white;
            autoArrangeBtn.clicked += AutoArrangeNodes;
            nodeCanvasArea.Add(autoArrangeBtn);
        }

        // ドラッグ用ゴースト要素（マウスに追従する仮の見た目）
        dragGhost = new VisualElement();
        dragGhost.style.position = Position.Absolute;
        dragGhost.style.width = 120;
        dragGhost.style.height = 40;
        dragGhost.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.8f));
        dragGhost.style.borderTopLeftRadius = dragGhost.style.borderTopRightRadius = dragGhost.style.borderBottomLeftRadius = dragGhost.style.borderBottomRightRadius = 5;
        dragGhost.style.display = DisplayStyle.None; // 普段は隠す
        dragGhost.pickingMode = PickingMode.Ignore; // マウスイベントをブロックさせない
        root.Add(dragGhost); // 画面全体で動かせるようにroot直下へ配置

        // パニング（視点移動）のイベント登録
        if (nodeCanvasArea != null)
        {
            nodeCanvasArea.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);
            nodeCanvasArea.RegisterCallback<PointerMoveEvent>(OnCanvasPointerMove);
            nodeCanvasArea.RegisterCallback<PointerUpEvent>(OnCanvasPointerUp);
            nodeCanvasArea.RegisterCallback<PointerCaptureOutEvent>(OnCanvasPointerCaptureOut);
        }

        // 画面全体のMouseMoveイベントで、パレットからのドラッグ中ならゴーストを動かす
        root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);

        // 結線用ゴースト（ドラッグ中の線）の初期化
        wireGhost = new VisualElement();
        wireGhost.style.position = Position.Absolute;
        wireGhost.style.backgroundColor = new StyleColor(Color.yellow);
        wireGhost.pickingMode = PickingMode.Ignore;
        wireGhost.style.display = DisplayStyle.None;
        wireGhost.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0));
        if (wireLayer != null) wireLayer.Add(wireGhost);

        // ノード側からの結線イベントフック
        OOBNodeElement.OnGlobalWireDragStart = (node, pos) => {
            isDraggingWire = true;
            wireSourceNode = node;
            wireGhost.style.display = DisplayStyle.Flex;
        };
        OOBNodeElement.OnGlobalWireDropTarget = (node) => {
            if (node != wireSourceNode) currentWireDropTarget = node;
            else currentWireDropTarget = null;
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
        if (unitMgmtContainer != null) unitMgmtContainer.style.display = DisplayStyle.None;
        if (nodeOOBContainer != null) nodeOOBContainer.style.display = DisplayStyle.None;

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
        if (unitMgmtContainer != null) unitMgmtContainer.style.display = DisplayStyle.None;
        if (nodeOOBContainer != null) nodeOOBContainer.style.display = DisplayStyle.None;

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

    // --- 部隊管理（Order of Battle）パネル関連 ---

    public void ShowUnitManagement()
    {
        if (unitMgmtContainer == null || unitTreeScrollView == null) return;
        
        // 他のパネルを閉じる
        if (taskListContainer != null) taskListContainer.style.display = DisplayStyle.None;
        if (taskDetailContainer != null) taskDetailContainer.style.display = DisplayStyle.None;
        if (provinceMgmtContainer != null) provinceMgmtContainer.style.display = DisplayStyle.None;
        if (nodeOOBContainer != null) nodeOOBContainer.style.display = DisplayStyle.None;
        
        unitTreeScrollView.Clear();

        if (UnitDataManager.Instance != null)
        {
            var rootUnits = UnitDataManager.Instance.GetRootUnits();
            foreach (var unit in rootUnits)
            {
                VisualElement node = RenderUnitNode(unit, 0);
                if (node != null)
                {
                    unitTreeScrollView.Add(node);
                }
            }
        }

        unitMgmtContainer.style.display = DisplayStyle.Flex;
    }

    // --- インタラクティブノード部隊編制（Node OOB）パネル ---

    public void ShowNodeOOBManagement()
    {
        if (nodeOOBContainer == null) return;
        
        // 他のパネルを閉じる
        if (taskListContainer != null) taskListContainer.style.display = DisplayStyle.None;
        if (taskDetailContainer != null) taskDetailContainer.style.display = DisplayStyle.None;
        if (provinceMgmtContainer != null) provinceMgmtContainer.style.display = DisplayStyle.None;
        if (unitMgmtContainer != null) unitMgmtContainer.style.display = DisplayStyle.None;

        PopulateNodePalette();
        RefreshCanvasNodes();

        nodeOOBContainer.style.display = DisplayStyle.Flex;
    }

    private void RefreshCanvasNodes()
    {
        nodesLayer.Clear();
        wireLayer.Clear();
        
        // 結線用ゴースト（ドラッグ中の線）を再追加する
        wireLayer.Add(wireGhost);

        if (UnitDataManager.Instance != null)
        {
            var rootUnits = UnitDataManager.Instance.GetRootUnits();
            Dictionary<string, OOBNodeElement> createdNodes = new Dictionary<string, OOBNodeElement>();

            // 全ノードとワイヤーを再帰的に生成
            foreach (var root in rootUnits)
            {
                RenderDataNodeRecursive(root, createdNodes);
            }
        }
    }

    private void RenderDataNodeRecursive(UnitData data, Dictionary<string, OOBNodeElement> createdNodes)
    {
        // ノードの生成（位置がなければ初期値を割り振る）
        if (float.IsNaN(data.nodePosX)) data.nodePosX = 100f;
        if (float.IsNaN(data.nodePosY)) data.nodePosY = 100f;

        OOBNodeElement node = new OOBNodeElement(data);
        nodesLayer.Add(node);
        createdNodes[data.unitId] = node;

        foreach (var child in data.subUnits)
        {
            RenderDataNodeRecursive(child, createdNodes);
            
            // ワイヤーの生成
            if (createdNodes.TryGetValue(child.unitId, out OOBNodeElement childNode))
            {
                var wire = new OOBWireElement(node, childNode);
                wireLayer.Add(wire);
            }
        }
    }

    private void PopulateNodePalette()
    {
        if (nodePaletteScrollView == null) return;
        nodePaletteScrollView.Clear();

        // UnitDataManagerから全テンプレートを取得してパレットに並べる
        var templates = UnitDataManager.Instance.GetAllTemplates();
        
        foreach (var t in templates)
        {
            VisualElement paletteItem = new VisualElement();
            paletteItem.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            paletteItem.style.borderTopWidth = 1;
            paletteItem.style.borderBottomWidth = 1;
            paletteItem.style.borderLeftWidth = 1;
            paletteItem.style.borderRightWidth = 1;
            paletteItem.style.borderTopColor = new StyleColor(Color.gray);
            paletteItem.style.borderBottomColor = new StyleColor(Color.gray);
            paletteItem.style.borderLeftColor = new StyleColor(Color.gray);
            paletteItem.style.borderRightColor = new StyleColor(Color.gray);
            paletteItem.style.paddingLeft = 10;
            paletteItem.style.paddingRight = 10;
            paletteItem.style.paddingTop = 15;
            paletteItem.style.paddingBottom = 15;
            paletteItem.style.marginBottom = 10;
            paletteItem.style.borderTopLeftRadius = 5;
            paletteItem.style.borderTopRightRadius = 5;
            paletteItem.style.borderBottomLeftRadius = 5;
            paletteItem.style.borderBottomRightRadius = 5;

            Label label = new Label(t.templateName);
            label.style.color = Color.white;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            paletteItem.Add(label);
            
            // ドラッグ開始(Drag&Drop)のイベントを追加
            paletteItem.RegisterCallback<PointerDownEvent>(evt => 
            {
                if (evt.button != 0) return; // 左クリックのみ
                isDraggingFromPalette = true;
                draggedTemplateName = t.templateName;
                draggedTemplateId = t.templateId;
                draggedTemplateType = t.targetUnitType;

                // ゴースト要素を表示してマウス位置に合わせる
                dragGhost.style.display = DisplayStyle.Flex;
                dragGhost.style.left = evt.position.x - 60; // 幅の半分ずらす
                dragGhost.style.top = evt.position.y - 20;

                evt.StopPropagation();
            });
            
            nodePaletteScrollView.Add(paletteItem);
        }
    }

    // --- 自動整列（Auto Layout）アルゴリズム ---
    private void AutoArrangeNodes()
    {
        if (UnitDataManager.Instance == null) return;

        var rootUnits = UnitDataManager.Instance.GetRootUnits();
        float currentY = 50f; // 開始Y座標
        float startX = 50f;   // 開始X座標

        foreach (var root in rootUnits)
        {
            float heightUsed = ArrangeNodeRecursive(root, startX, currentY);
            currentY += heightUsed + 80f; // 次のルート部隊（別系統の師団など）のために間隔をあける
        }

        UnitDataManager.Instance.SaveData();
        RefreshCanvasNodes(); // 位置を更新したあと再描画
        
        // 自動整列後、視点を初期位置に戻す（オプション）
        if (nodeCanvasContent != null)
        {
            nodeCanvasContent.style.left = 0;
            nodeCanvasContent.style.top = 0;
        }
    }

    // 返り値は、自分と自分の子孫全体が消費したY方向の「高さ」
    private float ArrangeNodeRecursive(UnitData node, float x, float y)
    {
        float horizontalSpacing = 280f; // 親子間の横の間隔
        float verticalSpacing = 90f;   // 兄弟間の縦の間隔

        // 自分が末端（子要素なし）の場合
        if (node.subUnits == null || node.subUnits.Count == 0)
        {
            node.nodePosX = x;
            node.nodePosY = y;
            return verticalSpacing;
        }

        // 子要素がある場合、まず子要素を再帰的に並べる
        float childrenTotalHeight = 0f;
        foreach (var child in node.subUnits)
        {
            float childHeight = ArrangeNodeRecursive(child, x + horizontalSpacing, y + childrenTotalHeight);
            childrenTotalHeight += childHeight;
        }

        // トップアライン（上揃え）：自分のY座標は、最初の子要素のY座標と同じ（つまり y そのまま）にする
        node.nodePosX = x;
        node.nodePosY = y;

        // 親（呼び出し元）には、自分と子要素群が消費したトータルの高さを返す
        return childrenTotalHeight;
    }

    // --- キャンバスのパニング処理 ---
    private void OnCanvasPointerDown(PointerDownEvent evt)
    {
        // 中クリック(2) または 右クリック(1) でパニング（視点移動）
        if (evt.button == 1 || evt.button == 2)
        {
            isPanningCanvas = true;
            lastPanMousePos = evt.position;
            nodeCanvasArea.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    private void OnCanvasPointerMove(PointerMoveEvent evt)
    {
        if (isPanningCanvas && nodeCanvasArea.HasPointerCapture(evt.pointerId))
        {
            Vector2 delta = new Vector2(evt.position.x, evt.position.y) - lastPanMousePos;
            
            // nodeCanvasContent (中の領域全体) を動かす
            Vector2 currentPos = new Vector2(nodeCanvasContent.resolvedStyle.left, nodeCanvasContent.resolvedStyle.top);
            if (float.IsNaN(currentPos.x)) currentPos.x = 0;
            if (float.IsNaN(currentPos.y)) currentPos.y = 0;

            nodeCanvasContent.style.left = currentPos.x + delta.x;
            nodeCanvasContent.style.top = currentPos.y + delta.y;

            lastPanMousePos = evt.position;
            evt.StopPropagation();
        }
    }

    private void OnCanvasPointerUp(PointerUpEvent evt)
    {
        if (isPanningCanvas && nodeCanvasArea.HasPointerCapture(evt.pointerId))
        {
            isPanningCanvas = false;
            nodeCanvasArea.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    private void OnCanvasPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        isPanningCanvas = false;
    }

    // --- パレットからのドラッグ＆ドロップ（全体処理） ---
    private void OnGlobalPointerMove(PointerMoveEvent evt)
    {
        if (isDraggingFromPalette)
        {
            dragGhost.style.left = evt.position.x - 60;
            dragGhost.style.top = evt.position.y - 20;
        }
        else if (isDraggingWire && wireSourceNode != null && nodeCanvasArea != null)
        {
            // キャンバス内のローカル座標計算。evt.position は Vector3 なので Vector2 にキャスト
            Vector2 mousePos2D = new Vector2(evt.position.x, evt.position.y);
            Vector2 localMouse = mousePos2D - new Vector2(nodeCanvasArea.worldBound.xMin, nodeCanvasArea.worldBound.yMin);
            float contentLeft = float.IsNaN(nodeCanvasContent.resolvedStyle.left) ? 0 : nodeCanvasContent.resolvedStyle.left;
            float contentTop = float.IsNaN(nodeCanvasContent.resolvedStyle.top) ? 0 : nodeCanvasContent.resolvedStyle.top;
            
            Vector2 startPos = new Vector2(
                wireSourceNode.resolvedStyle.left + wireSourceNode.resolvedStyle.width,
                wireSourceNode.resolvedStyle.top + wireSourceNode.resolvedStyle.height / 2f
            );
            Vector2 endPos = new Vector2(localMouse.x - contentLeft, localMouse.y - contentTop);

            float dist = Vector2.Distance(startPos, endPos);
            float angle = Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x) * Mathf.Rad2Deg;

            wireGhost.style.left = startPos.x;
            wireGhost.style.top = startPos.y;
            wireGhost.style.width = dist;
            wireGhost.style.height = 2;
            wireGhost.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnGlobalPointerUp(PointerUpEvent evt)
    {
        if (isDraggingFromPalette)
        {
            isDraggingFromPalette = false;
            dragGhost.style.display = DisplayStyle.None;

            // マウスカーソルがキャンバス(nodeCanvasArea)の範囲内に入っているか判定する
            if (nodeCanvasArea != null && nodeCanvasArea.worldBound.Contains(evt.position))
            {
                // ドロップ位置を、キャンバススクロール(パニング)を考慮したローカル座標へ変換
                Vector2 mousePos2D = new Vector2(evt.position.x, evt.position.y);
                Vector2 localMousePos = mousePos2D - new Vector2(nodeCanvasArea.worldBound.xMin, nodeCanvasArea.worldBound.yMin);
                
                // Contentのオフセットを引いて、実際のNodesLayer上の座標にする
                float contentLeft = float.IsNaN(nodeCanvasContent.resolvedStyle.left) ? 0 : nodeCanvasContent.resolvedStyle.left;
                float contentTop = float.IsNaN(nodeCanvasContent.resolvedStyle.top) ? 0 : nodeCanvasContent.resolvedStyle.top;

                Vector2 spawnPos = new Vector2(localMousePos.x - contentLeft - 75, localMousePos.y - contentTop - 40);

                // 実データとしてUnitDataManagerに登録
                if (UnitDataManager.Instance != null)
                {
                    UnitData newData = UnitDataManager.Instance.AddNewUnit(draggedTemplateName, draggedTemplateType, spawnPos.x, spawnPos.y, draggedTemplateId);
                    
                    // 新しいノードをUIに生成して配置
                    OOBNodeElement newNode = new OOBNodeElement(newData);
                    nodesLayer.Add(newNode);
                }
            }
        }
        else if (isDraggingWire)
        {
            isDraggingWire = false;
            wireGhost.style.display = DisplayStyle.None;

            if (currentWireDropTarget != null && wireSourceNode != null && currentWireDropTarget != wireSourceNode)
            {
                // データレベルでの親子関係を更新
                string errorMsg = "";
                if (UnitDataManager.Instance != null)
                {
                    errorMsg = UnitDataManager.Instance.LinkUnits(wireSourceNode.UnitReference.unitId, currentWireDropTarget.UnitReference.unitId);
                }

                if (string.IsNullOrEmpty(errorMsg))
                {
                    // UI上での結線成立：ワイヤーを生成してレイヤーに追加
                    var newWire = new OOBWireElement(wireSourceNode, currentWireDropTarget);
                    wireLayer.Add(newWire);

                    // スロット表示を更新
                    wireSourceNode.RefreshSlots();
                    
                    // 他の親から外れた可能性があるので、念のため全体再描画して古い線を消す
                    RefreshCanvasNodes();
                }
                else
                {
                    // バリデーション失敗
                    Debug.LogWarning($"[OOB編制エラー] {errorMsg}");
                }
            }

            wireSourceNode = null;
            currentWireDropTarget = null;
        }
    }

    // 再帰的に部隊ツリーを描画し、コンテナを返す
    private VisualElement RenderUnitNode(UnitData unit, int indentLevel, bool isLastChild = true, bool isRoot = true)
    {
        if (unit == null) return null;

        VisualElement nodeContainer = new VisualElement();

        // 行全体を包むコンテナ（左に枝の描画領域、右にコンテンツ）
        VisualElement rowContainer = new VisualElement();
        rowContainer.style.flexDirection = FlexDirection.Row;
        
        // --------------------------------------------------
        // ツリー構造の線（枝）を描画する左側領域の設定
        // --------------------------------------------------
        if (!isRoot)
        {
            VisualElement branchLineContainer = new VisualElement();
            branchLineContainer.style.width = 15;
            branchLineContainer.style.position = Position.Relative;
            
            // 親から伸びてくる縦線
            VisualElement verticalLine = new VisualElement();
            verticalLine.style.position = Position.Absolute;
            verticalLine.style.left = 0;
            verticalLine.style.top = 0;
            verticalLine.style.width = 2;
            verticalLine.style.backgroundColor = new StyleColor(Color.gray);
            
            // 右へ曲がって要素の中央に刺さる横線
            VisualElement horizontalLine = new VisualElement();
            horizontalLine.style.position = Position.Absolute;
            horizontalLine.style.left = 0;
            // headerRow自体の高さが概ね35px（padding上下各5px + ボタン25px等）なので、その中央である16.5px付近に線を引く
            horizontalLine.style.top = 16.5f; 
            horizontalLine.style.width = 15;
            horizontalLine.style.height = 2;
            horizontalLine.style.backgroundColor = new StyleColor(Color.gray);

            if (isLastChild)
            {
                // 最後の子要素なら、縦線はこのノードの横線と同じ高さで止める
                verticalLine.style.height = 17.5f; 
            }
            else
            {
                // それ以外なら、次の兄弟要素へ届かせるために下まで貫通させる
                verticalLine.style.bottom = 0;
            }

            branchLineContainer.Add(verticalLine);
            branchLineContainer.Add(horizontalLine);

            rowContainer.Add(branchLineContainer);
        }

        // --------------------------------------------------
        // 実際のコンテンツ（ヘッダーと子要素コンテナ）の右側領域
        // --------------------------------------------------
        VisualElement contentContainer = new VisualElement();
        contentContainer.style.flexDirection = FlexDirection.Column;
        contentContainer.style.flexGrow = 1;

        // ヘッダー行の作成
        VisualElement headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.alignItems = Align.Center;
        headerRow.style.marginBottom = 5;
        headerRow.style.paddingLeft = 5;
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingBottom = 5;
        
        // 単位に応じて背景色を微かに変えて視認性を上げる
        headerRow.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.5f));
        if (indentLevel == 0) headerRow.style.backgroundColor = new StyleColor(new Color(0.1f, 0.3f, 0.1f, 0.8f)); // 師団は緑系
        if (indentLevel == 1) headerRow.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.1f, 0.8f)); // 旅団は黄系
        if (indentLevel == 2) headerRow.style.backgroundColor = new StyleColor(new Color(0.3f, 0.1f, 0.1f, 0.8f)); // 連隊は赤系

        bool hasChildren = unit.subUnits != null && unit.subUnits.Count > 0;
        VisualElement childrenContainer = new VisualElement();

        // 折りたたみボタン（子がいる場合のみ機能する）
        Button toggleBtn = new Button();
        toggleBtn.style.width = 25;
        toggleBtn.style.height = 25;
        toggleBtn.style.marginRight = 5;
        toggleBtn.text = hasChildren ? "-" : ""; // 初期表示は展開状態とするため「-」
        
        if (hasChildren)
        {
            bool isExpanded = true;
            toggleBtn.clicked += () =>
            {
                isExpanded = !isExpanded;
                toggleBtn.text = isExpanded ? "-" : "+"; // 開いている時は「-」、閉じている時は「+」
                childrenContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            };
        }
        else
        {
            // 子がいない場合はボタンとしての見た目を消す（パディングだけ残す）
            toggleBtn.style.backgroundColor = new StyleColor(Color.clear);
            toggleBtn.style.borderBottomWidth = 0;
            toggleBtn.style.borderTopWidth = 0;
            toggleBtn.style.borderLeftWidth = 0;
            toggleBtn.style.borderRightWidth = 0;
        }

        headerRow.Add(toggleBtn);

        // 部隊タイプの文字起こし
        string typeStr = unit.type.ToString();

        // ラベルを作成
        Label label = new Label();
        label.text = $"[{typeStr}] {unit.unitName} (兵力: {unit.currentManpower} / {unit.maxManpower})";
        label.style.color = Color.white;
        label.style.fontSize = 14;
        
        headerRow.Add(label);
        contentContainer.Add(headerRow);

        // 子要素のコンテナ設定（階層線を描画）
        if (hasChildren)
        {
            // 子要素コンテナもインデント調整と、左側の貫通線（子供が続く間）の設定など
            childrenContainer.style.marginLeft = isRoot ? 5 : 0; 
            
            for (int i = 0; i < unit.subUnits.Count; i++)
            {
                var child = unit.subUnits[i];
                bool isChildLast = (i == unit.subUnits.Count - 1);
                var childNode = RenderUnitNode(child, indentLevel + 1, isChildLast, false);
                if (childNode != null)
                {
                    childrenContainer.Add(childNode);
                }
            }
            contentContainer.Add(childrenContainer);
        }

        rowContainer.Add(contentContainer);
        nodeContainer.Add(rowContainer);

        return nodeContainer;
    }
}