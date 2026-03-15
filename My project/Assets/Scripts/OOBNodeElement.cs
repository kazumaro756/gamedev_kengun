using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class OOBNodeElement : VisualElement
{
    private bool isDragging = false;
    private Vector2 startMousePosition;
    private Vector2 startNodePosition;

    private Label titleLabel;
    
    // 接続ポート（UI上の見た目）
    public VisualElement InPort { get; private set; }
    public VisualElement OutPort { get; private set; }
    private VisualElement slotContainer;

    // イベント
    public System.Action OnPositionChanged;
    public static System.Action<OOBNodeElement, Vector2> OnGlobalWireDragStart;
    public static System.Action<OOBNodeElement> OnGlobalWireDropTarget;

    // データ参照
    public UnitData UnitReference { get; private set; }

    public OOBNodeElement(UnitData data)
    {
        UnitReference = data;
        
        // ノード単位のスタイリング
        style.position = Position.Absolute;
        
        // データに座標があれば復元、なければ初期値（NaN防止）
        float initX = float.IsNaN(data.nodePosX) ? 0 : data.nodePosX;
        float initY = float.IsNaN(data.nodePosY) ? 0 : data.nodePosY;
        
        style.left = initX;
        style.top = initY;
        
        style.width = 150;
        style.height = 80;
        style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 0.9f));
        style.borderBottomWidth = style.borderTopWidth = style.borderLeftWidth = style.borderRightWidth = 2;
        style.borderBottomColor = style.borderTopColor = style.borderLeftColor = style.borderRightColor = new StyleColor(Color.gray);
        style.borderTopLeftRadius = style.borderTopRightRadius = style.borderBottomLeftRadius = style.borderBottomRightRadius = 8;
        style.flexDirection = FlexDirection.Column;

        // ヘッダー（タイトル部分）
        VisualElement header = new VisualElement();
        header.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        header.style.height = 25;
        header.style.borderTopLeftRadius = 6;
        header.style.borderTopRightRadius = 6;
        header.style.justifyContent = Justify.Center;
        
        titleLabel = new Label($"[{data.type.ToString()}]\n{data.unitName}");
        titleLabel.style.color = Color.white;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.fontSize = 10;
        header.Add(titleLabel);
        
        Add(header);

        // ボディ（ポート配置レイアウト）
        VisualElement body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.justifyContent = Justify.SpaceBetween;
        body.style.alignItems = Align.Center;
        body.style.flexGrow = 1;
        body.style.paddingLeft = 5;
        body.style.paddingRight = 5;

        // INポート (左側)
        InPort = CreatePort();
        InPort.RegisterCallback<PointerEnterEvent>(evt => { OnGlobalWireDropTarget?.Invoke(this); });
        InPort.RegisterCallback<PointerLeaveEvent>(evt => { OnGlobalWireDropTarget?.Invoke(null); });
        body.Add(InPort);

        // コンテンツ（将来的に兵員数などを入れるスペース）
        VisualElement content = new VisualElement();
        content.style.flexGrow = 1;
        content.style.justifyContent = Justify.Center;
        content.style.alignItems = Align.Center;

        slotContainer = new VisualElement();
        slotContainer.style.flexDirection = FlexDirection.Row;
        slotContainer.style.justifyContent = Justify.Center;
        content.Add(slotContainer);

        body.Add(content);

        // OUTポート (右側)
        OutPort = CreatePort();
        OutPort.RegisterCallback<PointerDownEvent>(evt => 
        {
            if (evt.button != 0) return;
            OnGlobalWireDragStart?.Invoke(this, evt.position);
            evt.StopPropagation();
        });
        body.Add(OutPort);

        Add(body);

        RefreshSlots();

        // ドラッグ移動イベントの登録
        RegisterCallback<PointerDownEvent>(OnPointerDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerUpEvent>(OnPointerUp);
        RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private VisualElement CreatePort()
    {
        VisualElement port = new VisualElement();
        port.style.width = 12;
        port.style.height = 12;
        port.style.backgroundColor = new StyleColor(Color.cyan);
        port.style.borderTopLeftRadius = port.style.borderTopRightRadius = port.style.borderBottomLeftRadius = port.style.borderBottomRightRadius = 6;
        return port;
    }

    public void RefreshSlots()
    {
        if (slotContainer == null) return;
        slotContainer.Clear();

        if (UnitDataManager.Instance == null || string.IsNullOrEmpty(UnitReference.templateId)) return;

        var tpl = UnitDataManager.Instance.GetTemplate(UnitReference.templateId);
        if (tpl == null) return;

        // 子部隊を取得して兵科ごとにカウント
        Dictionary<UnitType, int> childCounts = new Dictionary<UnitType, int>();
        foreach (var child in UnitReference.subUnits)
        {
            if (!childCounts.ContainsKey(child.type)) childCounts[child.type] = 0;
            childCounts[child.type]++;
        }

        bool allCoreFilled = true;

        foreach (var req in tpl.requirements)
        {
            int currentCount = childCounts.ContainsKey(req.requiredType) ? childCounts[req.requiredType] : 0;
            
            // コア枠の表示
            for (int i = 0; i < req.coreCount; i++)
            {
                bool isFilled = i < currentCount;
                slotContainer.Add(CreateSlotIcon(isFilled ? Color.green : Color.gray, true));
                if (!isFilled) allCoreFilled = false;
            }

            // サポート枠の表示
            int supportFilled = Mathf.Max(0, currentCount - req.coreCount);
            for (int i = 0; i < req.maxSupportCount; i++)
            {
                bool isFilled = i < supportFilled;
                slotContainer.Add(CreateSlotIcon(isFilled ? Color.blue : Color.gray, false));
            }
        }

        // 建軍完了フィードバック
        if (allCoreFilled && tpl.requirements.Count > 0)
        {
            style.borderBottomColor = style.borderTopColor = style.borderLeftColor = style.borderRightColor = new StyleColor(Color.yellow);
        }
        else
        {
            style.borderBottomColor = style.borderTopColor = style.borderLeftColor = style.borderRightColor = new StyleColor(Color.gray);
        }
    }

    private VisualElement CreateSlotIcon(Color color, bool isCore)
    {
        VisualElement icon = new VisualElement();
        icon.style.width = 8;
        icon.style.height = 8;
        icon.style.backgroundColor = new StyleColor(color);
        icon.style.marginLeft = 2;
        icon.style.marginRight = 2;
        if (isCore)
        {
            icon.style.borderTopLeftRadius = icon.style.borderTopRightRadius = icon.style.borderBottomLeftRadius = icon.style.borderBottomRightRadius = 4;
        }
        return icon;
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        // 左クリックのみ反応
        if (evt.button != 0) return;

        isDragging = true;
        startMousePosition = evt.position;
        startNodePosition = new Vector2(resolvedStyle.left, resolvedStyle.top);
        
        // ドラッグ中に他の要素（背景等）へイベントが漏れるのを防ぎ、マウスの動きをトラッキングさせる
        this.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging || !this.HasPointerCapture(evt.pointerId)) return;

        Vector2 mouseDelta = new Vector2(evt.position.x, evt.position.y) - startMousePosition;
        float newX = startNodePosition.x + mouseDelta.x;
        float newY = startNodePosition.y + mouseDelta.y;
        
        style.left = newX;
        style.top = newY;
        
        // データ本体に座標を保存する
        if (UnitReference != null)
        {
            UnitReference.nodePosX = newX;
            UnitReference.nodePosY = newY;
        }

        OnPositionChanged?.Invoke();
        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!isDragging || !this.HasPointerCapture(evt.pointerId)) return;

        isDragging = false;
        this.ReleasePointer(evt.pointerId);
        evt.StopPropagation();

        // 移動完了時、エディタ保存処理を走らせる
        if (UnitDataManager.Instance != null)
        {
            UnitDataManager.Instance.SaveData();
        }
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        isDragging = false;
    }
}
