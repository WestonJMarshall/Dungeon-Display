using System.Collections;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

public class Steam : MonoBehaviour
{
    private void Start()
    {
        try
        {
            SteamClient.Init(1363650);
        }
        catch { }
    }

    void FixedUpdate()
    {
        SteamClient.RunCallbacks();
    }
}
