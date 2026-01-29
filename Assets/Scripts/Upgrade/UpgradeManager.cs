using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 업그레이드 옵션 (UI 표시용)
/// </summary>
public class UpgradeOption
{
    public string upgradeId;           // 업그레이드 ID
    public string displayName;         // 표시 이름
    public string description;         // 설명
    public int currentLevel;           // 현재 레벨
    public int maxLevel;               // 최대 레벨
    public UpgradeConfig config;       // 원본 설정 참조

    public UpgradeOption(UpgradeConfig config, int currentLevel)
    {
        this.config = config;
        this.upgradeId = config.upgradeId;
        this.displayName = config.displayName;
        this.currentLevel = currentLevel;
        this.maxLevel = config.maxLevel;
        this.description = BuildDescription(config, currentLevel);
    }

    private string BuildDescription(UpgradeConfig config, int currentLevel)
    {
        // 레벨 정보
        string levelInfo = $"Lv.{currentLevel}/{config.maxLevel}";

        // 유니크 업그레이드면 간단히 설명만
        if (config.isUnique)
        {
            return $"{config.description}\n{levelInfo}";
        }

        // 일반 업그레이드면 스탯 증가량 표시
        var statChanges = new List<string>();
        foreach (var modifier in config.statModifiers)
        {
            StatMetadata metadata = StatMetadataRegistry.Get(modifier.field);
            if (metadata != null)
            {
                string sign = modifier.valuePerLevel > 0 ? "+" : "";
                string formattedValue = metadata.FormatValue(modifier.valuePerLevel);
                string unit = metadata.unit;
                statChanges.Add($"{metadata.displayName} {sign}{formattedValue}{unit}");
            }
        }

        string statChangesStr = string.Join("\n", statChanges);
        return $"{statChangesStr}\n{levelInfo}";
    }
}

public class UpgradeManager : MonoSingleton<UpgradeManager>
{
    [Header("=== Upgrade Databases ===")]
    [Tooltip("StatConfigDatabase SO (필수) - 스탯 메타데이터")]
    [SerializeField] private StatConfigDatabase statDatabase;

    [Tooltip("UpgradeDatabase SO (필수) - 업그레이드 정의")]
    [SerializeField] private UpgradeDatabase upgradeDatabase;

    [Header("=== Audio ===")]
    [SerializeField] AudioClip upgradeSound;
    [SerializeField] AudioClip upgradeFailSound;

    [Header("=== UI ===")]
    [SerializeField] Color textHighlight;

    List<UpgradeButtonUI> UpgradeButtons => UiManager.Instance.UpgradeButtonUIList;

    [Header("=== Upgrade State ===")]
    [Tooltip("현재 보유 중인 업그레이드 포인트")]
    public int upgradePoint = 0;

    // 업그레이드별 현재 레벨 추적 (업그레이드 ID → 레벨)
    Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

    // 이미 획득한 유니크 업그레이드 (중복 방지)
    HashSet<string> acquiredUniqueUpgrades = new HashSet<string>();

    // 현재 제시된 업그레이드 옵션들
    List<UpgradeOption> currentOptions = new List<UpgradeOption>();

    void Start()
    {
        // 데이터베이스 검증
        if (statDatabase == null)
        {
            Debug.LogError("[UpgradeManager] StatConfigDatabase가 할당되지 않았습니다!");
            return;
        }

        if (upgradeDatabase == null)
        {
            Debug.LogError("[UpgradeManager] UpgradeDatabase가 할당되지 않았습니다!");
            return;
        }

        // 1. 데이터베이스 초기화
        statDatabase.Initialize();
        upgradeDatabase.Initialize();

        // 2. StatMetadataRegistry에 스탯 메타데이터 등록
        StatMetadataRegistry.InitializeFromStatDatabase(statDatabase);

        // 3. PlayerStatsManager 초기화
        var _ = PlayerStatsManager.Instance;

        // 4. UI 업데이트
        UiManager.Instance.SetUpgradePointText(upgradePoint);

        // 버튼 리스너 등록
        for (int i = 0; i < UpgradeButtons.Count; i++)
        {
            int index = i;
            UpgradeButtons[i].Button.onClick.AddListener(delegate {
                SelectUpgrade(index);
            });
        }

        Debug.Log($"[UpgradeManager] Initialized with {upgradeDatabase.allUpgrades.Count} upgrades");
    }

    void SelectUpgrade(int index)
    {
        // 포인트 체크
        if (upgradePoint < 1)
        {
            ShowError("No Point!");
            return;
        }

        // 인덱스 체크
        if (index < 0 || index >= currentOptions.Count)
        {
            Debug.LogError($"Invalid upgrade index: {index}");
            return;
        }

        UpgradeOption option = currentOptions[index];

        // 최대 레벨 체크
        if (option.currentLevel >= option.maxLevel)
        {
            ShowError("Max Level!");
            return;
        }

        // 업그레이드 적용
        ApplyUpgrade(option);

        // 포인트 차감
        upgradePoint--;
        UiManager.Instance.SetUpgradePointText(upgradePoint);

        SoundManager.Instance.PlaySound(upgradeSound);
        UiManager.Instance.ShakeUI();

        // 포인트가 남아있으면 새로운 옵션 생성, 없으면 창 닫기
        if (upgradePoint > 0)
        {
            GenerateRandomUpgrades();
        }
        else
        {
            GameManager.Instance.ToggleUpgradeState(false);
        }
    }

