using UnityEngine;

/// <summary>
/// 입력에 따라 오브젝트를 회전시키는 컴포넌트
/// InputManager의 RotateInput을 사용하여 좌/우 회전 처리
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RotateByInput : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] float rotationSpeed = 180f; // degrees per second
    [SerializeField] bool usePhysics = false; // true: torque 사용, false: 직접 회전

    Rigidbody2D rbody;

    void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
    }

    // PlayerShip에서 회전 속도를 설정할 수 있도록 public setter 제공
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    void FixedUpdate()
    {
        float rotateInput = InputManager.Instance.RotateInput;

        if (Mathf.Abs(rotateInput) < 0.01f)
            return;

        if (usePhysics)
        {
            // 물리 기반 회전 (토크 적용)
            float torque = -rotateInput * rotationSpeed * rbody.mass;
            rbody.AddTorque(torque);
        }
        else
        {
            // 직접 회전 (각도 변경)
            float rotation = -rotateInput * rotationSpeed * Time.fixedDeltaTime;
            rbody.MoveRotation(rbody.rotation + rotation);
        }
    }
}
