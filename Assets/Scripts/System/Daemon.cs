using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SuperTiled2Unity;

public class Daemon : MonoBehaviour {

	public static Daemon main;
	
	public Dictionary<string, Transform> maps;

	public Transform playerPrefab;
	public Transform initialMap;
	public Vector3 initialPosition;

	public Transform activeMap;
	public Transform activePlayer;
	private FollowCam fc;

	void Awake() {
		if (main != null) {
			Destroy(gameObject);
			return;
		}
		Initialize();
	}

	void Initialize() {

		Game.Initialize();

		Game.state.map = initialMap.name;
		Game.state.point = initialPosition;
		Game.teleportOffset = new Vector3(.5f, -.5f, -1f);

		// Otherwise
		Game.TryLoadSlot(0);

		
	}

	void Start() {
		
	}
	
	void Update() {
		UpdateMap();
		UpdatePlayer();
		UpdateCamera();
		
		times[it++%times.Length] = Time.deltaTime;
	}

	float[] times = new float[32];
	int it = 0;
	void OnGUI() {
		float sum = 0;
		int max = Mathf.Min(it, times.Length);
		for (int i = 0; i < max; i++) {
			sum += times[i];
		}
		float avg = sum / max;
		float fps = 1f / avg;
		
		 
		GUILayout.Label($"FPS: {fps:F2}");
	}

	private void UpdateCamera() {
		if (fc == null) {
			fc = Camera.main.gameObject.AddComponent<FollowCam>();
		}
		if (fc != null && fc.target == null) {
			fc.target = activePlayer;
		}
	}

	private void UpdatePlayer() {
		if (activePlayer == null) {
			activePlayer = Instantiate(playerPrefab, Game.state.point, Quaternion.identity);
			activePlayer.name = activePlayer.name.Replace("(Clone)", "").Trim();
			activePlayer.gameObject.AddComponent<Player>();
		} else {
			Game.state.point = activePlayer.transform.position;
		}

		if (Game.state.teleport != null) {
			Game.state.point = Game.state.teleport.Value;
			activePlayer.position = Game.state.point;
			Game.state.teleport = null;
		}
	}

	private void UpdateMap() {
		if (activeMap == null || activeMap.name != Game.state.map) {
			if (activeMap != null) {
				Destroy(activeMap.gameObject);
			}
			activeMap = Instantiate(Resources.Load<Transform>(Game.state.map), Vector3.zero, Quaternion.identity);
			activeMap.name = activeMap.name.Replace("(Clone)", "").Trim();
		}
	}

}
