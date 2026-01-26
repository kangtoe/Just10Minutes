using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Upgrades.csv 파일을 로드하여 UpgradeData 초기화
/// </summary>
public static class UpgradesLoader
{
    private const string LOADER_NAME = "UpgradesLoader";
    private static readonly string[] REQUIRED_COLUMNS = { "Upgrade", "DisplayName", "Increment", "MaxLevel" };

    /// <summary>
    /// CSV 파일에서 업그레이드 데이터 로드
    /// </summary>
    /// <param name="csvAsset">Upgrades.csv TextAsset</param>
    /// <param name="incrementValues">출력: 증가량 Dictionary</param>
    /// <param name="maxLevels">출력: 최대 레벨 Dictionary</param>
    /// <param name="displayNames">출력: 표시 이름 Dictionary</param>
    public static void LoadFromCsv(
        TextAsset csvAsset,
        out Dictionary<UpgradeField, float> incrementValues,
        out Dictionary<UpgradeField, int> maxLevels,
        out Dictionary<UpgradeField, string> displayNames)
    {
        incrementValues = new Dictionary<UpgradeField, float>();
        maxLevels = new Dictionary<UpgradeField, int>();
        displayNames = new Dictionary<UpgradeField, string>();

        List<Dictionary<string, string>> rows = CsvReader.LoadCsvRows(csvAsset, LOADER_NAME);
        if (rows.Count == 0)
        {
            return;
        }

        foreach (var row in rows)
        {
            // 필수 컬럼 확인
            if (!CsvReader.ValidateRequiredColumns(row, REQUIRED_COLUMNS, LOADER_NAME))
            {
                continue;
            }

            string upgradeName = row["Upgrade"];

            // UpgradeField enum 파싱
            if (!System.Enum.TryParse<UpgradeField>(upgradeName, out UpgradeField field))
            {
                Debug.LogWarning($"[{LOADER_NAME}] Invalid UpgradeField: {upgradeName}");
                continue;
            }

            // DisplayName 가져오기
            string displayName = row["DisplayName"];

            // Increment 파싱 ("+50", "+5/s", "-0.2s" 등)
            string incrementStr = row["Increment"];
            float increment = ParseIncrement(incrementStr);

            // MaxLevel 파싱
            if (!CsvReader.TryParseInt(row["MaxLevel"], "MaxLevel", LOADER_NAME, out int maxLevel))
            {
                continue;
            }

            // Dictionary에 추가
            incrementValues[field] = increment;
            maxLevels[field] = maxLevel;
            displayNames[field] = displayName;

            Debug.Log($"[{LOADER_NAME}] Loaded {upgradeName} ({displayName}): Increment={increment}, MaxLevel={maxLevel}");
        }

        CsvReader.LogLoadComplete(incrementValues.Count, "upgrade entries", LOADER_NAME);
    }

    /// <summary>
    /// Increment 문자열 파싱 ("+50", "+5/s", "-0.2s" 등)
    /// </summary>
    private static float ParseIncrement(string incrementStr)
    {
        if (string.IsNullOrEmpty(incrementStr))
            return 0f;

        // 단위 제거 ("/s", "s", "발" 등)
        incrementStr = incrementStr.Replace("/s", "").Replace("s", "").Replace("발", "").Trim();

        // +/- 부호 포함하여 파싱
        if (float.TryParse(incrementStr, out float value))
        {
            return value;
        }

        Debug.LogWarning($"[UpgradesLoader] Failed to parse increment: {incrementStr}");
        return 0f;
    }
}
