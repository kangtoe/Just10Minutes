using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    public GameObject diePrefab;

    [HideInInspector]
    public UnityEvent onDead = new UnityEvent();

    [HideInInspector]
    public UnityEvent onDamaged = new UnityEvent();

    [HideInInspector]
    public UnityEvent onDurabilityChanged = new UnityEvent();  // 내구도 변경 시 (증가/감소 모두)

    [HideInInspector]
    public UnityEvent onShieldDamaged = new UnityEvent();

    [HideInInspector]
    public UnityEvent onShieldChanged = new UnityEvent();  // 실드 변경 시 (증가/감소 모두)

    [HideInInspector]
    public UnityEvent onShieldDepleted = new UnityEvent();

    // PlayerStats 싱글톤에서 스탯 참조 여부 (플레이어용: true, 적용: false)
    bool usePlayerStats = false;

    [Header("Durability")]
    [SerializeField, ReadOnly] float maxDurability = 100;
    [SerializeField, ReadOnly] float currDurability;
    [SerializeField] float durabilityRegenRate = 0f;  // 초당 재생량 (기본값 0 = 재생 없음)
    [SerializeField] float durabilityRegenDelay = 5f;  // 피해 후 재생 시작까지 지연

    [Header("Shield")]
    [SerializeField, ReadOnly] float maxShield = 100;
    [SerializeField, ReadOnly] float currShield;
    [SerializeField] float shieldRegenRate = 20f;  // 초당 재생량
    [SerializeField] float shieldRegenDelay = 2f;  // 피해 후 재생 시작까지 지연

    [Header("Sounds")]
    public AudioClip deathSound;
    public AudioClip hitSound;
    public AudioClip shieldHitSound;

    public float MaxDurability => usePlayerStats ? PlayerStats.Instance.maxDurability : maxDurability;
    public float CurrDurability => currDurability;
    float DurabilityRegenRate => usePlayerStats ? PlayerStats.Instance.durabilityRegenRate : durabilityRegenRate;
    float DurabilityRegenDelay => usePlayerStats ? PlayerStats.Instance.durabilityRegenDelay : durabilityRegenDelay;

    public float MaxShield => usePlayerStats ? PlayerStats.Instance.maxShield : maxShield;
    public float CurrShield => currShield;
    float ShieldRegenRate => usePlayerStats ? PlayerStats.Instance.shieldRegenRate : shieldRegenRate;
    float ShieldRegenDelay => usePlayerStats ? PlayerStats.Instance.shieldRegenDelay : shieldRegenDelay;

    bool isDead;
    public bool IsDead => isDead;

    float lastDamageTime;


    protected void Start()
    {
        // 사망 시 이벤트 체인 등록
        onDead.AddListener(delegate {
            if (diePrefab)
            {
                Instantiate(diePrefab, transform.position, diePrefab.transform.rotation);
            }
            Destroy(gameObject);
        });
    }

    void Update()
    {
        if (isDead) return;

        // 내구도 재생
        if (currDurability < MaxDurability && DurabilityRegenRate > 0)
        {
            if (Time.time - lastDamageTime >= DurabilityRegenDelay)
            {
                float regenAmount = DurabilityRegenRate * Time.deltaTime;
                ModifyDurability(regenAmount);
            }
        }

        // 실드 재생
        if (currShield < MaxShield && MaxShield > 0)
        {
            if (Time.time - lastDamageTime >= ShieldRegenDelay)
            {
                float regenAmount = ShieldRegenRate * Time.deltaTime;
                ModifyShield(regenAmount);
            }
        }
    }

    // 실드 증감 (양수: 증가, 음수: 감소)
    public void ModifyShield(float amount)
    {
        if (isDead) return;

        currShield += amount;
        currShield = Mathf.Clamp(currShield, 0, MaxShield);

        // 실드 소진 이벤트
        if (currShield == 0)
        {
            onShieldDepleted.Invoke();
        }

        // 피해를 받았을 때만 onShieldDamaged 발생
        if (amount < 0)
        {
            onShieldDamaged.Invoke();
        }

        // 실드 값 변경 시 항상 발생 (UI 업데이트용)
        onShieldChanged.Invoke();
    }

    // 내구도 증감 (양수: 증가, 음수: 감소)
    public void ModifyDurability(float amount)
    {
        if (isDead) return;

        currDurability += amount;
        currDurability = Mathf.Clamp(currDurability, 0, MaxDurability);

        // 피해를 받았을 때만 onDamaged 발생
        if (amount < 0)
        {
            onDamaged.Invoke();
        }

        // 내구도 값 변경 시 항상 발생 (UI 업데이트용)
        onDurabilityChanged.Invoke();

        // 사망 체크
        if (currDurability == 0)
        {
            isDead = true;
            onDead.Invoke();
        }
    }

    virtual public void GetDamaged(float damage, GameObject attacker = null)
    {
        if (isDead) return;

        float remainingDamage = damage;

        // 실드가 먼저 피해를 받음
        if (currShield > 0)
        {
            float shieldDamage = Mathf.Min(currShield, remainingDamage);
            ModifyShield(-shieldDamage);

            remainingDamage -= shieldDamage;
            lastDamageTime = Time.time;

            SoundManager.Instance.PlaySound(shieldHitSound ? shieldHitSound : hitSound);

            // 실드만 피해받고 내구도는 안전하면 return
            if (remainingDamage <= 0) return;
        }

        // 남은 피해를 내구도에 적용
        ModifyDurability(-remainingDamage);
        lastDamageTime = Time.time;

        // 사망 시 사망 사운드, 아니면 피격 사운드
        if (isDead)
        {
            SoundManager.Instance.PlaySound(deathSound);
        }
        else
        {
            SoundManager.Instance.PlaySound(hitSound);
        }
    }

    public void SetMaxDurability(float amount, bool adjustCurrDurability = false)
    {
        float adjust = amount - maxDurability;

        maxDurability += adjust;
        if (adjustCurrDurability) currDurability += adjust;
    }

    public void SetDurabilityRegenRate(float rate)
    {
        durabilityRegenRate = rate;
    }

    public void SetDurabilityRegenDelay(float delay)
    {
        durabilityRegenDelay = delay;
    }

    public void SetMaxShield(float amount, bool adjustCurrShield = false)
    {
        float adjust = amount - maxShield;

        maxShield += adjust;
        if (adjustCurrShield) currShield += adjust;
    }

    public void SetShieldRegenRate(float rate)
    {
        shieldRegenRate = rate;
    }

    public void SetShieldRegenDelay(float delay)
    {
        shieldRegenDelay = delay;
    }

    public void InitDurability()
    {
        currDurability = MaxDurability;
        currShield = MaxShield;
    }

    // 초기화 (usePlayerStats 설정 + 값 초기화)
    public void Initialize(bool useStats)
    {
        usePlayerStats = useStats;
        currDurability = MaxDurability;
        currShield = MaxShield;
    }
}
