using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ReallyDumbSimpleSwapCharacters : MonoBehaviour {

	public Transform playerPrefab1;
	public Transform playerPrefab2;
	Daemon daemon;

	void Awake() {
		daemon = GetComponent<Daemon>();
		
		
	}
	
	void Start() {
		daemon.playerPrefab = playerPrefab1;


		
	}
	
	void LateUpdate() {
		if (Input.GetKeyDown("k")) {
			Destroy(daemon.activePlayer.gameObject);
			daemon.playerPrefab = (daemon.playerPrefab == playerPrefab1) ? playerPrefab2 : playerPrefab1;
		}
		
	}
	
}
