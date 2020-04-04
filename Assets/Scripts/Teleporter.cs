using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Teleporter : MonoBehaviour {

	public string map;
	public float x;
	public float y;
	
	void OnTriggerEnter2D(Collider2D c) {
		if (c.GetComponentInParent<Player>() !=  null) {
			Game.TeleportPlayer(map, new Vector3(x, y, 0));
		}
	}
	
}
	
