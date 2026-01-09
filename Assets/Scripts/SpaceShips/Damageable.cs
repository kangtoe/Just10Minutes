using System.Collections;
using System.Collections.Generic;
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
    public UnityEvent onShieldDamaged = new UnityEvent();

    [HideInInspector]
    public UnityEvent onShieldDepleted = new UnityEvent();

    [Header("Durability")]
    [SerializeField] float maxDurability = 100;
    [SerializeField] float? currDurability = null;

    [Header("Shield")]
    [SerializeField] float maxShield = 100;
    [SerializeField] float? currShield = null;
    [SerializeField] float shieldRegenRate = 20f;  // 초당 재생량
    [SerializeField] float shieldRegenDelay = 2f;  // 피해 후 재생 시작까지 지연

    [Header("Sounds")]
    public AudioClip deathSound;
    public AudioClip hitSound;
    public AudioClip shieldHitSound;

    public float MaxDurability => maxDurability;
    public float CurrDurability
    {
        get{
            if (currDurability == null) currDurability = maxDurability;
            return currDurability.Value;
        }
    }

    public float MaxShield => maxShield;
    public float CurrShield
    {
        get{
            if (currShield == null) currShield = maxShield;
            return currShield.Value;
        }
    }

    bool isDead;
    public bool IsDead => isDead;

    float lastDamageTime;
    Coroutine shieldRegenCoroutine;


    // Start is called before the first frame update
    protected void Start()
    {
        // 사망 시 이벤트 체인 등록
        // TODO :
        // 플레이어 기체 파괴시 게임 오버 함수 추가 등록
        {
            onDead.AddListener(delegate {
                //Debug.Log("onDeadLocal");


                if (diePrefab)
                {
                    Instantiate(diePrefab, transform.position, diePrefab.transform.rotation);
                }

                Destroy(gameObject);
            });
        }

        // 실드 재생 코루틴 시작
        if (maxShield > 0)
        {
            shieldRegenCoroutine = StartCoroutine(ShieldRegenCoroutine());
        }
    }

    virtual public void GetDamaged(float damage, GameObject attacker = null)
    {
        if (isDead) return;

        float remainingDamage = damage;

        // 실드가 먼저 피해를 받음
        if (currShield > 0)
        {
            float shieldDamage = Mathf.Min(CurrShield, remainingDamage);
            currShield = CurrShield - shieldDamage;
            if (currShield < 0) currShield = 0;

            remainingDamage -= shieldDamage;
            lastDamageTime = Time.time;

            onShieldDamaged.Invoke();
            SoundManager.Instance.PlaySound(shieldHitSound ? shieldHitSound : hitSound);

            // 실드 소진 이벤트
            if (currShield == 0)
            {
                onShieldDepleted.Invoke();
            }

            // 실드만 피해받고 내구도는 안전하면 return
            if (remainingDamage <= 0) return;
        }

        // 남은 피해를 내구도에 적용
        currDurability = CurrDurability - remainingDamage;
        if (currDurability < 0) currDurability = 0;
        onDamaged.Invoke();

        // 사망 체크
        if (currDurability == 0)
        {
            SoundManager.Instance.PlaySound(deathSound);
            isDead = true;
            onDead.Invoke();
        }
        else
        {
            SoundManager.Instance.PlaySound(hitSound);
        }
    }

    IEnumerator ShieldRegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            // 사망했으면 중단
            if (isDead) yield break;

            // 실드가 최대치면 재생 불필요
            if (currShield >= maxShield) continue;

            // 재생 지연 시간이 지나지 않았으면 대기
            if (Time.time - lastDamageTime < shieldRegenDelay) continue;

            // 실드 재생
            float regenAmount = shieldRegenRate * 0.1f; // 0.1초당
            currShield = Mathf.Min(CurrShield + regenAmount, maxShield);
        }
    }

    public void SetMaxDurability(float amount, bool adjustCurrDurability = false)
    {
        float adjust = amount - maxDurability;

        maxDurability += adjust;
        if (adjustCurrDurability) currDurability += adjust;
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
        currDurability = maxDurability;
        currShield = maxShield;
    }
}
