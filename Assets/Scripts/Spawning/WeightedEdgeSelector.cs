using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가중치 기반 Edge 선택 유틸리티
/// 이전 스폰 위치의 반대편에 높은 확률을 부여하여 균형 잡힌 스폰 분포 제공
/// </summary>
[Serializable]
public class WeightedEdgeSelector
{
    private Edge lastSpawnEdge = Edge.Random;

    [Header("Weight Settings")]
    [SerializeField] private float baseWeight = 1f;           // 기본 가중치
    [SerializeField] private float oppositeWeight = 3f;       // 반대편 가중치 배율

    /// <summary>
    /// 기본 설정으로 초기화
    /// </summary>
    public WeightedEdgeSelector()
    {
        this.baseWeight = 1f;
        this.oppositeWeight = 3f;
    }

    /// <summary>
    /// 커스텀 가중치로 초기화
    /// </summary>
    /// <param name="baseWeight">기본 가중치 (다른 면들)</param>
    /// <param name="oppositeWeight">반대편 가중치 배율</param>
    public WeightedEdgeSelector(float baseWeight, float oppositeWeight)
    {
        this.baseWeight = baseWeight;
        this.oppositeWeight = oppositeWeight;
    }

    /// <summary>
    /// 가중치 기반으로 다음 스폰 Edge 선택
    /// </summary>
    public Edge GetNextEdge()
    {
        // 가중치 맵 초기화
        Dictionary<Edge, float> weights = new Dictionary<Edge, float>
        {
            { Edge.Up, baseWeight },
            { Edge.Down, baseWeight },
            { Edge.Left, baseWeight },
            { Edge.Right, baseWeight }
        };

        // 이전 스폰이 있었다면, 반대편에 높은 가중치 부여
        if (lastSpawnEdge != Edge.Random)
        {
            Edge opposite = GetOppositeEdge(lastSpawnEdge);
            weights[opposite] = baseWeight * oppositeWeight;
        }

        // 가중치 기반 무작위 선택
        Edge selected = SelectWeightedRandom(weights);

        // 선택된 Edge 저장
        lastSpawnEdge = selected;

        return selected;
    }

    /// <summary>
    /// 반대편 Edge 계산
    /// </summary>
    private Edge GetOppositeEdge(Edge edge)
    {
        switch (edge)
        {
            case Edge.Up:     return Edge.Down;
            case Edge.Down:   return Edge.Up;
            case Edge.Left:   return Edge.Right;
            case Edge.Right:  return Edge.Left;
            default:          return Edge.Random;
        }
    }

    /// <summary>
    /// 가중치 기반 무작위 선택 (Weighted Random Selection)
    /// </summary>
    private Edge SelectWeightedRandom(Dictionary<Edge, float> weights)
    {
        // 총 가중치 계산
        float totalWeight = 0f;
        foreach (var weight in weights.Values)
        {
            totalWeight += weight;
        }

        // 0~totalWeight 범위에서 무작위 값 선택
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        // 누적 가중치로 선택
        float cumulativeWeight = 0f;
        foreach (var kvp in weights)
        {
            cumulativeWeight += kvp.Value;
            if (randomValue <= cumulativeWeight)
            {
                return kvp.Key;
            }
        }

        // 폴백 (이론적으로 도달 불가)
        return Edge.Up;
    }

    /// <summary>
    /// 가중치 설정 변경
    /// </summary>
    public void SetWeights(float baseWeight, float oppositeWeight)
    {
        this.baseWeight = baseWeight;
        this.oppositeWeight = oppositeWeight;
    }

    /// <summary>
    /// 이전 스폰 위치 초기화 (새 게임 시작 등)
    /// </summary>
    public void Reset()
    {
        lastSpawnEdge = Edge.Random;
    }

    /// <summary>
    /// 현재 설정 정보 반환 (디버그용)
    /// </summary>
    public (Edge lastEdge, float baseWeight, float oppositeWeight) GetDebugInfo()
    {
        return (lastSpawnEdge, baseWeight, oppositeWeight);
    }
}
