using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class PlayerShip : MonoBehaviour
{    
    [Header("systems")]
    [SerializeField] ShooterBase shooter;
    [SerializeField] Impactable impactable;
    [SerializeField] Damageable damageable;
    [SerializeField] MoveStandard moveStandard;
    [SerializeField] RotateByInput rotateByInput;

    Rigidbody2D rbody;

    public Damageable Damageable => damageable;

    // MoveStandard와 RotateByInput 컴포넌트가 자동으로 이동/회전 처리
    // ShooterBase 컴포넌트가 자동으로 사격 처리
    // Start is called before the first frame update
    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();

        Initialize();

        // 피해 이벤트 (게임 메카닉용 - 화면 흔들림)
        damageable.onDamaged.AddListener(delegate
        {
            UiManager.Instance.ShakeUI();
        });

        // 값 변경 이벤트 (UI 업데이트용 - 증가/감소 모두 포함)
        damageable.onDurabilityChanged.AddListener(delegate
        {
            UpdateDurabilityUI();
        });

        damageable.onShieldChanged.AddListener(delegate
        {
            UpdateDurabilityUI();
        });

        damageable.onDead.AddListener(delegate
        {
            GameManager.Instance.GameOver();
        });

        LevelManager.Instance.onLevelUp.AddListener(delegate {
            UiManager.Instance.CreateText("Level Up!", transform.position);
        });
    }

    // PlayerShip 초기화
    public void Initialize()
    {
        // 1. PlayerShip의 컴포넌트들을 PlayerStats 참조 모드로 초기화
        damageable.Initialize(true);  // PlayerStats 기반으로 최대 체력/실드 설정
        shooter.TogglePlayerStatsReference(true);
        impactable.TogglePlayerStatsReference(true);

        // 2. UI 게이지 초기화
        UiManager.Instance.InitializeDurabilityUI();

        // 3. 초기 UI 갱신
        UpdateDurabilityUI();

        // 4. Rigidbody2D.mass를 PlayerStats.mass 값으로 강제 설정
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"[PlayerShip] Rigidbody2D가 없습니다!");
        }
        else
        {
            float expectedMass = PlayerStats.Instance.mass;
            if (Mathf.Abs(rb.mass - expectedMass) > 0.001f)
            {
                Debug.LogWarning($"[PlayerShip] Rigidbody2D.mass를 직접 수정하지 마세요! PlayerStats.mass({expectedMass})로 복원합니다.");
                rb.mass = expectedMass;
            }
        }

        // 5. 이동/회전 속도를 PlayerStats 값으로 설정
        moveStandard.SetMovePower(PlayerStats.Instance.moveSpeed);
        rotateByInput.SetRotationSpeed(PlayerStats.Instance.rotateSpeed);
    }



    public void UpdateDurabilityUI()
    {
        float currDurability = damageable.CurrDurability;
        float maxDurability = damageable.MaxDurability;
        float currShield = damageable.CurrShield;
        float maxShield = damageable.MaxShield;

        UiManager.Instance.SetDurabilityAndShieldUI(currDurability, maxDurability, currShield, maxShield);
    }

    public void InitShip(bool stackFull = false)
    {
        if(stackFull) UiManager.Instance.CreateText("Restore All!", transform.position);

        damageable.InitDurability();
        UpdateDurabilityUI();
    }

    // PlayerStats의 값을 컴포넌트에 반영 (업그레이드 시 사용)
    public void ApplyStatFromPlayerStats(UpgradeField field)
    {
        switch (field)
        {
            case UpgradeField.MaxDurability:
                damageable.SetMaxDurability(PlayerStats.Instance.maxDurability, true);
                UpdateDurabilityUI();
                break;
            case UpgradeField.MaxShield:
                damageable.SetMaxShield(PlayerStats.Instance.maxShield, true);
                UpdateDurabilityUI();
                break;
            case UpgradeField.ShieldRegenRate:
                damageable.SetShieldRegenRate(PlayerStats.Instance.shieldRegenRate);
                break;
            case UpgradeField.ShieldRegenDelay:
                damageable.SetShieldRegenDelay(PlayerStats.Instance.shieldRegenDelay);
                break;
            case UpgradeField.OnImpact:
                impactable.SetDamageAmount(PlayerStats.Instance.onImpact);
                break;
            case UpgradeField.MultiShot:
                shooter.SetMultiShot(PlayerStats.Instance.multiShot);
                break;
            case UpgradeField.MoveSpeed:
                moveStandard.SetMovePower(PlayerStats.Instance.moveSpeed);
                break;
            case UpgradeField.RotateSpeed:
                rotateByInput.SetRotationSpeed(PlayerStats.Instance.rotateSpeed);
                break;
        }
    }

    public void SetSystem(UpgradeField _type, float amount)
    {
        switch (_type)
        {
            // 생존
            case UpgradeField.MaxDurability:
                damageable.SetMaxDurability(amount, true);
                UpdateDurabilityUI();
                break;
            case UpgradeField.MaxShield:
                damageable.SetMaxShield(amount, true);
                UpdateDurabilityUI();
                break;
            case UpgradeField.ShieldRegenRate:
                damageable.SetShieldRegenRate(amount);
                break;
            case UpgradeField.ShieldRegenDelay:
                damageable.SetShieldRegenDelay(amount);
                break;

            // 충돌
            case UpgradeField.OnImpact:
                impactable.SetDamageAmount(amount);
                break;

            // 사격
            case UpgradeField.MultiShot:
                shooter.SetMultiShot((int)amount);
                break;

            // 이동
            case UpgradeField.MoveSpeed:
                moveStandard.SetMovePower(amount);
                break;
            case UpgradeField.RotateSpeed:
                rotateByInput.SetRotationSpeed(amount);
                break;
        }
    }

}
