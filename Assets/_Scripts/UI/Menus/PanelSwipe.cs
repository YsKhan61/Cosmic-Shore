using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PanelSwipe : MonoBehaviour, IDragHandler, IEndDragHandler{
    private Vector3 panelLocation;
    private Vector3 initialPanelLocation;
    public float percentThreshold = 0.2f; // Sensitivity of swipe detector. Smaller number = more sensitive
    public float easing = 0.5f; // Makes the transition less jarring
    public int currentScreen; // Keeps track of how many screens you have in the menu system. From 0 to 4, home = 2

    public GameObject Ship_Select;
    public GameObject Minigame_Settings;
    public GameObject Coming_Soon;

    [SerializeField] Transform NavBar;
    [SerializeField] public List<GameObject> NavSelection;
    void Start()
    {
        NavigateTo(currentScreen);
    }
    public void OnDrag(PointerEventData data) {
        float difference = data.pressPosition.x - data.position.x;
        transform.position = panelLocation - new Vector3(difference, 0, 0);
    }

    public void OnEndDrag(PointerEventData data){
        float percentage = (data.pressPosition.x - data.position.x) / Screen.width;
        if(Mathf.Abs(percentage) >= percentThreshold){
            Vector3 newLocation = panelLocation;
            if(percentage > 0 && currentScreen < transform.childCount -1){
                newLocation += new Vector3(-Screen.width, 0, 0);
                currentScreen += 1;
                UpdateNavBar(currentScreen);
            }
            else if(percentage < 0 && currentScreen > 0){
                newLocation += new Vector3(Screen.width, 0, 0);
                currentScreen -= 1;
                UpdateNavBar(currentScreen);
            }
            StartCoroutine(SmoothMove(transform.position, newLocation, easing));
            panelLocation = newLocation;
            Debug.Log(panelLocation);
        }
        else{
            StartCoroutine(SmoothMove(transform.position, panelLocation, easing));
        }
    }

    public void NavigateTo(int ScreenIndex) {
        Vector3 newLocation = new Vector3(-ScreenIndex * Screen.width, 0, 0);
        StartCoroutine(SmoothMove(transform.position, newLocation, easing));
        panelLocation = newLocation;
        currentScreen = ScreenIndex;
        UpdateNavBar(currentScreen);
    }
    IEnumerator SmoothMove(Vector3 startpos, Vector3 endpos, float seconds){
        float t = 0f;
        while(t <= 1.0){
            t += Time.deltaTime / seconds;
            transform.position = Vector3.Lerp(startpos, endpos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }
    public void OnClickHangar()
    {
        NavigateTo(3);
        Ship_Select.SetActive(false);
        Minigame_Settings.SetActive(false);
        Coming_Soon.SetActive(false);
    }
    public void OnClickRecords()
    {
        NavigateTo(1);
        Ship_Select.SetActive(false);
        Minigame_Settings.SetActive(false);
        Coming_Soon.SetActive(false);
    }
    public void OnClickMinigames()
    {
        NavigateTo(4);
        Ship_Select.SetActive(false);
        Minigame_Settings.SetActive(false);
        Coming_Soon.SetActive(false);
    }
    public void OnClickOptionsMenuButton()
    {
        NavigateTo(0);
        Ship_Select.SetActive(false);
        Minigame_Settings.SetActive(false);
        Coming_Soon.SetActive(false);
    }
    public void OnClickHome()
    {
        NavigateTo(2);
        Ship_Select.SetActive(false);
        Minigame_Settings.SetActive(false);
        Coming_Soon.SetActive(false);
    }
    public void OnClickLeft()
    {
        if (currentScreen > 0)
        {
            Vector3 newLocation = panelLocation;
            newLocation += new Vector3(Screen.width, 0, 0);
            currentScreen -= 1;
            StartCoroutine(SmoothMove(transform.position, newLocation, easing));
            panelLocation = newLocation;
            UpdateNavBar(currentScreen);
        }
        else
        {
            //I didn't want to write these empty else statements.
            //I was happy just not having else statements.
        }
    }
    public void OnClickRight()
    {
        if (currentScreen < transform.childCount - 1)
        {
            Vector3 newLocation = panelLocation;
            newLocation += new Vector3(-Screen.width, 0, 0);
            currentScreen += 1;
            StartCoroutine(SmoothMove(transform.position, newLocation, easing));
            panelLocation = newLocation;
            UpdateNavBar(currentScreen);
        }
        else
        {
            //But here they are.
            //Am I supposed to put like return void or something?
        }
    }
    public void UpdateNavBar(int index)
    {
        // Deselect them all
        for (var i = 0; i < NavBar.childCount; i++)
            NavBar.GetChild(i).GetChild(1).gameObject.SetActive(false);
        for (var i = 0; i <NavBar.childCount; i++)
            NavBar.GetChild(i).GetChild(0).gameObject.SetActive(true);

        // Select the one
        NavBar.GetChild(index+1).GetChild(0).gameObject.SetActive(false);
        NavBar.GetChild(index + 1).GetChild(1).gameObject.SetActive(true);
    }
}
