using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 점차 확대되고, 흐릿해짐
public class Pulse : BulletBase
{
    [Header("Pulse Info")]        
    [SerializeField] float expansionSpeed = 1f; // 확대 속도
    [SerializeField] float expansionScale = 1f; // 최대 확대 스케일    

    float currentExpansion = 0;
    float attackableRatio = 0.8f; // 어느정도  확대 후, 희미하게 사라져 갈때 쯤은 공격 판정을 지운다.  

    public float ExpansionRadius => Coll.bounds.size.x * expansionScale;

    // Update is called once per frame
    void Update()
    {
        Sprite.transform.localScale = Vector2.one * currentExpansion;

        if (currentExpansion / expansionScale > attackableRatio) Coll.enabled = false;
        //Debug.Log("currentExpansion : " + currentExpansion);

        if (currentExpansion < expansionScale)
        {
            currentExpansion += Time.deltaTime * expansionSpeed;
        }
        else
        {
            Destroy(gameObject);
        }        
        
        float fadeStartRatio = 0.5f;
        float t;
        if (currentExpansion / expansionScale < fadeStartRatio) t = 0f;
        else t = currentExpansion / expansionScale / fadeStartRatio - 2 * fadeStartRatio;
        //Debug.Log("t : " + t);

        Color color = Sprite.color;
        color.a = Mathf.Lerp(1f, 0f, t);
        Sprite.color = color;
    }

    public override void Init(int ownerLayer, int targetLayer, int damage, int impact, float movePower, AudioClip onHitSound = null)
    {
        base.Init(ownerLayer, targetLayer, damage, impact, 0, onHitSound);
        Sprite.transform.localScale = Vector2.zero;
    }

    //private void OnDrawGizmos()
    //{
    //    if(!sprite) sprite = GetComponent<SpriteRenderer>();
    //    float size = sprite.sprite.bounds.size.x;

    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, size * expansionMax / 2f);
    //}
}
