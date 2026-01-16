using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class UiManager : MonoSingleton<UiManager>
{
    [SerializeField] RectTransform root;

    [Header("combat ui")]
    [SerializeField] Text scoreText;
    [SerializeField] Text timeRecordText;
    [SerializeField] DynamicGauge durabilityGauge;  // 내구도 게이지
    [SerializeField] DynamicGauge shieldGauge;      // 실드 게이지
    [SerializeField] Text levelText;
    [SerializeField] Image expGage;

    [Header("upgrade ui")]
    [SerializeField] UpgradeButtons upgradeButtons;
    [SerializeField] Text upgradePointText;
    [SerializeField] Text combatUpgradePointText;
    [SerializeField] Button combatUpgradeButton;  // 전투 중 업그레이드 화면 열기 버튼
    [SerializeField] Text combatUpgradeButtonText;  // 전투 중 업그레이드 버튼의 텍스트

    public List<UpgradeButtonUI> UpgradeButtonUIList => upgradeButtons.UpgradeButtonUIList;

    [Header("game over ui")]
    [SerializeField] Text overTimeText;
    [SerializeField] Text overScoreText;

    [Header("debug ui")]
    [SerializeField] Text waveDebugText;

    [Header("Panels")]
    [SerializeField] RectTransform upgradePanel;
    [SerializeField] RectTransform settingsPanel;
    [SerializeField] RectTransform titlePanel;
    [SerializeField] RectTransform combatPanel;
    [SerializeField] RectTransform gameOverPanel;
    [SerializeField] RectTransform floatTextRoot;


    [Header("volumes")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    public Slider BgmSlider => bgmSlider;
    public Slider SfxSlider => sfxSlider;

    [Header("prefab")]
    [SerializeField] GameObject floatingText;
    [SerializeField] Canvas floatTextCanvas;  // 플로팅 텍스트용 캔버스 (좌표 변환용)


    // 내구도/실드 UI 초기화 (PlayerShip에서 호출)
    public void InitializeDurabilityUI()
    {                        
        durabilityGauge.Initialize();        
        shieldGauge.Initialize();
    }

    private void Start()
    {
        // 업그레이드 버튼 이벤트 등록
        if (combatUpgradeButton != null)
        {
            combatUpgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            // 초기에는 비활성화
            combatUpgradeButton.gameObject.SetActive(false);
        }
    }

    public void SetScoreText(int score)
    {
        string str = score.ToString("000,000"); // "SCORE : " + 
        scoreText.text = str;
        overScoreText.text = str;
    }

    public void SetTimeRecordText(int score)
    {
        string str = "TIME : " + (score / 60).ToString("00") + ":" + (score % 60).ToString("00");

        timeRecordText.text = str;
        overTimeText.text = str;
    }

    public void SetDurabilityAndShieldUI(float currDurability, float maxDurability, float currShield, float maxShield)
    {
        durabilityGauge.UpdateGauge(currDurability, maxDurability);
        shieldGauge.UpdateGauge(currShield, maxShield);            
    }

    public void SetCanvas(GameState state)
    {
        titlePanel.gameObject.SetActive(false);
        combatPanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);

        switch (state)
        {
            case GameState.OnTitle:
                titlePanel.gameObject.SetActive(true);
                break;
            case GameState.OnCombat:
                combatPanel.gameObject.SetActive(true);
                break;
            case GameState.GameOver:
                gameOverPanel.gameObject.SetActive(true);
                break;
        }        
    }


    public void ToggleUpgradeUI(bool active)
    {
        upgradePanel.gameObject.SetActive(active);
    }

    // 업그레이드 화면 열기 버튼 콜백 (전투 중 업그레이드 포인트 클릭 시)
    public void OnUpgradeButtonClicked()
    {
        if (GameManager.Instance.GameState == GameState.OnCombat)
        {
            GameManager.Instance.ToggleUpgradeState(true);
        }
    }

    public void ToggleSettingsUI(bool active)
    {
        settingsPanel.gameObject.SetActive(active);
    }

    public void ToggleGameOverUI(bool active)
    {
        gameOverPanel.gameObject.SetActive(active);
    }

    public void SetLevelText(int level)
    {
        levelText.text = "LV." + level.ToString("D2");
    }

    public void SetExpGage(float ratio)
    {
        expGage.fillAmount = ratio;
    }

    public void SetUpgradePointText(int point)
    {
        upgradePointText.text = "point : " + point.ToString("D2");

        if (point > 0)
        {
            combatUpgradePointText.enabled = true;
            combatUpgradePointText.text = "+" + point;
            SetCombatUpgradeButtonText(point);
            ToggleCombatUpgradeButton(true);
        }
        else
        {
            combatUpgradePointText.enabled = false;
            ToggleCombatUpgradeButton(false);
        }
    }

    public void SetWaveDebugText(int waveNumber, bool isSpawning, int remainingEnemies)
    {
        if (waveDebugText != null)
        {
            waveDebugText.text = $"Wave: {waveNumber} | Spawning: {(isSpawning ? "Yes" : "No")} | Enemies: {remainingEnemies}";
        }
    }

    public void SetCombatUpgradeButtonText(int point)
    {
        if (combatUpgradeButtonText == null)
        {
            Debug.LogError("combatUpgradeButtonText is null!", this);
            return;
        }

        combatUpgradeButtonText.text = $"Left UPGRADE! +{point}";
    }

    public void ToggleCombatUpgradeButton(bool active)
    {
        if (combatUpgradeButton == null)
        {
            Debug.LogError("combatUpgradeButton is null!", this);
            return;
        }

        combatUpgradeButton.gameObject.SetActive(active);
    }

    public void ToggleCustomCursor(bool active)
    {
        Cursor.visible = !active;
    }

    public void CreateText(string str)
    {
        Text txt = Instantiate(floatingText, floatTextRoot).GetComponent<Text>();
        txt.text = str;
    }

    public void CreateText(string str, Vector2 screenPos)
    {
        Text txt = Instantiate(floatingText, floatTextRoot).GetComponent<Text>();
        txt.text = str;

        // 스크린 좌표를 Canvas 로컬 좌표로 변환 (Render Mode에 따라 카메라 설정)
        Camera canvasCamera = floatTextCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : floatTextCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            floatTextRoot,
            screenPos,
            canvasCamera,
            out Vector2 localPos
        );
        txt.rectTransform.anchoredPosition = localPos;
    }

    public void CreateText(string str, Vector3 worldPos)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        CreateText(str, screenPos);
    }

    public void ShakeUI(float _amount = 10f, float _duration = 0.2f)
    {
        IEnumerator ShakeCr(float _amount, float _duration)
        {
            float timer = 0;
            while (timer <= _duration)
            {
                root.anchoredPosition = (Vector3)Random.insideUnitCircle * _amount;

                timer += Time.unscaledDeltaTime;
                //yield return new WaitForSeconds(0.1f);
                yield return null;
            }
            root.anchoredPosition = Vector2.zero;
        }

        StopAllCoroutines();
        StartCoroutine(ShakeCr(_amount, _duration));
    }
}
