using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 위쪽으로 이동 & 서서히 흐려지다가 사라지는 택스트
public class FloatingText : MonoBehaviour
{
    Text txt;

    public float moveSpeed = 1;
    public float fadeSpeed = 1;

    // Start is called before the first frame update
    void Start()
    {
        txt = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(0, moveSpeed * Time.deltaTime, 0);

        // 이미지를 흐릿하게
        Color color = txt.color;
        color.a -= Time.deltaTime * fadeSpeed;
        txt.color = color;

        if (txt.color.a < 0.01f) Destroy(gameObject);
    }

    
}
