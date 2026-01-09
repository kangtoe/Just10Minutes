using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class ShooterBase : MonoBehaviour
{
    public Transform[] firePoints;
    public LayerMask targetLayer;
    public bool manualFire = false;

    // PlayerStats 싱글톤에서 스탯 참조 여부 (플레이어용: true, 적용: false)
    bool usePlayerStats = false;

    [Header("Fire time")]
    public float firePointDelay; // 탄환 발사 위치 별 간격
    public float fireStartDelay = 0f; // 등장 후 첫 사격까지 대기시간
    public float fireDelay; // 탄환 발사간격
    protected float lastFireTime = 0f; // 마지막 탄환 사격 시점

    [Header("Multy shoot")]
    public int shotCountPerFirepoint = 1; // 한번에 fire point 마다 발사하는 탄환 수
    public float shotAngle = 0;
    public float intervalX = 0.2f, intervalY = 0.2f; // 탄환 간격
    [Range(0, 1)]
    public float outsideSlower = 0f; // 바깥쪽 탄환일수록 속도를 느리게 하는 정도

    [Header("Projectile Info")]
    //public GameObject hitEffect;// 발사체 프리펩에서 지정할것 : 동기화 직렬화 불가
    public GameObject projectilePrefab; // 발사할 탄환
    public int damage = 0;
    public int impactPower = 0;
    public int projectileMovePower = 10;
    public float projectileLiveTime = 3f;
    public bool createBulletAsChild = false;

    [Header("Sounds")]
    public AudioClip shootSound;
    public AudioClip onHitSound;

    // 런타임 스탯 프로퍼티 (PlayerStats 또는 로컬 필드에서 가져옴)
    int ShotCount => usePlayerStats ? PlayerStats.Instance.multiShot : shotCountPerFirepoint;
    int Damage => usePlayerStats ? PlayerStats.Instance.projectileDamage : damage;
    float ProjectileSpeed => usePlayerStats ? PlayerStats.Instance.projectileSpeed : projectileMovePower;

    public bool Available => Time.time >= lastFireTime + fireDelay;

    void Update()
    {
        if (fireStartDelay > 0)
        {
            fireStartDelay -= Time.deltaTime;
            return;
        }
        else fireStartDelay = 0;

        if(!manualFire) TryFire();
    }

    public void SetMultiShot(int amount)
    {
        shotCountPerFirepoint = amount;
    }

    public void SetDamage(int damage)
    {
        this.damage = damage;
    }

    // 사격 시도
    public virtual bool TryFire(bool forceFire = false)
    {
        //Debug.Log("TryFire");        

        // 마지막 발사로부터 충분한 시간 간격이 있었는가
        if (forceFire || Available)
        {
            // 마지막 발사시점 갱신
            lastFireTime = Time.time;

            //Debug.Log("fire!");
            Fire();
            return true;
        }

        return false;
    }

    // 실제 사격 -> shooter의 firePoint 방향대로 projectile을 생성
    protected virtual void Fire()
    {
        IEnumerator Cr()
        {
            foreach (Transform firePoint in firePoints)
            {
                FireMulty(firePoint, ShotCount, intervalX, intervalY);
                yield return new WaitForSeconds(firePointDelay);
            }
        }

        StartCoroutine(Cr());
    }

    // firePoint에서 탄환 생성
    // 인수: 생성할 총알의 개수, 탄환간 간격
    protected void FireMulty(Transform firePoint, int numberOfBullets = 1, float intervalX = 0.2f, float intervalY = 0.2f)
    {                        
        Vector3 pos = firePoint.position;
        Quaternion rot = firePoint.rotation;

        // 총알 생성
        for (int i = 0; i < numberOfBullets; i++)
        {
            float adjustX;
            float adjustY;

            // 탄환 위치 구하기
            if (numberOfBullets == 1)
            {
                adjustX = 0;
            }
            else
            {
                if (numberOfBullets % 2 == 1)
                {
                    // 발사체 생성 개수 홀수
                    int centerIdx = numberOfBullets / 2;
                    adjustX = centerIdx - i;                     
                }
                else
                {
                    // 발사체 생성 개수 짝수
                    int centerIdxUp = numberOfBullets / 2;
                    int centerIdxDown = centerIdxUp - 1;
                    if (i <= centerIdxDown) adjustX = i - centerIdxDown - 0.5f;
                    else adjustX = i - centerIdxUp + 0.5f;

                    //Debug.Log("i : "+ i +" || adjust : " + adjustX);
                }
            }

            // 위치 조정
            adjustY = -Mathf.Abs(adjustX);
            pos = firePoint.position;
            pos += firePoint.rotation * new Vector3(adjustX * intervalX, adjustY * intervalY, 0);

            // 탄환 회전 구하기            
            float f = 0; // 얼마나 중앙에 있는가 나타냄 (-0.5: 왼쪽 끝,  1: 정중앙, 0.5: 오른쪽 끝)
            if (numberOfBullets > 1) f = -1 + 2f * i / (numberOfBullets - 1);

            // 회전 구하기
            float angle = f * shotAngle; // 중앙으로부터 멀수록 회전이 큼
            rot = firePoint.rotation * Quaternion.Euler(0, 0, angle);

            // 발사체 생성
            GameObject go = Instantiate(projectilePrefab, pos, rot);
            if (createBulletAsChild) go.transform.SetParent(transform);

            // 발사체 속도 구하기
            // 중앙으로부터 회전이 클수록 탄속도 느려진다.
            float baseSpeed = ProjectileSpeed;
            float nomalRatio = 1 - outsideSlower;
            float slowRatio = outsideSlower;
            float speed = (nomalRatio * baseSpeed) + (slowRatio * baseSpeed * (1 - Mathf.Abs(f)));

            // 발사체 초기화
            go.GetComponent<BulletBase>().Init(gameObject.layer, targetLayer, Damage, impactPower, speed, projectileLiveTime, onHitSound);
        }

        SoundManager.Instance.PlaySound(shootSound);
    }

    // PlayerStats 참조 사용 여부 설정
    public void TogglePlayerStatsReference(bool useStats)
    {
        usePlayerStats = useStats;
    }
}
