using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// CSV 파일을 읽어서 Dictionary 리스트로 변환하는 유틸리티
/// </summary>
public static class CsvReader
{
    /// <summary>
    /// CSV 텍스트를 파싱하여 Dictionary 리스트로 변환
    /// 첫 번째 줄은 헤더로 간주
    /// </summary>
    /// <param name="csvText">CSV 텍스트</param>
    /// <returns>각 행을 Dictionary로 변환한 리스트</returns>
    public static List<Dictionary<string, string>> Parse(string csvText)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogWarning("[CsvReader] Empty CSV text");
            return result;
        }

        // 줄 분리
        string[] lines = csvText.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogWarning("[CsvReader] CSV has no data rows");
            return result;
        }

        // 헤더 파싱
        string[] headers = SplitCsvLine(lines[0]);

        // 데이터 행 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // 빈 줄 건너뛰기
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = SplitCsvLine(line);

            // 헤더 개수와 값 개수가 다르면 경고
            if (values.Length != headers.Length)
            {
                Debug.LogWarning($"[CsvReader] Line {i + 1} has {values.Length} values but expected {headers.Length}");
            }

            // Dictionary 생성
            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                row[headers[j]] = values[j];
            }

            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// CSV 한 줄을 콤마로 분리 (간단한 구현, 따옴표 이스케이프 미지원)
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',')
            .Select(s => s.Trim())
            .ToArray();
    }

    /// <summary>
    /// TextAsset에서 CSV 파싱
    /// </summary>
    public static List<Dictionary<string, string>> Parse(TextAsset csvAsset)
    {
        if (csvAsset == null)
        {
            Debug.LogError("[CsvReader] CSV TextAsset is null");
            return new List<Dictionary<string, string>>();
        }

        return Parse(csvAsset.text);
    }

    #region CSV Loader Utility Methods

    /// <summary>
    /// CSV 파일 로드 및 파싱 (로더용)
    /// </summary>
    public static List<Dictionary<string, string>> LoadCsvRows(TextAsset csvAsset, string loaderName)
    {
        if (csvAsset == null)
        {
            Debug.LogError($"[{loaderName}] CSV asset is null");
            return new List<Dictionary<string, string>>();
        }

        List<Dictionary<string, string>> rows = Parse(csvAsset);

        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning($"[{loaderName}] CSV is empty or failed to parse");
            return new List<Dictionary<string, string>>();
        }

        return rows;
    }

    /// <summary>
    /// 필수 컬럼 존재 여부 확인
    /// </summary>
    public static bool ValidateRequiredColumns(Dictionary<string, string> row, string[] requiredColumns, string loaderName)
    {
        foreach (string column in requiredColumns)
        {
            if (!row.ContainsKey(column))
            {
                Debug.LogWarning($"[{loaderName}] Row missing required column: {column}");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// float 파싱 (실패 시 경고 로그)
    /// </summary>
    public static bool TryParseFloat(string value, string fieldName, string loaderName, out float result)
    {
        if (float.TryParse(value, out result))
        {
            return true;
        }

        Debug.LogWarning($"[{loaderName}] Invalid float value for {fieldName}: {value}");
        result = 0f;
        return false;
    }

    /// <summary>
    /// int 파싱 (실패 시 경고 로그)
    /// </summary>
    public static bool TryParseInt(string value, string fieldName, string loaderName, out int result)
    {
        if (int.TryParse(value, out result))
        {
            return true;
        }

        Debug.LogWarning($"[{loaderName}] Invalid int value for {fieldName}: {value}");
        result = 0;
        return false;
    }

    /// <summary>
    /// 로드 완료 로그
    /// </summary>
    public static void LogLoadComplete(int count, string itemName, string loaderName)
    {
        Debug.Log($"[{loaderName}] Loaded {count} {itemName} from CSV");
    }

    #endregion
}
