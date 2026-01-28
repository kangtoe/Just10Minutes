using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 탄환이 사라지는 조건 3가지
// 1. 생성 후 일정 시간이 경과.
// 2. 화면 밖으로 나감
// 3. 물체 충돌
public class BulletBase : MonoBehaviour
{
    // 기술적 상수
    private const float LIVE_TIME = 10f;  // 최대 생존 시간 (안전장치)

    [Header("Sounds")]
    public AudioClip onHitSound;

    [Space]
    [SerializeField] bool destoryOnHit = true;
    [SerializeField] bool removeOtherBullet = false;
    int ownerLayer;

    [Space]
    public GameObject hitEffect;
    [SerializeField] float hitEffectRadius = 0f;  // 히트 이펙트 범위 데미지 반경 (0 = 범위 데미지 없음)
    public LayerMask targetLayer; // 해당 오브젝트와 충돌을 검사할 레이어
    public int damage;
    public int impact;
    public float movePower;

    float spwanedTime = 0;

    [Header("Transformation System (Decay/Expansion)")]
    [SerializeField] float transformStartTime = 0f;           // 변형 시작 시간 (0 = 즉시, -1 = 비활성화)
    [SerializeField] float transformDuration = -1f;           // 변형 진행 시간 (-1 = 비활성화)

    [Space]
    [SerializeField] float scaleMultiplierStart = 1f;         // 시작 크기 배율 (Pulse: 0, Decay: 1)
    [SerializeField] float scaleMultiplierEnd = 1f;           // 끝 크기 배율 (Pulse: 8, Decay: 0.5, 변화없음: 1)

    [Space]
    [SerializeField] float alphaStart = 1f;                   // 시작 알파값
    [SerializeField] float alphaEnd = 1f;                     // 끝 알파값 (같으면 변화 없음)
    [SerializeField] float alphaFadeStartRatio = 0f;          // 페이드 시작 비율 (0~1, 0=처음부터, 0.5=50%부터)

    [Space]
    [SerializeField] float damageMultiplierEnd = 1f;          // 끝 데미지 배율 (1.0=변화없음, 0.5=50%로 감소)

    [Space]
    [SerializeField] float colliderDisableThreshold = 1f;     // 충돌 판정 비활성화 임계값 (1.0=비활성화 안 함, 0.8=80%에서 비활성화)

    [Space]
    [SerializeField] bool destroyOnComplete = true;           // 완료 시 파괴 여부

    int initialDamage;      // 초기 데미지 (변형 계산용)
    Vector3 initialScale;   // 초기 스케일 (변형 계산용)
    Color initialColor;     // 초기 색상 (투명도 변형 계산용)

    Rigidbody2D rBody;
    protected Rigidbody2D RBody
    {
        get {
            rBody = GetComponent<Rigidbody2D>();
            return rBody;
        }
    }

    SpriteRenderer sprite;
    protected SpriteRenderer Sprite {
        get {
            sprite = GetComponent<SpriteRenderer>();
            return sprite;
        }
    }

    Collider2D coll;
    protected Collider2D Coll
    {
        get
        {
            coll = GetComponent<Collider2D>();
            return coll;
        }
    }

    TrailRenderer trail;

    private void Update()
    {
        spwanedTime += Time.deltaTime;

        // 변형 시스템 처리 (transformDuration이 0 이상일 때만)
        if (transformDuration >= 0f)
        {
            UpdateTransformation();
        }

        if (LIVE_TIME < spwanedTime)
        {
            OnHitDestory(null, playSound: false, playEffect: false);
        }
    }

