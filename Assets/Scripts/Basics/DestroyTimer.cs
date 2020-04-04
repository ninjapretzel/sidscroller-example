using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DestroyTimer : MonoBehaviour {

	public float time = 15;

	float timeout = 0;
	
	void Update() {
		timeout += Time.deltaTime;
		if (timeout > time) {
			Destroy(gameObject);
		}
	}
	
}