    void ApplyUpgrade(UpgradeOption option)
    {
        UpgradeConfig config = option.config;

        // 레벨 증가
        if (!upgradeLevels.ContainsKey(config.upgradeId))
            upgradeLevels[config.upgradeId] = 0;

        upgradeLevels[config.upgradeId]++;

        // 유니크 업그레이드면 획득 목록에 추가
        if (config.isUnique)
        {
            acquiredUniqueUpgrades.Add(config.upgradeId);
        }

        // StatModifier 리스트를 순회하며 각 스탯에 증분 적용
        foreach (var modifier in config.statModifiers)
        {
            PlayerStatsManager.Instance.ApplyUpgrade(modifier.field, modifier.valuePerLevel);

            // PlayerShip에 스탯 반영
            GameManager.Instance.PlayerShip.ApplyStatFromPlayerStats(modifier.field);
        }

        Debug.Log($"[UpgradeManager] Applied upgrade: {config.upgradeId} (Lv.{upgradeLevels[config.upgradeId]})");
    }

    void GenerateRandomUpgrades()
    {
        // 선택 가능한 업그레이드 필터링
        List<UpgradeConfig> availableUpgrades = upgradeDatabase.allUpgrades
            .Where(u => u != null && CanSelectUpgrade(u))
            .ToList();

        if (availableUpgrades.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] 선택 가능한 업그레이드가 없습니다!");
            currentOptions.Clear();
            return;
        }

        // 랜덤 셔플
        System.Random rng = new System.Random();
        availableUpgrades = availableUpgrades.OrderBy(x => rng.Next()).ToList();

        // 최대 3개 선택
        int count = Mathf.Min(3, availableUpgrades.Count);
        currentOptions.Clear();

        for (int i = 0; i < count; i++)
        {
            UpgradeConfig config = availableUpgrades[i];
            int currentLevel = upgradeLevels.ContainsKey(config.upgradeId) ? upgradeLevels[config.upgradeId] : 0;
            currentOptions.Add(new UpgradeOption(config, currentLevel));
        }

        // UI 업데이트
        for (int i = 0; i < UpgradeButtons.Count; i++)
        {
            if (i < currentOptions.Count)
            {
                UpdateUpgradeButton(UpgradeButtons[i], currentOptions[i]);
                UpgradeButtons[i].gameObject.SetActive(true);
            }
            else
            {
                UpgradeButtons[i].gameObject.SetActive(false);
            }
        }
    }

    bool CanSelectUpgrade(UpgradeConfig config)
    {
        // 유니크 업그레이드는 이미 획득했으면 선택 불가
        if (config.isUnique && acquiredUniqueUpgrades.Contains(config.upgradeId))
        {
            return false;
        }

        // 현재 레벨 확인
        int currentLevel = upgradeLevels.ContainsKey(config.upgradeId) ? upgradeLevels[config.upgradeId] : 0;

        // 최대 레벨 도달 시 선택 불가
        if (currentLevel >= config.maxLevel)
        {
            return false;
        }

        return true;
    }

    void UpdateUpgradeButton(UpgradeButtonUI btn, UpgradeOption option)
    {
        // 타이틀: 한글 이름
        btn.SetTitle(option.displayName);

        // 설명
        string colorCode = ColorUtility.ToHtmlStringRGBA(textHighlight);
        string desc = $"<color=#{colorCode}>{option.description}</color>";
        btn.SetDesc(desc);
    }

    void ShowError(string message)
    {
        Vector2 touchPos = InputManager.Instance.PointerPosition;
        UiManager.Instance.CreateText(message, touchPos);
        UiManager.Instance.ShakeUI();
        SoundManager.Instance.PlaySound(upgradeFailSound);
    }

    public void PointUp(int amount = 1)
    {
        upgradePoint += amount;
        UiManager.Instance.SetUpgradePointText(upgradePoint);

        // 이미 업그레이드 화면이 열려있으면 옵션만 갱신
        if (GameManager.Instance.GameState == GameState.OnUpgrade)
        {
            GenerateRandomUpgrades();
            return;
        }

        // 업그레이드 화면 열기
        GenerateRandomUpgrades();
        GameManager.Instance.ToggleUpgradeState(true);
    }

    /// <summary>
    /// 디버그: 특정 업그레이드의 현재 레벨 조회
    /// </summary>
    public int GetUpgradeLevel(string upgradeId)
    {
        return upgradeLevels.ContainsKey(upgradeId) ? upgradeLevels[upgradeId] : 0;
    }

    /// <summary>
    /// 디버그: 특정 업그레이드 획득 여부
    /// </summary>
    public bool HasUpgrade(string upgradeId)
    {
        return upgradeLevels.ContainsKey(upgradeId) && upgradeLevels[upgradeId] > 0;
    }
}
