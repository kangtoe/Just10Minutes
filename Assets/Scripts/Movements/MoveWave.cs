using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWave : MonoBehaviour
{
    public float movePower = 1f;
    Rigidbody2D rbody;

    [HideInInspector] public bool waveInverse;
    public float amplitude = 1f;
    public float frequency = 1f;

    Vector3 startPosition;
    Quaternion rotation;

    float ellapsedTime = 0;

    BoundaryJump boundaryJump;
    BoundaryJump BoundaryJump
    {
        get
        {
            if (!boundaryJump) boundaryJump = GetComponent<BoundaryJump>();
            return boundaryJump;
        }
    }

    private void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
        rbody.linearVelocity = Vector2.zero;

        startPosition = transform.position;
        rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 90);

        if (BoundaryJump)
        {
            BoundaryJump.onJump.AddListener(delegate
            {
                ellapsedTime = 0;
                startPosition = transform.position;
                rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 90);
            });
        }
    }

    void FixedUpdate()
    {
        ellapsedTime += Time.fixedDeltaTime * movePower;
        Move();
    }

    void Move()
    {
        float x = ellapsedTime;
        float y = Mathf.Sin(ellapsedTime * frequency) * amplitude;
        if (waveInverse) y *= -1;

        Vector3 rotatedVector = rotation * new Vector3(x, y, 0);        

        //transform.position = startPosition + rotatedVector;
        rbody.MovePosition(startPosition + rotatedVector);
    }
}
