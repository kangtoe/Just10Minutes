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

    // MoveStandard와 RotateByInput 컴포넌트가 자동으로 이동/회전 처리
    // ShooterBase 컴포넌트가 자동으로 사격 처리

    // Start is called before the first frame update
    void Start()
    {
        UpdateDurabilityUI();

        damageable.onDamaged.AddListener(delegate
        {
            UpdateDurabilityUI();
            UiManager.Instance.ShakeUI();
        });

        damageable.onShieldDamaged.AddListener(delegate
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


    void UpdateDurabilityUI()
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
        }
    }
}
