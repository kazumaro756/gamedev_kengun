using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum TaskState
{
    New,       // 未受諾
    Accepted,  // 受諾済み
    Completed  // 納品済み
}

[System.Serializable]
public class TaskData
{
    public int id;
    public string requesterName;
    public string faceImageKey;
    public string title;
    public string message;
    public string requirement;
    public TaskState state = TaskState.New; // 初期状態は未受諾
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

    public System.Action<int> OnTaskDelivered;

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
            taskList.Clear();
            taskList.AddRange(data.tasks);
            Debug.Log($"[TaskData] {taskList.Count}件のタスクを読み込みました。");
        }
    }

    public TaskData GetTaskById(int id)
    {
        return taskList.Find(t => t.id == id);
    }

    // 【追加】エラー解消のために必要なメソッド
    public List<TaskData> GetAllTasks()
    {
        return taskList;
    }

    // 特定のステータスのタスク一覧を取得
    public List<TaskData> GetTasksByState(TaskState state)
    {
        return taskList.FindAll(t => t.state == state);
    }

    // タスクを受諾する
    public void AcceptTask(int id)
    {
        var task = GetTaskById(id);
        if (task != null && task.state == TaskState.New)
        {
            task.state = TaskState.Accepted;
            Debug.Log($"タスク [{task.title}] を受諾しました。");
        }
    }

    // タスクを納品する
    public void DeliverTask(int id)
    {
        var task = GetTaskById(id);
        if (task != null && task.state == TaskState.Accepted)
        {
            task.state = TaskState.Completed;
            Debug.Log($"タスク [{task.title}] を納品しました。");
            
            // 納品されたことを通知する（報酬システム等はGameManagerが担当する）
            OnTaskDelivered?.Invoke(id);
        }
    }
}