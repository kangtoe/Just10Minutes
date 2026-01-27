using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImage : MonoBehaviour
{
    public float fadeSpeed = 1;
    public SpriteRenderer sprite;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (!sprite) sprite = gameObject.AddComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Color color = sprite.color;
        color.a -= Time.deltaTime * fadeSpeed;
        sprite.color = color;

        if (sprite.color.a < 0.01f) Destroy(gameObject);
    }
}
