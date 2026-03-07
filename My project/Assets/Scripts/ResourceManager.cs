using UnityEngine;

[System.Serializable]
public class GlobalResourceData
{
    public long funds;
    public int manpower;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;
    public TextAsset jsonFile;
    private GlobalResourceData data;

    void Awake()
    {
        if (Instance == null) { Instance = this; LoadData(); }
        else Destroy(gameObject);
    }

    private void LoadData()
    {
        data = JsonUtility.FromJson<GlobalResourceData>(jsonFile.text);
    }

    public GlobalResourceData GetData() => data;

    // 資源を消費・獲得した際に呼ぶ
    public void AddResources(long fundsDelta, int manpowerDelta)
    {
        data.funds += fundsDelta;
        data.manpower += manpowerDelta;
        UIManager.Instance.UpdateResourceUI(); // UIに即反映
    }
}