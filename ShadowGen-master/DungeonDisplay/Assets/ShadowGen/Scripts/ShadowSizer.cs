using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowSizer : MonoBehaviour
{
    void Start()
    {
        StartCoroutine("ChangeScale");
    }

    public IEnumerator ChangeScale()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        transform.localScale = new Vector3(Manager.Instance.tiles.GetLength(0), 1, Manager.Instance.tiles.GetLength(1));

        transform.localScale *= 0.1f;
    }
}
