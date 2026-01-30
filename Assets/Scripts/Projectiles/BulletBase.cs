using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

// 탄환이 사라지는 조건 3가지
// 1. 생성 후 일정 시간이 경과.
// 2. 화면 밖으로 나감
// 3. 물체 충돌
public class BulletBase : MonoBehaviour
{
    // 기술적 상수
    private const float LIVE_TIME = 10f;  // 최대 생존 시간 (안전장치)

    [Header("Bullet Configuration")]
    [SerializeField] BulletData bulletData;

    // 런타임 설정 (Shooter에서 전달)
    int ownerLayer;
    [SerializeField,ReadOnly] LayerMask targetLayer; // 해당 오브젝트와 충돌을 검사할 레이어

    float spwanedTime = 0;

    // 변형 시스템용 초기값
    int initialDamage;
    Vector3 initialScale;
    Color initialColor;

    // 추적 시스템용 런타임 데이터
    FindTarget findTarget;
    float homingElapsedTime = 0f;

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
        if (bulletData.transformDuration >= 0f)
        {
            UpdateTransformation();
        }

        if (LIVE_TIME < spwanedTime)
        {
            OnHitDestory(null, playSound: false, playEffect: false);
        }
    }

    private void FixedUpdate()
    {
        // 추적 시스템 처리 (homingTurnSpeed > 0일 때만)
        if (bulletData.homingTurnSpeed > 0f && RBody)
        {
            UpdateHoming();
        }
    }

    void UpdateHoming()
    {
        // 추적 시작 시간 이전이면 무시
        if (spwanedTime < bulletData.homingStartTime)
            return;

        // 추적 경과 시간 업데이트
        homingElapsedTime += Time.fixedDeltaTime;

        // 최대 추적 시간 초과 시 직진
        if (bulletData.homingMaxDuration > 0f && homingElapsedTime > bulletData.homingMaxDuration)
        {
            RBody.linearVelocity = transform.up * bulletData.speed;
            return;
        }

        // FindTarget 컴포넌트 가져오기 (lazy initialization)
        if (!findTarget)
        {
            findTarget = GetComponent<FindTarget>();
            if (findTarget)
            {
                findTarget.targetLayer = targetLayer;
            }
        }

        // 타겟이 없으면 직진
        Transform target = findTarget ? findTarget.Target : null;
        if (!target)
        {
            RBody.linearVelocity = transform.up * bulletData.speed;
            return;
        }

        // 타겟 방향 계산 (position을 한 번만 접근)
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        Vector2 targetDirection = toTarget.normalized;

        // 현재 속도 방향 (이미 정규화된 방향 사용)
        Vector2 currentVelocity = RBody.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;
        Vector2 currentDirection = currentSpeed > 0.01f ? currentVelocity / currentSpeed : transform.up;

        // 부드러운 방향 전환 (Lerp)
        Vector2 newDirection = Vector2.Lerp(currentDirection, targetDirection, bulletData.homingTurnSpeed * Time.fixedDeltaTime);
        RBody.linearVelocity = newDirection * bulletData.speed;

        // 스프라이트 회전 (속도 방향으로)
        float angle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
    }

    void UpdateTransformation()
    {
        // 변형 시작 시간 이전이면 무시
        if (spwanedTime < bulletData.transformStartTime)
            return;

        // 변형 진행도 계산 (0 ~ 1)
        float elapsed = spwanedTime - bulletData.transformStartTime;
        float progress = Mathf.Clamp01(elapsed / bulletData.transformDuration);

        // 크기 변화 (scaleMultiplierStart → scaleMultiplierEnd)
        float currentScaleMultiplier = Mathf.Lerp(bulletData.scaleMultiplierStart, bulletData.scaleMultiplierEnd, progress);
        transform.localScale = initialScale * currentScaleMultiplier;

        // 데미지 변화 (1.0 → damageMultiplierEnd)
        if (bulletData.damageMultiplierEnd != 1f)
        {
            float currentDamageMultiplier = Mathf.Lerp(1f, bulletData.damageMultiplierEnd, progress);
            bulletData.damage = Mathf.RoundToInt(initialDamage * currentDamageMultiplier);
        }

        // 속도 변화 (speedMultiplierStart → speedMultiplierEnd)
        if (bulletData.speedMultiplierEnd != bulletData.speedMultiplierStart && RBody)
        {
            float currentSpeedMultiplier = Mathf.Lerp(bulletData.speedMultiplierStart, bulletData.speedMultiplierEnd, progress);
            Vector2 currentDirection = RBody.linearVelocity.normalized;
            RBody.linearVelocity = currentDirection * (bulletData.speed * currentSpeedMultiplier);
        }

        // 회전 (rotationSpeedStart → rotationSpeedEnd)
        if (bulletData.rotationSpeedEnd != bulletData.rotationSpeedStart)
        {
            float currentRotationSpeed = Mathf.Lerp(bulletData.rotationSpeedStart, bulletData.rotationSpeedEnd, progress);
            transform.Rotate(0, 0, currentRotationSpeed * Time.deltaTime);
        }

        // 투명도 변화 (alphaStart → alphaEnd)
        if (bulletData.alphaStart != bulletData.alphaEnd && Sprite)
        {
            Color color = initialColor;
            float alphaProgress;

            if (progress < bulletData.alphaFadeStartRatio)
            {
                // 페이드 시작 전: 시작 알파값 유지
                alphaProgress = 0f;
            }
            else
            {
                // 페이드 시작 후: 남은 구간에서 0~1로 매핑
                alphaProgress = (progress - bulletData.alphaFadeStartRatio) / (1f - bulletData.alphaFadeStartRatio);
            }

            color.a = Mathf.Lerp(bulletData.alphaStart, bulletData.alphaEnd, alphaProgress);
            Sprite.color = color;
        }

        // 진행도에 따라 충돌 판정 비활성화
        if (bulletData.colliderDisableThreshold < 1f && progress >= bulletData.colliderDisableThreshold && Coll)
        {
            Coll.enabled = false;
        }

        // 완전히 변형 완료 시 파괴
        if (progress >= 1f && bulletData.destroyOnComplete)
        {
            OnHitDestory(null, playSound: false, playEffect: false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(bulletData.removeOtherBullet) RemoveOtherBullet(other);

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
        if (bulletData.destroyOnHit) Destroy(gameObject);

        if(playSound) SoundManager.Instance.PlaySound(bulletData.onHitSound);

        if (trail)
        {
            //Debug.Log("trail disttach");
            trail.transform.parent = null;
            trail.autodestruct = true;
        }

        if (playEffect && bulletData.hitEffect)
        {
            //Debug.Log("Instantiate hitEffect");
            Vector2 point = hitColl ? hitColl.ClosestPoint(transform.position) : transform.position;

            // 히트 이펙트 범위 데미지 처리
            if (bulletData.hitEffectRadius > 0f)
            {
                ApplyAreaDamage(point, bulletData.hitEffectRadius, bulletData.damage, bulletData.impact);
                Instantiate(bulletData.hitEffect, point, transform.rotation);  // 시각 효과만 생성
                return;  // 범위 데미지를 입혔으므로 개별 데미지는 생략
            }

            // 폭발 피해 처리 (explosionDamageRatio > 0)
            if (bulletData.explosionDamageRatio > 0f)
            {
                float explosionRadius = 2f;  // 기본 폭발 반경
                int explosionDamage = Mathf.RoundToInt(bulletData.damage * bulletData.explosionDamageRatio);
                ApplyAreaDamage(point, explosionRadius, explosionDamage, bulletData.impact);
                Instantiate(bulletData.hitEffect, point, transform.rotation);
                // 폭발 피해 적용 후에도 직접 충돌 피해는 아래에서 처리됨
            }
            else
            {
                Instantiate(bulletData.hitEffect, point, transform.rotation);
            }
        }

        if (hitColl)
        {
            // 피해주기
            Damageable damageable = hitColl.GetComponent<Damageable>();
            if (!damageable) damageable = hitColl.attachedRigidbody.GetComponent<Damageable>();
            if (damageable)
            {
                damageable.GetDamaged(bulletData.damage, gameObject);
            }

            // 힘 가하기
            Rigidbody2D rbody = hitColl.attachedRigidbody;
            if (rbody)
            {
                Vector2 dir = (hitColl.transform.position - transform.position).normalized;
                //Vector2 dir = transform.up;
                rbody.AddForce(dir * bulletData.impact, ForceMode2D.Impulse);
                //Debug.Log(name + " to " + rbody.name + " || " + dir * impact);


            }
        }
    }

    // shooter에서 생성 시 호출
    virtual public void Init(int ownerLayer, LayerMask targetLayer, BulletData data)
    {
        //Debug.Log("init");
        this.ownerLayer = ownerLayer;
        this.targetLayer = targetLayer;

        // 기존 참조 타입 백업 (프리팹에 설정된 값)
        GameObject existingHitEffect = this.bulletData.hitEffect;
        AudioClip existingOnHitSound = this.bulletData.onHitSound;

        // 전체 데이터 할당
        this.bulletData = data;

        // null이면 프리팹의 기존 값 복원
        if (data.hitEffect == null) this.bulletData.hitEffect = existingHitEffect;
        if (data.onHitSound == null) this.bulletData.onHitSound = existingOnHitSound;

        // 변형 시스템용 초기값 저장
        initialDamage = bulletData.damage;
        initialScale = transform.localScale;
        if (Sprite) initialColor = Sprite.color;

        // 변형 시스템이 활성화된 경우 즉시 시작 스케일 적용 (첫 프레임 깜빡임 방지)
        if (bulletData.transformDuration >= 0f)
        {
            transform.localScale = initialScale * bulletData.scaleMultiplierStart;
        }

        if(RBody) RBody.linearVelocity = transform.up * bulletData.speed;
        //Debug.Log("velocity : " + rbody.velocity);

        //ColorCtrl colorCtrl = GetComponent<ColorCtrl>();
        //Color color = new Color(colorR, colorG, colorB);
        //colorCtrl.SetColor(color);
    }
}
