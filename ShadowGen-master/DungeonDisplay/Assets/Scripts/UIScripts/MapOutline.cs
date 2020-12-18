using System.Collections;
using UnityEngine;

public class MapOutline : MonoBehaviour
{
    void Start()
    {
        StartCoroutine("UpdateMapSize");
    }

    public IEnumerator UpdateMapSize()
    {
        yield return new WaitForEndOfFrame();
        GetComponent<SpriteRenderer>().size = new Vector2(Manager.Instance.tiles.GetLength(0) + 4, Manager.Instance.tiles.GetLength(1) + 4);
        transform.position -= new Vector3(0.5f, 0.5f, 0);
    }
}
