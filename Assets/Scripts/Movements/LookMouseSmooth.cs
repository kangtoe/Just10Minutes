using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LookMouseSmooth : MonoBehaviour
{
    Vector3 mousePos;
    public float rotateSpeed = 1f;

    // Update is called once per frame
    void Update()
    {          

        mousePos = GetMouseWorldPos();
        //mousePos.z = 0f;
        //Debug.Log("mosue pos : " + mousePos);
        
        LookPosSmooth(mousePos, rotateSpeed);        
    }

    Vector3 GetMouseWorldPos()
    {
        if (Mouse.current == null)
            return Vector3.zero;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        return worldPos;
    }

    void LookPosSmooth(Vector3 targetPos, float _rotateSpeed)
    {
        Vector3 dir = targetPos - transform.position;
                
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;        
        Quaternion quat = Quaternion.AngleAxis(angle - 90, Vector3.forward);        
        transform.rotation = Quaternion.Lerp(transform.rotation, quat, Time.deltaTime * _rotateSpeed);
    }

}
