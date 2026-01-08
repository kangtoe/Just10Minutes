using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 반복해서 흘러가는 이미지

// 이미지 소스 스프라이트 설정 : wrap mode = repeat
// 이미지의 메터리얼 설정 : UI/Shader/Unlit/Transparent
// 이미지 소스 스프라이트를 메터리얼의 texture로도 사용할 것

public class FlowImage : MonoBehaviour
{
    Image image;
    Material InstancedMaterial;

    [SerializeField]
    float xSpeed = 0.1f;
    [SerializeField]
    float ySpeed = 0.1f;

    private void Start()
    {
        image = GetComponent<Image>();
        // 공유 머티리얼을 수정하지 않도록 새 인스턴스 생성
        InstancedMaterial = new Material(image.material);
        image.material = InstancedMaterial;
    }

    private void Update()
    {
        InstancedMaterial.mainTextureOffset += new Vector2(xSpeed * Time.deltaTime, ySpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지
        if (InstancedMaterial != null)
        {
            Destroy(InstancedMaterial);
        }
    }
}
