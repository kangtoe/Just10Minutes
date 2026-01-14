using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ObjectSpawner : MonoSingleton<ObjectSpawner>
{
    List<GameObject> spawned = new();
    List<GameObject> Spawned
    {
        get {
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (!spawned[i]) spawned.RemoveAt(i);
            }
            return spawned;
        }
    }

    private void Start()
    {
        spawned.Add(GameManager.Instance.PlayerShip.gameObject);
    }

    Vector2 GetObjectBoundSize(GameObject obj)
    {
        GameObject instance = Instantiate(obj);
        Collider2D collider = instance.GetComponentInChildren<Collider2D>();
        Vector2 vec = collider.bounds.size;

        instance.SetActive(false);
        Destroy(instance);

        return vec;        
    }

    void SpawnObject(GameObject objectPrefab, (Vector2, Quaternion) pointAndRotation)
    {
        if (objectPrefab == null)
        {
            Debug.Log("enemyPrefab is null");
        }       

        GameObject go = Instantiate(objectPrefab);
        var (pos, rot) = pointAndRotation;

        go.transform.position = pos;
        go.transform.rotation = rot;
        spawned.Add(go);
    }

    void SpawnObjectsAtFixed(GameObject objectPrefab, Edge spawnSide, int count, float spawnInterval)
    {        
        IEnumerator Cr()
        {
            for (int i = 1; i <= count; i++)
            {
                float lengthRatio = (float)i / (count + 1);
                var (pos, rot) = GetSpawnPointAndRotation(objectPrefab, spawnSide, lengthRatio, false);
                SpawnObject(objectPrefab, (pos, rot));                

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        StartCoroutine(Cr());
    }

    void SpawnObjectsRandomly(GameObject objectPrefab, int count, float spawnInterval)
    {        
        IEnumerator Cr()
        {
            for (int i = 1; i <= count; i++)
            {                
                var pointAndRotation = GetRandomPointAndRotation(objectPrefab, Spawned);
                SpawnObject(objectPrefab, pointAndRotation);

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        StartCoroutine(Cr());
    }

    public void SpawnObjects(GameObject objectPrefab, Edge spawnSide, int count, float spawnInterval)
    {
        if (spawnSide == Edge.Random)
        {
            if (spawnInterval == 0) spawnInterval = 0.02f;
            SpawnObjectsRandomly(objectPrefab, count, spawnInterval);
        }
        else
        {
            SpawnObjectsAtFixed(objectPrefab, spawnSide, count, spawnInterval);
        }
    }

    /// <summary>
    /// 단일 오브젝트를 즉시 스폰 (웨이브 시스템용)
    /// </summary>
    public GameObject SpawnObject(GameObject objectPrefab, Edge spawnSide)
    {
        if (objectPrefab == null)
        {
            Debug.Log("objectPrefab is null");
            return null;
        }

        var (pos, rot) = GetSpawnPointAndRotation(objectPrefab, spawnSide);
        GameObject go = Instantiate(objectPrefab);
        go.transform.position = pos;
        go.transform.rotation = rot;
        spawned.Add(go);

        return go;
    }

    (Vector2, Quaternion) GetRandomPointAndRotation(GameObject objectPrefab, List<GameObject> checkObjects = null)
    {
        bool CloseCheck(Vector3 pos, List<GameObject> checkObjects, float dist)
        {
            foreach (GameObject other in checkObjects)
            {
                if ((other.transform.position - pos).magnitude < dist) return true;
            }

            return false;
        }

        float closeDist = 2;
        int tryCount = 10;

        Vector2 pos;
        Quaternion rot;

        do
        {
            (pos, rot) = GetSpawnPointAndRotation(objectPrefab);

            tryCount--;
            if (tryCount <= 0) break;
        }
        while (checkObjects != null && CloseCheck(pos, checkObjects, closeDist)); // 너무 가까우면 재배치      

        return (pos, rot);
    }    


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

    float GetCenterAroundLookAngle(Vector2 pos, float aroundRadius = 2)
    {
        Vector2 lookAt = Vector2.zero + Random.insideUnitCircle * aroundRadius;
        Vector2 dir = lookAt - pos;        
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;  
    }
}
