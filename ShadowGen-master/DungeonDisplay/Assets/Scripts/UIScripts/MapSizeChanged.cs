using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSizeChanged : MonoBehaviour
{
    public void EditMapSize()
    {
        string input = GetComponent<InputField>().text;
        int parsedInput = 8;
        int.TryParse(input, out parsedInput);
        if (parsedInput > 0)
        {
            DungeonFile.freeFormSize = parsedInput;
        }
        else
        {
            DungeonFile.freeFormSize = 8;
        }
    }
}
