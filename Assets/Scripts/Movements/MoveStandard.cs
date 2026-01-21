using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveStandard : MonoBehaviour
{
    [SerializeField] bool moveManually;
    [SerializeField] bool limitMaxSpeed;
    [SerializeField] protected float movePower = 10f;

    protected Rigidbody2D rbody;
    bool isPlayerShip;

    //TrailEffect trailEffect;
    //FlameEffect flameEffect;

    // Start is called before the first frame update
    protected void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        isPlayerShip = GetComponent<PlayerShip>() != null;

        //trailEffect = GetComponentInChildren<TrailEffect>();
        //flameEffect = GetComponentInChildren<FlameEffect>();
    }

    // 현재 사용할 질량 값 반환 (PlayerShip이면 PlayerStats 참조, 아니면 Rigidbody2D 사용)
    float GetMass()
    {
        return isPlayerShip ? PlayerStats.Instance.mass : rbody.mass;
    }

    // PlayerShip에서 이동 속도를 설정할 수 있도록 public setter 제공
    public void SetMovePower(float power)
    {
        movePower = power;
    }
    
    protected void FixedUpdate()
    {
        if (!moveManually)
        {
            Move();
        }

        //print(rbody.velocity.magnitude);

        float mass = GetMass();
        float limit = movePower * mass / rbody.linearDamping;
        if (limitMaxSpeed && rbody.linearVelocity.magnitude > limit)
        {
            rbody.linearVelocity = Vector2.ClampMagnitude(rbody.linearVelocity, limit * 1f);
        }

        //float TrailVelocity = 0.5f;
        //if (rbody.velocity.magnitude <= TrailVelocity) trailEffect.TrailDistach();
        //else trailEffect.TrailAttach();
    }


    public void Move()
    {
        float mass = GetMass();
        rbody.AddForce(transform.up * movePower * mass);
    }

    public void Move(Vector2 vec)
    {
        float mass = GetMass();
        rbody.AddForce(vec.normalized * movePower * mass);
    }
}
