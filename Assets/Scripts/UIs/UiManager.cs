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
    [SerializeField] Text durabilityPercentText;
    [SerializeField] Image durabilityGauge;
    [SerializeField] Text shieldPercentText;
    [SerializeField] Image shieldGauge;
    [SerializeField] Text levelText;
    [SerializeField] Image expGage;

    [Header("upgrade ui")]
    [SerializeField] UpgradeButtons upgradeButtons;    
    [SerializeField] Text upgradePointText;
    [SerializeField] Text combatUpgradePointText;

    public List<UpgradeButtonUI> UpgradeButtonUIList => upgradeButtons.UpgradeButtonUIList;

    [Header("game over ui")]
    [SerializeField] Text overTimeText;
    [SerializeField] Text overScoreText;

    [Header("help ui")]
    [SerializeField] Text helpText;
    [SerializeField] Text upgradeHelpText;

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

    public bool OnHelp => onHelp;
    bool onHelp;

    private void Start()
    {
        onHelp = helpText.gameObject.activeSelf;
    }

    public void SetScoreText(int score)
    {
        string str = "SCORE : " + score.ToString("000,000");
        scoreText.text = str;
        overScoreText.text = str;
    }

    public void SetTimeRecordText(int score)
    {
        string str = "TIME : " + (score / 60).ToString("00") + ":" + (score % 60).ToString("00");

        timeRecordText.text = str;
        overTimeText.text = str;
    }

    public void SetDurabilityUI(int currDurability, int maxDurability)
    {
        durabilityPercentText.text = Mathf.Floor((float)currDurability / maxDurability * 100) + "%";
        durabilityGauge.fillAmount = (float)currDurability / maxDurability;
    }

    public void SetShieldUI(float currShield, float maxShield)
    {
        if (shieldGauge != null)
        {
            shieldGauge.fillAmount = maxShield > 0 ? currShield / maxShield : 0;
        }

        if (shieldPercentText != null)
        {
            shieldPercentText.text = maxShield > 0 ? Mathf.Floor(currShield / maxShield * 100) + "%" : "0%";
        }
    }

    public void SetDurabilityAndShieldUI(float currDurability, float maxDurability, float currShield, float maxShield)
    {
        // 내구도
        durabilityPercentText.text = Mathf.Floor(currDurability / maxDurability * 100) + "%";
        durabilityGauge.fillAmount = currDurability / maxDurability;

        // 실드
        SetShieldUI(currShield, maxShield);
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

    public void ToggleHelpUI(bool active)
    {
        helpText.gameObject.SetActive(active);
        upgradeHelpText.gameObject.SetActive(active);
        onHelp = active;
    }

    public void ToggleUpgradeUI(bool active)
    {
        upgradePanel.gameObject.SetActive(active);
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
            upgradeHelpText.enabled = true;
            combatUpgradePointText.enabled = true;
            combatUpgradePointText.text = "+" + point;            
        }
        else
        {
            upgradeHelpText.enabled = false;
            combatUpgradePointText.enabled = false;            
        }        
    }

    public void ToggleCustomCursor(bool active)
    {
        Cursor.visible = !active;
    }

    public void CreateText(string str, bool onMousePos = false)
    {
        Text txt = Instantiate(floatingText, floatTextRoot).GetComponent<Text>();
        txt.text = str;

        if (onMousePos && Mouse.current != null)
        {
            txt.rectTransform.position = Mouse.current.position.ReadValue();
        }
    }

    public void CreateText(string str, Vector3 worldPos)
    {
        Text txt = Instantiate(floatingText, floatTextRoot).GetComponent<Text>();
        txt.text = str;

        txt.rectTransform.position = Camera.main.WorldToScreenPoint(worldPos);
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
