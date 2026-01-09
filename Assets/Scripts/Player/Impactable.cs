using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// collider - collider 충돌 시, 부딪힌 대상에게 피해와 힘을 가한다.
public class Impactable : MonoBehaviour
{
    [SerializeField] GameObject bumpEffect;

    // PlayerStats 싱글톤에서 스탯 참조 여부 (플레이어용: true, 적용: false)
    bool usePlayerStats = false;

    [Header("Amount")]
    public float impactDamageOther = 10;
    public float impactDamageSelf = 10;
    public float impactPowerOther = 10;
    public float impactPowerSelf = 10;

    // 런타임 스탯 프로퍼티 (PlayerStats 또는 로컬 필드에서 가져옴)
    float ImpactDamageOther => usePlayerStats ? PlayerStats.Instance.onImpact : impactDamageOther;

    Damageable m_damageable;
    Rigidbody2D m_rbody;

    private void Start()
    {
        m_damageable = GetComponent<Damageable>();
        m_rbody = GetComponent<Rigidbody2D>();
    }
    void OnCollisionEnter2D(Collision2D coll)
    {
        Impact(coll);
        StuckAdjust(coll);
    }
    void Impact(Collision2D coll)
    {
        // take damage
        if (m_damageable)
        {
            m_damageable.GetDamaged(impactDamageSelf);
        }

        // do damage
        Damageable damageable = coll.transform.GetComponent<Damageable>();
        if (damageable)
        {
            damageable.GetDamaged(ImpactDamageOther);
        }

        Vector2 dir = coll.transform.position - transform.position;

        // add force other
        Rigidbody2D rbody = coll.transform.GetComponent<Rigidbody2D>();
        if (rbody)
        {            
            rbody.AddForce(dir * impactPowerOther, ForceMode2D.Impulse);
        }

        if (m_rbody)
        {
            m_rbody.AddForce(-1 * dir * impactPowerSelf, ForceMode2D.Impulse);
        }


        // effect
        if (bumpEffect)
        {
            Vector2 normal = coll.contacts[0].normal;
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90; 
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            Vector2 impactPoint = coll.contacts[0].point;
            Instantiate(bumpEffect, impactPoint, rotation);
        }
    }

    public void SetDamageAmount(float? DamageSelf = null, float ? DamageOther = null)
    {
        if (DamageSelf != null) impactDamageSelf = DamageSelf.Value;
        if (DamageOther != null) impactDamageOther = DamageOther.Value;
    }

    public void StuckAdjust(Collision2D coll)
    {        
        Collider2D mColl = coll.collider;
        Collider2D otherColl = coll.otherCollider;        
        Bounds mBounds = mColl.bounds;
        Bounds otherBounds = otherColl.bounds;
        Transform mTf = mColl.attachedRigidbody.transform;
        Transform otherTf = otherColl.attachedRigidbody.transform;

        // overlap check
        if (mBounds.Contains(otherBounds.min) && mBounds.Contains(otherBounds.max))
        {
            print("mColl : " + mColl);
            print("otherColl : " + otherColl);

            Vector3 dir = (otherTf.position - mTf.position).normalized;
            Vector3 farPos = otherTf.position + dir * 10;

            otherTf.position = mColl.ClosestPoint(farPos);
        }
    }

    // PlayerStats 참조 사용 여부 설정
    public void TogglePlayerStatsReference(bool useStats)
    {
        usePlayerStats = useStats;
    }
}
