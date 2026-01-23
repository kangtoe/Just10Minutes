using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 오브젝트가 화면 밖으로 벗어났을 때,
// 반대편 (오른쪽 <-> 왼쪽 / 위쪽 <-> 아래쪽) 가장자리 이동
public class BoundaryJump : BoundaryBase
{
    [SerializeField]
    int destoryJumpCount = 5;

    [SerializeField]
    bool addForceOppsiteOnJump = false;

    [SerializeField]
    bool initVelocityOnJump = true;

    Rigidbody2D rbody;
    //TrailEffect effect;

    float jumpableTime = 0;
    float jumpInterval = 0.5f; // 한번 가장자리 이동이 실행되면, 직후 이 기간동안은 다시 가장자리 이동체크를 중단

    [HideInInspector]
    public UnityEvent onJump;

    protected override void Start()
    {
        base.Start();

        rbody = GetComponent<Rigidbody2D>();
        //effect = GetComponentInChildren<TrailEffect>();
    }

    protected override void OnExitScreen(Vector3 currentPos, float boundaryX, float boundaryY)
    {
        if (Time.time < jumpableTime) return;

        Vector3 pos = currentPos;

        // x축 체크
        if (pos.x < -boundaryX) JumpToOppsite(Edge.Right, boundaryX, boundaryY);
        else if (pos.x > boundaryX) JumpToOppsite(Edge.Left, boundaryX, boundaryY);

        // y축 체크
        if (pos.y < -boundaryY) JumpToOppsite(Edge.Up, boundaryX, boundaryY);
        else if (pos.y > boundaryY) JumpToOppsite(Edge.Down, boundaryX, boundaryY);
    }

    void JumpToOppsite(Edge jumpedEdge, float moveX, float moveY)
    {
        //Debug.Log("JumpToOppsite");
        //if (effect) effect.TrailDistachRPC();

        Vector3 pos = transform.position;

        if (jumpedEdge == Edge.Up)      pos = new Vector2(pos.x, moveY);
        if (jumpedEdge == Edge.Down)    pos = new Vector2(pos.x, -moveY);
        if (jumpedEdge == Edge.Right)   pos = new Vector2(moveX, pos.y);
        if (jumpedEdge == Edge.Left)    pos = new Vector2(-moveX, pos.y);
        transform.position = pos;

        AddForceToOppsite(jumpedEdge);
        onJump.Invoke();

        jumpableTime = Time.time + jumpInterval;

        if (destoryJumpCount >= 0)
        {
            if (destoryJumpCount == 0) Destroy(gameObject);
            else destoryJumpCount--;
        }
    }

    // 가장 자리 이동 후 반대편으로 약간 밀어준다
    // -> 점프 직후 속력이 충분하지 못해 다시 점프가 일어나는 현상을 줄여준다
    void AddForceToOppsite(Edge jumpedEdge)
    {
        Vector2 dir = Vector2.zero;

        if (addForceOppsiteOnJump)
        {
            if (jumpedEdge == Edge.Up) dir = Vector2.down;
            if (jumpedEdge == Edge.Down) dir = Vector2.up;
            if (jumpedEdge == Edge.Right) dir = Vector2.left;
            if (jumpedEdge == Edge.Left) dir = Vector2.right;
        }
        else
        {
            dir = transform.up;
        }

        if (initVelocityOnJump) rbody.linearVelocity = Vector2.zero;
        float movePower = 2f;
        rbody.AddForce(rbody.mass * dir * movePower, ForceMode2D.Impulse);
    }
}
