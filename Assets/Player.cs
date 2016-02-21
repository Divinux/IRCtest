using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
public class Player : NetworkBehaviour
{

    [SyncVar]
    public int health;

    public void TakeDamage(int amount)
    {
        if (Input.GetMouseButtonDown(1))
         health -= 5;
    }
	void Update()
	{
		if (Input.GetMouseButtonDown(1))
         health -= 5;
	}
}

