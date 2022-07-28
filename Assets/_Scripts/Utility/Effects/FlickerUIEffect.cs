using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlickerUIEffect : MonoBehaviour
{
    [SerializeField]
    Image flickerImage;

    [SerializeField]
    private float minWaitOffTime = 0.01f;
    [SerializeField]
    private float maxWaitOffTime = 0.1f;
    [SerializeField]
    private float minWaitOnTime = 0.1f;
    [SerializeField]
    private float maxWaitOnTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        flickerImage = GetComponent<Image>();
        StartCoroutine(ToggleBetweenImages());
    }

    IEnumerator ToggleBetweenImages()
    {
        while (gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(Random.Range(minWaitOffTime, maxWaitOffTime));
            flickerImage.enabled = false;

            yield return new WaitForSeconds(Random.Range(minWaitOnTime, maxWaitOnTime));
            flickerImage.enabled = true;
        }      
    }  
}
