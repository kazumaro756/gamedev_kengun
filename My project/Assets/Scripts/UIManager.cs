using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Label stateNameLabel;
    private Label populationLabel;
    private Label ownerLabel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        // UI DocumentからUIのルート要素を取得
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // UXMLで定義した名前（name属性）でラベルを検索して取得
        stateNameLabel = root.Q<Label>("StateNameLabel");
        populationLabel = root.Q<Label>("PopulationLabel");
        ownerLabel = root.Q<Label>("OwnerLabel");
    }

    // クリック時に呼ばれる更新処理
    public void UpdateProvinceUI(ProvinceData data)
    {
        if (data != null)
        {
            stateNameLabel.text = data.stateName;
            populationLabel.text = $"人口: {data.population:N0}人"; // N0でカンマ区切りにする
            ownerLabel.text = $"所有者: {data.owner}";
        }
        else
        {
            stateNameLabel.text = "未登録の領域";
            populationLabel.text = "人口: --";
            ownerLabel.text = "所有者: --";
        }
    }
}