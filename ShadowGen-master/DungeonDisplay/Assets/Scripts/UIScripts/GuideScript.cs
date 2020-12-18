using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuideScript : MonoBehaviour
{
    public List<GameObject> pages;
    public int pageNumber = 0;

    public void LeftButtonPressed()
    {
        if(pageNumber != 0)
        {
            pageNumber--;
            pages[pageNumber].SetActive(!pages[pageNumber].activeSelf);
            pages[pageNumber + 1].SetActive(!pages[pageNumber + 1].activeSelf);
        }
    }
    public void RightButtonPressed()
    {
        if (pageNumber != pages.Count - 1)
        {
            pageNumber++;
            pages[pageNumber].SetActive(!pages[pageNumber].activeSelf);
            pages[pageNumber - 1].SetActive(!pages[pageNumber - 1].activeSelf);
        }
    }
}
