using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TaskData
{
    public int id;
    public string requesterName;
    public string faceImageKey; // 画像を特定するためのキー
    public string title;
    public string message;
    public string requirement;
}

[System.Serializable]
public class TaskDataList
{
    public TaskData[] tasks;
}

public class TaskDataManager : MonoBehaviour
{
    public static TaskDataManager Instance;
    public TextAsset jsonFile;
    private List<TaskData> taskList = new List<TaskData>();

    void Awake()
    {
        if (Instance == null) { Instance = this; LoadData(); }
        else Destroy(gameObject);
    }

    private void LoadData()
    {
        if (jsonFile != null)
        {
            TaskDataList data = JsonUtility.FromJson<TaskDataList>(jsonFile.text);
            taskList.AddRange(data.tasks);
            Debug.Log($"[TaskData] {taskList.Count}件のタスクを読み込みました。");
        }
    }

    public TaskData GetTaskById(int id)
    {
        return taskList.Find(t => t.id == id);
    }
}