    void UpdateTransformation()
    {
        // 변형 시작 시간 이전이면 무시
        if (spwanedTime < transformStartTime)
            return;

        // 변형 진행도 계산 (0 ~ 1)
        float elapsed = spwanedTime - transformStartTime;
        float progress = Mathf.Clamp01(elapsed / transformDuration);

        // 크기 변화 (scaleMultiplierStart → scaleMultiplierEnd)
        float currentScaleMultiplier = Mathf.Lerp(scaleMultiplierStart, scaleMultiplierEnd, progress);
        transform.localScale = initialScale * currentScaleMultiplier;

        // 데미지 변화 (1.0 → damageMultiplierEnd)
        if (damageMultiplierEnd != 1f)
        {
            float currentDamageMultiplier = Mathf.Lerp(1f, damageMultiplierEnd, progress);
            damage = Mathf.RoundToInt(initialDamage * currentDamageMultiplier);
        }

        // 투명도 변화 (alphaStart → alphaEnd)
        if (alphaStart != alphaEnd && Sprite)
        {
            Color color = initialColor;
            float alphaProgress;

            if (progress < alphaFadeStartRatio)
            {
                // 페이드 시작 전: 시작 알파값 유지
                alphaProgress = 0f;
            }
            else
            {
                // 페이드 시작 후: 남은 구간에서 0~1로 매핑
                alphaProgress = (progress - alphaFadeStartRatio) / (1f - alphaFadeStartRatio);
            }

            color.a = Mathf.Lerp(alphaStart, alphaEnd, alphaProgress);
            Sprite.color = color;
        }

        // 진행도에 따라 충돌 판정 비활성화
        if (colliderDisableThreshold < 1f && progress >= colliderDisableThreshold && Coll)
        {
            Coll.enabled = false;
        }

        // 완전히 변형 완료 시 파괴
        if (progress >= 1f && destroyOnComplete)
        {
            OnHitDestory(null, playSound: false, playEffect: false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(removeOtherBullet) RemoveOtherBullet(other);

        // targetLayer 검사
        if (1 << other.gameObject.layer == targetLayer.value)
        {
            OnHitDestory(other);
        }
    }

    void RemoveOtherBullet(Collider2D other)
    {
        BulletBase bullet = other.GetComponent<BulletBase>();

        if (!bullet) return;
        if (bullet.ownerLayer == ownerLayer) return;

        Destroy(bullet.gameObject);
    }

    void ApplyAreaDamage(Vector2 center, float radius, int damage, int impact)
    {
        // 범위 내 모든 Collider 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, targetLayer);

        foreach (Collider2D hit in hits)
        {
            // 데미지 적용
            Damageable damageable = hit.GetComponent<Damageable>();
            if (!damageable && hit.attachedRigidbody)
            {
                damageable = hit.attachedRigidbody.GetComponent<Damageable>();
            }
            if (damageable)
            {
                damageable.GetDamaged(damage, gameObject);
            }

            // 넉백 적용
            Rigidbody2D rbody = hit.attachedRigidbody;
            if (rbody)
            {
                Vector2 dir = (hit.transform.position - (Vector3)center).normalized;
                rbody.AddForce(dir * impact, ForceMode2D.Impulse);
            }
        }
    }

    protected void OnHitDestory(Collider2D hitColl = null, bool playSound = true, bool playEffect = true)
    {
        if (destoryOnHit) Destroy(gameObject);

        if(playSound) SoundManager.Instance.PlaySound(onHitSound);

        if (trail)
        {
            //Debug.Log("trail disttach");
            trail.transform.parent = null;
            trail.autodestruct = true;
        }

        if (playEffect && hitEffect)
        {
            //Debug.Log("Instantiate hitEffect");
            Vector2 point = hitColl ? hitColl.ClosestPoint(transform.position) : transform.position;

            // 히트 이펙트 범위 데미지 처리
            if (hitEffectRadius > 0f)
            {
                ApplyAreaDamage(point, hitEffectRadius, damage, impact);
                Instantiate(hitEffect, point, transform.rotation);  // 시각 효과만 생성
                return;  // 범위 데미지를 입혔으므로 개별 데미지는 생략
            }

            Instantiate(hitEffect, point, transform.rotation);
        }

        if (hitColl)
        {
            // 피해주기
            Damageable damageable = hitColl.GetComponent<Damageable>();
            if (!damageable) damageable = hitColl.attachedRigidbody.GetComponent<Damageable>();
            if (damageable)
            {
                damageable.GetDamaged(damage, gameObject);
            }

            // 힘 가하기       
            Rigidbody2D rbody = hitColl.attachedRigidbody;
            if (rbody)
            {
                Vector2 dir = (hitColl.transform.position - transform.position).normalized;
                //Vector2 dir = transform.up;            
                rbody.AddForce(dir * impact, ForceMode2D.Impulse);
                //Debug.Log(name + " to " + rbody.name + " || " + dir * impact);


            }
        }        
    }

    // shooter에서 생성 시 호출
    virtual public void Init(int ownerLayer, int targetLayer, int damage, int impact, float movePower, AudioClip onHitSound = null)
    {
        //Debug.Log("init");
        this.ownerLayer = ownerLayer;
        this.targetLayer = targetLayer;
        this.damage = damage;
        this.impact = impact;
        this.movePower = movePower;
        this.onHitSound = onHitSound;

        // 변형 시스템용 초기값 저장
        initialDamage = damage;
        initialScale = transform.localScale;
        if (Sprite) initialColor = Sprite.color;

        // 변형 시스템이 활성화된 경우 즉시 시작 스케일 적용 (첫 프레임 깜빡임 방지)
        if (transformDuration >= 0f)
        {
            transform.localScale = initialScale * scaleMultiplierStart;
        }

        if(RBody) RBody.linearVelocity = transform.up * movePower;
        //Debug.Log("velocity : " + rbody.velocity);

        //ColorCtrl colorCtrl = GetComponent<ColorCtrl>();
        //Color color = new Color(colorR, colorG, colorB);
        //colorCtrl.SetColor(color);
    }
}
