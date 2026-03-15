using UnityEngine;
using System.Collections.Generic;

public enum UnitType
{
    Division = 0, // 師団
    Brigade = 1,  // 旅団
    Regiment = 2, // 連隊
    Battalion = 3,// 大隊
    Company = 4   // 中隊
}

[System.Serializable]
public class UnitData
{
    public string unitId;
    public string parentId; // 階層情報をフラットなリストで表現するためのID
    public string unitName;
    public UnitType type;
    
    public int currentManpower;
    public int maxManpower;
    
    // ノードUI（インタラクティブ編制）上での位置情報
    public float nodePosX;
    public float nodePosY;
    
    // JsonUtilityの深さ制限警告を回避するため、実行時のツリー構造はシリアライズから除外
    [System.NonSerialized]
    public List<UnitData> subUnits = new List<UnitData>();

    // どの構築テンプレート（スロット枠）を持っているかのID
    public string templateId = "";
}

[System.Serializable]
public class UnitDataList
{
    // ルート（師団レベル）のリスト
    public List<UnitData> units = new List<UnitData>();
}

// 部隊の戦闘序列（Order of Battle）を管理するクラス
public class UnitDataManager : MonoBehaviour
{
    public static UnitDataManager Instance;
    public TextAsset jsonFile; // Unity上でUnitData.jsonをアタッチ
    public TextAsset templateJsonFile; // 新規: 編制レシピを含む UnitTemplates.json

    private List<UnitData> rootUnits = new List<UnitData>();
    private Dictionary<string, UnitTemplateConfig> templateDict = new Dictionary<string, UnitTemplateConfig>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadData()
    {
        // 1. テンプレートデータの読み込み
        string templateJsonText = "";
        if (templateJsonFile != null)
        {
            templateJsonText = templateJsonFile.text;
        }
        else
        {
            // インスペクター未設定の場合、Resourcesから直接読み込んでみる
            TextAsset resAsset = Resources.Load<TextAsset>("UnitTemplates");
            if (resAsset != null) templateJsonText = resAsset.text;
        }

        if (!string.IsNullOrEmpty(templateJsonText))
        {
            UnitTemplateConfigList tList = JsonUtility.FromJson<UnitTemplateConfigList>(templateJsonText);
            if (tList != null && tList.templates != null)
            {
                templateDict.Clear();
                foreach (var t in tList.templates)
                {
                    templateDict[t.templateId] = t;
                }
                Debug.Log($"[UnitDataManager] {templateDict.Count} 件の部隊テンプレートを読み込みました。");
            }
        }
        else
        {
            Debug.LogWarning("[UnitDataManager] 部隊テンプレートファイルが見つかりません。");
        }

        // 2. 部隊の実データの読み込み
        string unitJsonText = "";
        if (jsonFile != null)
        {
            unitJsonText = jsonFile.text;
        }
        else
        {
            TextAsset resAsset = Resources.Load<TextAsset>("UnitData");
            if (resAsset != null) unitJsonText = resAsset.text;
        }

        if (!string.IsNullOrEmpty(unitJsonText))
        {
            UnitDataList dataList = JsonUtility.FromJson<UnitDataList>(unitJsonText);
            if (dataList != null && dataList.units != null)
            {
                rootUnits.Clear();
                
                // 1. 全要素をIDで引けるように辞書化
                Dictionary<string, UnitData> unitDict = new Dictionary<string, UnitData>();
                foreach (var unit in dataList.units)
                {
                    if (unit.subUnits == null) unit.subUnits = new List<UnitData>();
                    else unit.subUnits.Clear();
                    
                    unitDict[unit.unitId] = unit;
                }

                // 2. フラットなリストを親子関係に再構築
                foreach (var unit in dataList.units)
                {
                    if (string.IsNullOrEmpty(unit.parentId))
                    {
                        // 親がいない場合はルート（師団レベル）
                        rootUnits.Add(unit);
                    }
                    else
                    {
                        if (unitDict.TryGetValue(unit.parentId, out UnitData parent))
                        {
                            parent.subUnits.Add(unit);
                        }
                        else
                        {
                            // 万が一親が見つからない場合はフォールバックとしてルートに入れる
                            rootUnits.Add(unit);
                        }
                    }
                }
                
                Debug.Log($"[UnitDataManager] {rootUnits.Count} 個のルート部隊（全体リスト数: {dataList.units.Count}）を読み込みました。");
            }
        }
        else
        {
            Debug.LogWarning("[UnitDataManager] jsonFile がアタッチされていません。");
        }
    }

    // ルート（師団レベル）の部隊一覧を取得
    public List<UnitData> GetRootUnits()
    {
        return rootUnits;
    }

    // IDで部隊を再帰的に検索する
    public UnitData FindUnitById(string unitId)
    {
        foreach (var root in rootUnits)
        {
            var found = FindInChildren(root, unitId);
            if (found != null) return found;
        }
        return null;
    }

    private UnitData FindInChildren(UnitData current, string targetId)
    {
        if (current.unitId == targetId) return current;
        foreach (var child in current.subUnits)
        {
            var found = FindInChildren(child, targetId);
            if (found != null) return found;
        }
        return null;
    }

