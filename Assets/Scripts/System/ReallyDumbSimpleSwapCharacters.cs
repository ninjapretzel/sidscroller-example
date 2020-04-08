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
	
	void Update() {
		if (Input.GetKeyDown(KeyCode.Equals)) {
			SidescrollController old = daemon.activePlayer.GetComponent<SidescrollController>();
			if (old != null) {
				Game.lastFacing = old.facing;
			}
			Destroy(daemon.activePlayer.gameObject);
			daemon.playerPrefab = (daemon.playerPrefab == playerPrefab1) ? playerPrefab2 : playerPrefab1;
			
		}
		
	}
	
}
