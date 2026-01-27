using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeRecordManager : MonoSingleton<TimeRecordManager>
{
    [Header("Debug Settings")]
    [SerializeField] private float startTime = 0f; // 게임 시작 시 초기 시간 (디버깅용)

    float timeRecord;
    public float TimeRecord => timeRecord;

    bool canCount = false;

    public override bool Initialize()
    {
        if (!base.Initialize()) return false;

        // 초기 시간 설정 (디버깅용)
        timeRecord = startTime;

        if (startTime > 0)
        {
            Debug.Log($"[TimeRecordManager] Starting at {startTime} seconds for debugging");
        }

        // UI 초기화
        UiManager.Instance.SetTimeRecordText((int)timeRecord);

        return true;
    }

    // Update is called once per frame
    void Update()
    {
        if (canCount)
        {
            timeRecord += Time.deltaTime;
            UiManager.Instance.SetTimeRecordText((int)TimeRecord);
        }
    }

    public void SetActiveCount(bool _active)
    {
        canCount = _active;
    }
}
