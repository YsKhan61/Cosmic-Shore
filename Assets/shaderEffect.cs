using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderEffect : MonoBehaviour
{

    [SerializeField]
    Material MutonMaterial;

    
    private float effect=.01f;


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FadeInCoroutine());
    }

    IEnumerator FadeInCoroutine()
    {
        while (effect <= 1)
        {
            yield return new WaitForSeconds(.001f);
            effect *= 1.015f;
            MutonMaterial.SetFloat("_opacity", effect);
        }
    
    }

    // Update is called once per frame
    void Update()
    {
       

    }
}