    // --- テンプレート取得用 ---
    public UnitTemplateConfig GetTemplate(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (templateDict.TryGetValue(id, out var tpl)) return tpl;
        return null;
    }

    public List<UnitTemplateConfig> GetAllTemplates()
    {
        return new List<UnitTemplateConfig>(templateDict.Values);
    }

    // キャンバスに新規ドロップされたノードを追加
    public UnitData AddNewUnit(string name, UnitType type, float x, float y, string tplId = "")
    {
        UnitData newUnit = new UnitData
        {
            unitId = System.Guid.NewGuid().ToString(),
            parentId = "", // 新規配置時はルート要素として扱う
            templateId = tplId,
            unitName = name,
            type = type,
            currentManpower = 1000,
            maxManpower = 1000,
            nodePosX = x,
            nodePosY = y,
            subUnits = new List<UnitData>()
        };
        rootUnits.Add(newUnit);
        SaveData();
        return newUnit;
    }

    // 部隊の親子関係（ワイヤーリンク）を構築
    public string LinkUnits(string parentId, string childId)
    {
        UnitData parent = FindUnitById(parentId);
        UnitData child = FindUnitById(childId);

        if (parent == null || child == null) return "部隊が見つかりません。";
        if (parent == child) return "自分自身に接続することはできません。";

        // 1. 階層バリデーション（親のランク > 子のランク である必要がある）
        // UnitTypeは値が小さいほど上位（Division=0, Company=4）
        if ((int)parent.type >= (int)child.type)
        {
            return $"階級エラー: {parent.type} の下に {child.type} を配置することはできません。";
        }

        // 2. テンプレート（レシピ）バリデーション
        if (!string.IsNullOrEmpty(parent.templateId))
        {
            var tpl = GetTemplate(parent.templateId);
            if (tpl != null && tpl.requirements != null && tpl.requirements.Count > 0)
            {
                // その兵科がレシピに含まれているかチェック
                var req = tpl.requirements.Find(r => r.requiredType == child.type);
                if (req == null)
                {
                    return $"編制ルール違反: {parent.unitName} のレシピに {child.type} は含まれていません。";
                }

                // スロット上限（コア＋サポート）のチェック
                int currentCount = parent.subUnits.FindAll(u => u.type == child.type).Count;
                if (currentCount >= (req.coreCount + req.maxSupportCount))
                {
                    return $"スロット不足: {child.type} の最大枠数に達しています。";
                }
            }
        }

        // 3. 循環参照の簡易チェック（子が親の先祖でないか）
        if (IsAncestor(child, parent.unitId))
        {
            return "循環参照エラー: ループする接続はできません。";
        }

        // 全てのバリデーションを通過
        // まず現在の子がどこかに所属していれば外す（ルートからも外す）
        RemoveFromParent(childId);
        
        // 新しい親の子として追加
        child.parentId = parent.unitId;
        parent.subUnits.Add(child);
        SaveData();
        Debug.Log($"[UnitDataManager] 結線成立: {parent.unitName} -> {child.unitName}");
        return ""; // 成功
    }

    private bool IsAncestor(UnitData current, string targetId)
    {
        foreach (var sub in current.subUnits)
        {
            if (sub.unitId == targetId) return true;
            if (IsAncestor(sub, targetId)) return true;
        }
        return false;
    }

    // 指定IDの部隊を親から（またはルートから）切り離す
    private void RemoveFromParent(string targetId)
    {
        // ルートにいる場合
        var rootItem = rootUnits.Find(u => u.unitId == targetId);
        if (rootItem != null) rootItem.parentId = "";
        rootUnits.RemoveAll(u => u.unitId == targetId);

        // 各ルートの子要素を探索して削除
        foreach (var root in rootUnits)
        {
            RemoveFromChildren(root, targetId);
        }
    }

    private void RemoveFromChildren(UnitData parent, string targetId)
    {
        var childItem = parent.subUnits.Find(u => u.unitId == targetId);
        if (childItem != null) childItem.parentId = "";
        
        parent.subUnits.RemoveAll(u => u.unitId == targetId);
        foreach (var child in parent.subUnits)
        {
            RemoveFromChildren(child, targetId);
        }
    }

    // データの保存（エディタ実行時プロトタイプ用としてAssetsに上書き）
    public void SaveData()
    {
#if UNITY_EDITOR
        List<UnitData> flatList = new List<UnitData>();
        
        // 再帰的にすべてのノードを1次元リストに回収する処理
        foreach (var root in rootUnits)
        {
            FlattenUnitRecursive(root, flatList);
        }

        UnitDataList dataList = new UnitDataList { units = flatList };
        string json = JsonUtility.ToJson(dataList, true);
        string path = Application.dataPath + "/Resources/UnitData.json";
        System.IO.File.WriteAllText(path, json);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("[UnitDataManager] データをフラットなリストとして UnitData.json に保存しました。");
#endif
    }
    
    private void FlattenUnitRecursive(UnitData node, List<UnitData> flatList)
    {
        flatList.Add(node);
        foreach (var child in node.subUnits)
        {
            FlattenUnitRecursive(child, flatList);
        }
    }
}
