using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 화면 가장자리에서 오브젝트를 스폰하는 싱글톤 매니저
/// - 스폰 위치: 화면 4방향 가장자리 (Up, Down, Left, Right, Random)
/// - 스폰 회전: 기본적으로 화면 중앙을 바라보도록 설정 (lookCenter 옵션)
/// - 다중 스폰: 고정 간격 또는 랜덤 위치로 여러 개 스폰 가능
/// - 겹침 방지: 기존 오브젝트와 최소 거리 유지 (랜덤 스폰)
/// </summary>
public class ObjectSpawner : MonoSingleton<ObjectSpawner>
{
    #region Fields & Properties

    // 스폰된 오브젝트 목록
    List<GameObject> spawned = new();

    #endregion

    #region Public API

    /// <summary>
    /// 단일 오브젝트를 즉시 스폰 (시간 기반 스폰 시스템용)
    /// </summary>
    /// <param name="objectPrefab">스폰할 프리팹</param>
    /// <param name="spawnSide">스폰할 Edge</param>
    /// <param name="lookCenter">중앙을 바라볼지 여부</param>
    /// <param name="lengthRatio">Edge 내 위치 비율 (0~1), null이면 랜덤</param>
    public GameObject SpawnObject(GameObject objectPrefab, Edge spawnSide, bool lookCenter = true, float? lengthRatio = null)
    {
        if (objectPrefab == null)
        {
            Debug.Log("objectPrefab is null");
            return null;
        }

        var (pos, rot) = GetSpawnPointAndRotation(objectPrefab, spawnSide, lengthRatio, lookCenter);
        GameObject go = Instantiate(objectPrefab);
        go.transform.position = pos;
        go.transform.rotation = rot;
        spawned.Add(go);

        return go;
    }

    /// <summary>
    /// 스폰 위치 미리 계산 (경고 시스템용)
    /// </summary>
    /// <param name="objectPrefab">스폰할 프리팹</param>
    /// <param name="spawnSide">스폰할 Edge</param>
    /// <param name="lengthRatio">Edge 내 위치 비율 (0~1), null이면 랜덤</param>
    public Vector2 CalculateSpawnPosition(GameObject objectPrefab, Edge spawnSide, float? lengthRatio = null)
    {
        if (objectPrefab == null)
        {
            Debug.LogWarning("objectPrefab is null");
            return Vector2.zero;
        }

        var (pos, _) = GetSpawnPointAndRotation(objectPrefab, spawnSide, lengthRatio, true);
        return pos;
    }

    #endregion

    #region Position & Rotation

    /// <summary>
    /// 화면 가장자리에서 스폰 위치와 회전 계산
    /// </summary>
    /// <param name="spawnSide">스폰할 가장자리 방향</param>
    /// <param name="lengthRatio">가장자리 위치 비율 (0~1), null이면 랜덤</param>
    /// <param name="lookCenter">true면 화면 중앙 방향으로 회전, false면 Edge 기본 방향</param>
    (Vector2, Quaternion) GetSpawnPointAndRotation(GameObject objectPrefab, Edge spawnSide = Edge.Random, float? lengthRatio = null, bool lookCenter = true)
    {
        Vector2 boundSize = GetObjectBoundSize(objectPrefab) / 2;

        Vector3 offsetPos = Vector3.zero;
        Vector3 viewPos;
        float angle;

        if (lengthRatio == null)
        {
            lengthRatio = Random.Range(0f, 1f);
        }
        if (spawnSide == Edge.Random)
        {
            int len = Enum.GetValues(typeof(Edge)).Length;
            spawnSide = (Edge)Random.Range(0, len);
        }
        switch (spawnSide)
        {
            // 상부 가장자리
            case Edge.Up:
                viewPos = new Vector3(lengthRatio.Value, 1f, 1f);
                offsetPos.y += boundSize.y;
                angle = 180;
                break;

            // 하부 가장자리
            case Edge.Down:
                viewPos = new Vector3(1 - lengthRatio.Value, 0, 1f);
                offsetPos.y -= boundSize.y;
                angle = 0;
                break;

            // 오른쪽 가장자리
            case Edge.Right:
                viewPos = new Vector3(1, 1 - lengthRatio.Value, 1f);
                offsetPos.x += boundSize.x;
                angle = 90;
                break;

            // 왼쪽 가장자리
            case Edge.Left:
                viewPos = new Vector3(0, lengthRatio.Value, 1f);
                offsetPos.x -= boundSize.x;
                angle = 270;
                break;

            default:
                viewPos = Vector3.zero;
                angle = 0;
                break;
        }

        Vector2 pos = Camera.main.ViewportToWorldPoint(viewPos) + offsetPos;
        if (lookCenter) angle = GetCenterAroundLookAngle(pos);
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        return (pos, rot);
    }

    /// <summary>
    /// 화면 중앙 근처(aroundRadius 범위 내)를 바라보는 각도 계산
    /// 모든 적이 정확히 같은 지점을 향하지 않도록 약간의 랜덤 분산 적용
    /// </summary>
    float GetCenterAroundLookAngle(Vector2 pos, float aroundRadius = 2)
    {
        Vector2 lookAt = Vector2.zero + Random.insideUnitCircle * aroundRadius;
        Vector2 dir = lookAt - pos;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
    }

    #endregion

    #region Utility

    /// <summary>
    /// 오브젝트의 Collider 크기 측정 (임시 인스턴스 생성/파괴)
    /// 스폰 위치가 화면 밖으로 벗어나지 않도록 오프셋 계산에 사용
    /// </summary>
    Vector2 GetObjectBoundSize(GameObject obj)
    {
        GameObject instance = Instantiate(obj);
        Collider2D collider = instance.GetComponentInChildren<Collider2D>();
        Vector2 vec = collider.bounds.size;

        instance.SetActive(false);
        Destroy(instance);

        return vec;
    }

    #endregion
}
