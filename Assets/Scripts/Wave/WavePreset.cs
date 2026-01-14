using UnityEngine;

/// <summary>
/// 웨이브 프리셋 ScriptableObject
/// 수작업 웨이브 (보스 웨이브 10, 20, 30)에 사용
/// </summary>
[CreateAssetMenu(fileName = "Wave_", menuName = "프로젝트 커스텀/Wave/Wave Preset", order = 0)]
public class WavePreset : ScriptableObject
{
    [Header("Wave Info")]
    public int waveNumber;

    [TextArea(3, 5)]
    public string description;

    [Header("Spawn Configuration")]
    public SpawnInfo[] spawnInfos;

    /// <summary>
    /// SpawnInfo 배열을 깊은 복사하여 반환 (원본 보존)
    /// </summary>
    public SpawnInfo[] GetSpawnInfos()
    {
        if (spawnInfos == null || spawnInfos.Length == 0)
        {
            Debug.LogWarning($"[WavePreset] Wave {waveNumber} has no spawn infos!");
            return new SpawnInfo[0];
        }

        // 깊은 복사 (원본 보존)
        SpawnInfo[] copy = new SpawnInfo[spawnInfos.Length];
        for (int i = 0; i < spawnInfos.Length; i++)
        {
            copy[i] = new SpawnInfo
            {
                spawnPrefab = spawnInfos[i].spawnPrefab,
                spawnTime = spawnInfos[i].spawnTime,
                spawnSide = spawnInfos[i].spawnSide,
                count = spawnInfos[i].count,
                spawnInterval = spawnInfos[i].spawnInterval
            };
        }
        return copy;
    }

    /// <summary>
    /// 웨이브 정보 요약 출력 (디버깅용)
    /// </summary>
    public void DebugPrintInfo()
    {
        Debug.Log($"=== Wave {waveNumber} Preset ===");
        Debug.Log($"Description: {description}");
        Debug.Log($"Spawn Infos: {spawnInfos?.Length ?? 0}");

        if (spawnInfos != null)
        {
            for (int i = 0; i < spawnInfos.Length; i++)
            {
                var info = spawnInfos[i];
                Debug.Log($"  [{i}] {info.spawnPrefab?.name ?? "null"} x{info.count} @ {info.spawnTime}s ({info.spawnSide})");
            }
        }
    }
}
