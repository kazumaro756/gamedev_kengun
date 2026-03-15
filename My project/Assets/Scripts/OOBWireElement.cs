using UnityEngine;
using UnityEngine.UIElements;
using System;

public class OOBWireElement : VisualElement
{
    private OOBNodeElement outputNode;
    private OOBNodeElement inputNode;

    private VisualElement line1;
    private VisualElement line2;
    private VisualElement line3;

    public OOBWireElement(OOBNodeElement outNode, OOBNodeElement inNode)
    {
        outputNode = outNode;
        inputNode = inNode;

        style.position = Position.Absolute;
        style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
        pickingMode = PickingMode.Ignore;

        Color wireColor = new Color(0.2f, 0.8f, 0.8f, 0.8f);

        line1 = new VisualElement(); line1.style.position = Position.Absolute; line1.style.backgroundColor = wireColor; line1.pickingMode = PickingMode.Ignore;
        line2 = new VisualElement(); line2.style.position = Position.Absolute; line2.style.backgroundColor = wireColor; line2.pickingMode = PickingMode.Ignore;
        line3 = new VisualElement(); line3.style.position = Position.Absolute; line3.style.backgroundColor = wireColor; line3.pickingMode = PickingMode.Ignore;

        Add(line1); Add(line2); Add(line3);

        if (outputNode != null) outputNode.OnPositionChanged += UpdateLine;
        if (inputNode != null) inputNode.OnPositionChanged += UpdateLine;

        // 次のフレームでレイアウトが確定したら描画を合わせる
        schedule.Execute(() => UpdateLine());
    }

    public void UpdateLine()
    {
        if (outputNode == null || inputNode == null) return;
        
        // Outputはノードの右辺中央、Inputは左辺中央と仮定
        Vector2 startPos = new Vector2(outputNode.resolvedStyle.left + outputNode.resolvedStyle.width, outputNode.resolvedStyle.top + outputNode.resolvedStyle.height / 2f);
        Vector2 endPos = new Vector2(inputNode.resolvedStyle.left, inputNode.resolvedStyle.top + inputNode.resolvedStyle.height / 2f);

        // NaN対応（初回レイアウト前など）
        if (float.IsNaN(startPos.x) || float.IsNaN(endPos.x)) return;

        float thickness = 3f;
        float midX = (startPos.x + endPos.x) / 2f;

        // Line 1: outNode から midX まで (横線)
        line1.style.left = Mathf.Min(startPos.x, midX);
        line1.style.top = startPos.y - thickness / 2f;
        line1.style.width = Mathf.Abs(startPos.x - midX);
        line1.style.height = thickness;

        // Line 2: midX での (縦線)
        float minY = Mathf.Min(startPos.y, endPos.y);
        float maxY = Mathf.Max(startPos.y, endPos.y);
        line2.style.left = midX - thickness / 2f;
        line2.style.top = minY - thickness / 2f;
        line2.style.width = thickness;
        line2.style.height = (maxY - minY) + thickness;

        // Line 3: midX から inNode まで (横線)
        line3.style.left = Mathf.Min(midX, endPos.x);
        line3.style.top = endPos.y - thickness / 2f;
        line3.style.width = Mathf.Abs(endPos.x - midX);
        line3.style.height = thickness;
    }
}
