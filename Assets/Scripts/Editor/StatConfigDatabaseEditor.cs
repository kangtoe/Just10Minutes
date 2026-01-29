using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// StatConfigDatabase의 인스펙터를 개선하여 각 스탯을 명확하게 표시
/// </summary>
[CustomEditor(typeof(StatConfigDatabase))]
public class StatConfigDatabaseEditor : Editor
{
    private bool showStats = true;
    private Dictionary<UpgradeField, bool> foldouts = new Dictionary<UpgradeField, bool>();
    private bool groupByCategory = true;

    public override void OnInspectorGUI()
    {
        StatConfigDatabase database = (StatConfigDatabase)target;
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("스탯 설정 데이터베이스", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 옵션
        groupByCategory = EditorGUILayout.Toggle("카테고리별 그룹화", groupByCategory);
        EditorGUILayout.Space(5);

        // 통계 정보
        DrawStatistics(database);
        EditorGUILayout.Space(10);

        // 유틸리티 버튼들
        DrawUtilityButtons(database);
        EditorGUILayout.Space(10);

        // 스탯 목록
        showStats = EditorGUILayout.Foldout(showStats, $"스탯 목록 ({database.allStats.Count}개)", true);
        if (showStats)
        {
            if (groupByCategory)
            {
                DrawStatsByCategory(database);
            }
            else
            {
                DrawStatsFlat(database);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawStatistics(StatConfigDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("통계", EditorStyles.boldLabel);

        int totalStats = database.allStats.Count;
        int totalEnumFields = System.Enum.GetValues(typeof(UpgradeField)).Length;
        int upgradeableStats = database.allStats.Count(s => s != null && s.maxLevel > 0);

        // 중복 체크
        var duplicates = database.allStats
            .Where(s => s != null)
            .GroupBy(s => s.field)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        // 누락된 필드 체크
        var existingFields = new HashSet<UpgradeField>(
            database.allStats.Where(s => s != null).Select(s => s.field)
        );
        var allFields = System.Enum.GetValues(typeof(UpgradeField)).Cast<UpgradeField>();
        var missingFields = allFields.Where(f => !existingFields.Contains(f)).ToList();

        EditorGUILayout.LabelField($"총 스탯 개수: {totalStats} / {totalEnumFields}");
        EditorGUILayout.LabelField($"업그레이드 가능: {upgradeableStats}개");

        if (duplicates.Count > 0)
        {
            EditorGUILayout.HelpBox($"⚠️ 중복된 필드: {string.Join(", ", duplicates)}", MessageType.Error);
        }

        if (missingFields.Count > 0)
        {
            EditorGUILayout.HelpBox($"누락된 필드: {missingFields.Count}개", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ 모든 필드가 등록되었습니다", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawUtilityButtons(StatConfigDatabase database)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("누락된 필드 자동 추가"))
        {
            AddMissingFields(database);
        }

        if (GUILayout.Button("중복 제거"))
        {
            RemoveDuplicates(database);
        }

        if (GUILayout.Button("필드 순서로 정렬"))
        {
            SortByField(database);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatsFlat(StatConfigDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        for (int i = 0; i < database.allStats.Count; i++)
        {
            StatConfig stat = database.allStats[i];
            if (stat == null)
            {
                EditorGUILayout.HelpBox($"Index {i}: null 항목", MessageType.Error);
                continue;
            }

            DrawStatItem(database, stat, i);
        }

        EditorGUILayout.EndVertical();

        // 새 스탯 추가 버튼
        if (GUILayout.Button("+ 새 스탯 추가"))
        {
            AddNewStat(database);
        }
    }

    private void DrawStatsByCategory(StatConfigDatabase database)
    {
        var categories = new[] { "Survival", "Shooting", "Impact", "Mobility" };

        foreach (var category in categories)
        {
            var statsInCategory = database.allStats
                .Where(s => s != null && s.category == category)
                .ToList();

            if (statsInCategory.Count == 0)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📁 {category} ({statsInCategory.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (var stat in statsInCategory)
            {
                int index = database.allStats.IndexOf(stat);
                DrawStatItem(database, stat, index);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // 카테고리 없는 항목들
        var uncategorized = database.allStats
            .Where(s => s != null && !categories.Contains(s.category))
            .ToList();

        if (uncategorized.Count > 0)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📁 기타 ({uncategorized.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (var stat in uncategorized)
            {
                int index = database.allStats.IndexOf(stat);
                DrawStatItem(database, stat, index);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawStatItem(StatConfigDatabase database, StatConfig stat, int index)
    {
        if (!foldouts.ContainsKey(stat.field))
        {
            foldouts[stat.field] = false;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 헤더: 필드 이름 + 기본 정보
        EditorGUILayout.BeginHorizontal();

        foldouts[stat.field] = EditorGUILayout.Foldout(
            foldouts[stat.field],
            $"{stat.field} - {stat.displayName}",
            true
        );

        // 삭제 버튼
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("스탯 삭제",
                $"{stat.field}를 삭제하시겠습니까?", "삭제", "취소"))
            {
                Undo.RecordObject(database, "Remove Stat");
                database.allStats.RemoveAt(index);
                EditorUtility.SetDirty(database);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // 세부 내용
        if (foldouts[stat.field])
        {
            EditorGUI.indentLevel++;

            SerializedProperty statProperty = serializedObject.FindProperty("allStats").GetArrayElementAtIndex(index);

            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("field"));
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("category"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("기본값 및 타입", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("defaultValue"));
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("unit"));
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("isInteger"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("업그레이드 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("incrementPerLevel"));
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("maxLevel"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("추가 정보", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("description"));
            EditorGUILayout.PropertyField(statProperty.FindPropertyRelative("icon"));

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void AddMissingFields(StatConfigDatabase database)
    {
        Undo.RecordObject(database, "Add Missing Fields");

        var existingFields = new HashSet<UpgradeField>(
            database.allStats.Where(s => s != null).Select(s => s.field)
        );

        var allFields = System.Enum.GetValues(typeof(UpgradeField)).Cast<UpgradeField>();
        int addedCount = 0;

        foreach (var field in allFields)
        {
            if (!existingFields.Contains(field))
            {
                var newStat = new StatConfig
                {
                    field = field,
                    displayName = GetDefaultDisplayName(field),
                    category = GetDefaultCategory(field),
                    defaultValue = 0,
                    unit = "",
                    isInteger = false,
                    incrementPerLevel = 1,
                    maxLevel = 10,
                    description = ""
                };

                database.allStats.Add(newStat);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(database);
            Debug.Log($"[StatConfigDatabase] {addedCount}개의 누락된 필드를 추가했습니다.");
        }
        else
        {
            Debug.Log("[StatConfigDatabase] 누락된 필드가 없습니다.");
        }
    }

    private void RemoveDuplicates(StatConfigDatabase database)
    {
        Undo.RecordObject(database, "Remove Duplicates");

        var seen = new HashSet<UpgradeField>();
        var toRemove = new List<int>();

        for (int i = 0; i < database.allStats.Count; i++)
        {
            var stat = database.allStats[i];
            if (stat == null)
            {
                toRemove.Add(i);
                continue;
            }

            if (seen.Contains(stat.field))
            {
                toRemove.Add(i);
            }
            else
            {
                seen.Add(stat.field);
            }
        }

        // 역순으로 제거
        for (int i = toRemove.Count - 1; i >= 0; i--)
        {
            database.allStats.RemoveAt(toRemove[i]);
        }

        if (toRemove.Count > 0)
        {
            EditorUtility.SetDirty(database);
            Debug.Log($"[StatConfigDatabase] {toRemove.Count}개의 중복 항목을 제거했습니다.");
        }
        else
        {
            Debug.Log("[StatConfigDatabase] 중복 항목이 없습니다.");
        }
    }

    private void SortByField(StatConfigDatabase database)
    {
        Undo.RecordObject(database, "Sort Stats");
        database.allStats.Sort((a, b) => a.field.CompareTo(b.field));
        EditorUtility.SetDirty(database);
        Debug.Log($"[StatConfigDatabase] {database.allStats.Count}개의 스탯을 필드 순서로 정렬했습니다.");
    }

    private void AddNewStat(StatConfigDatabase database)
    {
        Undo.RecordObject(database, "Add New Stat");

        var newStat = new StatConfig
        {
            field = UpgradeField.MaxDurability,
            displayName = "새 스탯",
            category = "Survival",
            defaultValue = 0,
            unit = "",
            isInteger = false,
            incrementPerLevel = 1,
            maxLevel = 10,
            description = ""
        };

        database.allStats.Add(newStat);
        EditorUtility.SetDirty(database);
    }

    private string GetDefaultDisplayName(UpgradeField field)
    {
        switch (field)
        {
            case UpgradeField.MaxDurability: return "최대 내구도";
            case UpgradeField.MaxShield: return "최대 실드";
            case UpgradeField.ShieldRegenRate: return "실드 재생 속도";
            case UpgradeField.ShieldRegenDelay: return "실드 재생 지연";
            case UpgradeField.DurabilityRegenRate: return "내구도 재생 속도";
            case UpgradeField.DurabilityRegenDelay: return "내구도 재생 지연";
            case UpgradeField.FireRate: return "연사 속도";
            case UpgradeField.ProjectileDamage: return "발사체 피해";
            case UpgradeField.ProjectileSpeed: return "발사체 속도";
            case UpgradeField.MultiShot: return "멀티샷";
            case UpgradeField.OnImpact: return "충돌 피해";
            case UpgradeField.ImpactResist: return "충돌 저항";
            case UpgradeField.MoveSpeed: return "이동 속도";
            case UpgradeField.RotateSpeed: return "회전 속도";
            case UpgradeField.Mass: return "기체 질량";
            default: return field.ToString();
        }
    }

    private string GetDefaultCategory(UpgradeField field)
    {
        if (field <= UpgradeField.DurabilityRegenDelay)
            return "Survival";
        else if (field <= UpgradeField.MultiShot)
            return "Shooting";
        else if (field <= UpgradeField.ImpactResist)
            return "Impact";
        else
            return "Mobility";
    }
}
