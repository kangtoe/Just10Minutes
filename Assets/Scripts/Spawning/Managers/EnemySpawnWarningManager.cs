using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰 경고 시스템
/// - 적이 스폰되기 전 지정된 위치에 경고 마커를 표시
/// - 오브젝트 풀링을 사용하여 마커 재사용
/// </summary>
public class EnemySpawnWarningManager : MonoSingleton<EnemySpawnWarningManager>
{
    [Header("Warning Settings")]
    [SerializeField] private GameObject warningMarkerPrefab;
    [SerializeField] private float warningDuration = 1f; // 경고 표시 시간
    [SerializeField] private float edgeOffset = 0.5f; // 화면 가장자리에서 안쪽으로 들어올 거리

    [Header("Marker Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;

    private Queue<GameObject> markerPool = new Queue<GameObject>();
    private List<GameObject> activeMarkers = new List<GameObject>();

    private void Start()
    {
        InitializePool();
    }

    /// <summary>
    /// 마커 풀 초기화
    /// </summary>
    private void InitializePool()
    {
        if (warningMarkerPrefab == null)
        {
            Debug.LogError("[EnemySpawnWarning] Warning Marker Prefab is not assigned!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject marker = Instantiate(warningMarkerPrefab, transform);
            marker.SetActive(false);
            markerPool.Enqueue(marker);
        }
    }

    /// <summary>
    /// 경고 마커 표시 (스폰 위치 계산 및 화면 안쪽으로 조정)
    /// </summary>
    /// <param name="enemyPrefab">스폰될 적 프리팹</param>
    /// <param name="spawnEdge">스폰되는 Edge</param>
    /// <param name="lengthRatio">Edge 내 위치 비율 (0~1)</param>
    /// <param name="duration">표시 시간 (null이면 기본값 사용)</param>
    public void ShowWarning(GameObject enemyPrefab, Edge spawnEdge, float lengthRatio, float? duration = null)
    {
        // 스폰 위치 계산
        Vector2 spawnPosition = ObjectSpawner.Instance.CalculateSpawnPosition(
            enemyPrefab,
            spawnEdge,
            lengthRatio
        );

        // Edge별로 화면 안쪽으로 위치 조정
        Vector2 warningPosition = CalculateWarningPosition(spawnPosition, spawnEdge);

        Debug.Log($"[EnemySpawnWarning] Showing warning at {warningPosition} (spawn: {spawnPosition}, edge: {spawnEdge})");

        GameObject marker = GetMarker();
        if (marker == null)
        {
            Debug.LogWarning("[EnemySpawnWarning] No available markers in pool!");
            return;
        }

        marker.transform.position = warningPosition;
        marker.transform.rotation = GetMarkerRotation(spawnEdge);
        marker.SetActive(true);
        activeMarkers.Add(marker);

        float displayDuration = duration ?? warningDuration;
        StartCoroutine(HideMarkerAfterDelay(marker, displayDuration));
    }

    /// <summary>
    /// Edge별로 경고 마커 위치를 화면 안쪽으로 계산
    /// </summary>
    private Vector2 CalculateWarningPosition(Vector2 spawnPosition, Edge spawnEdge)
    {
        // 화면 경계 계산
        Vector2 screenMin = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector2 screenMax = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

        Vector2 warningPos = spawnPosition;

        switch (spawnEdge)
        {
            case Edge.Up:
                // 위쪽 스폰: Y를 아래로
                warningPos.y = screenMax.y - edgeOffset;
                break;

            case Edge.Down:
                // 아래쪽 스폰: Y를 위로
                warningPos.y = screenMin.y + edgeOffset;
                break;

            case Edge.Left:
                // 왼쪽 스폰: X를 오른쪽으로
                warningPos.x = screenMin.x + edgeOffset;
                break;

            case Edge.Right:
                // 오른쪽 스폰: X를 왼쪽으로
                warningPos.x = screenMax.x - edgeOffset;
                break;
        }

        return warningPos;
    }

    /// <summary>
    /// Edge별로 경고 마커 회전 계산 (화살표가 스폰 방향을 가리키도록)
    /// 기본 화살표가 아래를 가리킨다고 가정
    /// </summary>
    private Quaternion GetMarkerRotation(Edge spawnEdge)
    {
        float angle = 0f;

        switch (spawnEdge)
        {
            case Edge.Up:
                // 위에서 스폰: 아래로 가리킴 (적이 아래로 들어옴)
                angle = 0f;
                break;

            case Edge.Down:
                // 아래에서 스폰: 위로 가리킴 (적이 위로 들어옴)
                angle = 180f;
                break;

            case Edge.Left:
                // 왼쪽에서 스폰: 왼쪽을 가리킴
                angle = 90f;
                break;

            case Edge.Right:
                // 오른쪽에서 스폰: 오른쪽을 가리킴
                angle = -90f;
                break;
        }

        return Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 풀에서 마커 가져오기
    /// </summary>
    private GameObject GetMarker()
    {
        if (markerPool.Count > 0)
        {
            return markerPool.Dequeue();
        }

        // 풀이 부족하면 새로 생성
        GameObject newMarker = Instantiate(warningMarkerPrefab, transform);
        return newMarker;
    }

    /// <summary>
    /// 일정 시간 후 마커 숨기기
    /// </summary>
    private IEnumerator HideMarkerAfterDelay(GameObject marker, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (marker != null)
        {
            marker.SetActive(false);
            activeMarkers.Remove(marker);
            markerPool.Enqueue(marker);
        }
    }

    /// <summary>
    /// 모든 활성 마커 즉시 숨기기
    /// </summary>
    public void HideAllWarnings()
    {
        foreach (GameObject marker in activeMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(false);
                markerPool.Enqueue(marker);
            }
        }
        activeMarkers.Clear();
    }
}

/// <summary>
/// 경고 마커 애니메이션 (깜빡임 효과)
/// </summary>
public class WarningMarkerAnimation : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;

    private SpriteRenderer spriteRenderer;
    private float time;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        time = 0f;
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        time += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1f) / 2f);

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}
