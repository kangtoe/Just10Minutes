using UnityEngine;

// 화면 경계 처리 베이스 클래스
// BoundaryJump, BoundaryDeath 등이 상속
[DisallowMultipleComponent]
public abstract class BoundaryBase : MonoBehaviour
{
    protected Collider2D shipCollider;
    protected Vector2 cameraSize;
    protected Vector2 shipSize;

    float checkInterval = 0.1f;
    bool isInBoundaryZone = false;
    bool hasExitedScreen = false;

    protected virtual void Start()
    {
        shipCollider = GetComponentInChildren<Collider2D>();
        if (shipCollider == null)
        {
            Debug.LogError("BoundaryBase requires Collider2D component!");
            enabled = false;
            return;
        }

        cameraSize = GetCameraSize();
        shipSize = GetShipBoundSize();

        InvokeRepeating(nameof(CheckBoundary), 0, checkInterval);
    }

    Vector2 GetCameraSize()
    {
        float cameraSizeX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x * 2;
        float cameraSizeY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0)).y * 2;
        return new Vector2(cameraSizeX, cameraSizeY);
    }

    Vector2 GetShipBoundSize()
    {
        return shipCollider.bounds.size;
    }

    void CheckBoundary()
    {
        Vector3 pos = transform.position;

        // 화면 가장자리 (카메라 뷰포트 경계)
        float screenEdgeX = cameraSize.x / 2;
        float screenEdgeY = cameraSize.y / 2;

        // 화면 완전 벗어남 (ship이 완전히 보이지 않는 지점)
        float exitX = screenEdgeX + shipSize.x / 2;
        float exitY = screenEdgeY + shipSize.y / 2;

        bool inBoundaryZoneNow = (pos.x < -screenEdgeX || pos.x > screenEdgeX ||
                                   pos.y < -screenEdgeY || pos.y > screenEdgeY);

        bool exitedScreenNow = (pos.x < -exitX || pos.x > exitX ||
                                pos.y < -exitY || pos.y > exitY);

        // 경계 구역 진입 이벤트 (처음 화면 가장자리에 닿았을 때)
        if (inBoundaryZoneNow && !isInBoundaryZone)
        {
            isInBoundaryZone = true;
            OnEnterBoundaryZone(pos, screenEdgeX, screenEdgeY);
        }
        // 경계 구역에서 벗어남 (다시 화면 안으로 돌아옴)
        else if (!inBoundaryZoneNow && isInBoundaryZone)
        {
            isInBoundaryZone = false;
            hasExitedScreen = false;
        }

        // 화면 완전 벗어남 이벤트 (처음 화면을 완전히 벗어났을 때)
        if (exitedScreenNow && !hasExitedScreen)
        {
            hasExitedScreen = true;
            OnExitScreen(pos, exitX, exitY);
        }
    }

    // 화면 가장자리에 닿았을 때의 동작 (필요시 파생 클래스에서 오버라이드)
    protected virtual void OnEnterBoundaryZone(Vector3 currentPos, float boundaryX, float boundaryY)
    {
    }

    // 화면을 완전히 벗어났을 때의 동작 (필요시 파생 클래스에서 오버라이드)
    protected virtual void OnExitScreen(Vector3 currentPos, float boundaryX, float boundaryY)
    {
    }
}
