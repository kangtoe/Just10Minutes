using UnityEngine;

// 플레이어가 화면 밖으로 벗어났을 때 즉시 사망 처리
[RequireComponent(typeof(Damageable))]
public class BoundaryDeath : BoundaryBase
{
    Damageable damageable;

    protected override void Start()
    {
        base.Start();

        damageable = GetComponent<Damageable>();
    }

    protected override void OnEnterBoundaryZone(Vector3 currentPos, float boundaryX, float boundaryY)
    {
        if (damageable.IsDead) return;

        // 화면 밖으로 완전히 벗어나면 즉시 사망
        damageable.GetDamaged(int.MaxValue);
    }
}
