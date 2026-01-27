using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImageMaker : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public bool createImage;
    public Color StartColor = Color.white;
    public float createInterval = 0.1f;
    public float fadeSpeed = 1f;          

    GameObject afterImagePrefab;

    void Start()
    {
        InitAfterImagePrefab();

        StartCoroutine(AfterImageSpawn());
    }

    void OnValidate()
    {
        InitAfterImagePrefab();
    }

    void InitAfterImagePrefab()
    {
        afterImagePrefab = new GameObject("afterImageOrigin");
        AfterImage afterImage = afterImagePrefab.AddComponent<AfterImage>();
        afterImage.fadeSpeed = fadeSpeed;
        SpriteRenderer sprite = afterImagePrefab.AddComponent<SpriteRenderer>();
        sprite.sprite = spriteRenderer.sprite;
        sprite.color = StartColor;
        afterImagePrefab.SetActive(false);
    }

    IEnumerator AfterImageSpawn()
    {
        while (true)
        {
            if (createImage) {
                GameObject currentGhost = Instantiate(afterImagePrefab, transform.position, transform.rotation);
                currentGhost.transform.localScale = transform.localScale;
                currentGhost.SetActive(true);

                Sprite currentSprite = spriteRenderer.sprite;
                currentGhost.GetComponent<SpriteRenderer>().sprite = currentSprite;

                Destroy(currentGhost, 1f);
            }            

            yield return new WaitForSeconds(createInterval);
        }

        
    }
}
