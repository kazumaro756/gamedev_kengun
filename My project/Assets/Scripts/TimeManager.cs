using UnityEngine;

// 日付とゲーム内のターンを管理するクラス
// ※独立したオブジェクトを作らず、GameManagerと同じオブジェクトにアタッチして実行します。
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    // 初期日付（例：1560年1月1日）
    public int currentYear = 1560;
    public int currentMonth = 1;
    public int currentDay = 1;

    // 日付が変わったことを通知するイベント
    public System.Action<int, int, int> OnDateChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this); // オブジェクト自体は壊さず、重複したスクリプトコンポーネントのみ破棄
        }
    }

    void Start()
    {
        // 初期状態のUI反映のために1度イベントを発火
        OnDateChanged?.Invoke(currentYear, currentMonth, currentDay);
    }

    // 1日進める処理（GameManagerから呼ばれる）
    public void AdvanceDay()
    {
        currentDay++;

        // 各月の日数判定（簡易的に全月30日として計算するか、リアルにするか）
        // ここでは歴史ゲームによくある「1ヶ月=30日」ベースで簡易実装します。
        // ※必要であれば後で変更可能です。
        if (currentDay > 30)
        {
            currentDay = 1;
            currentMonth++;

            if (currentMonth > 12)
            {
                currentMonth = 1;
                currentYear++;
            }
        }

        Debug.Log($"[TimeManager] 日付が進行しました: {currentYear}年 {currentMonth}月 {currentDay}日");
        
        // 日付変更イベントを発行（UIの表示更新などに使われる）
        OnDateChanged?.Invoke(currentYear, currentMonth, currentDay);
    }

    // 現在の日付を文字列で取得するヘルパー
    public string GetDateString()
    {
        return $"{currentYear}年 {currentMonth}月 {currentDay}日";
    }
}
