using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleTrigger : MonoBehaviour {
	
	void Awake() {

		GetComponent<Renderer>().material.color = Color.red;
	}

	void OnTriggerEnter(Collider c) {
		Debug.Log($"Enter: {c}");
		var check = c.GetComponent<ThirdPersonController>();
		if (check != null) {
			GetComponent<Renderer>().material.color = new Color(Random.value, Random.value, Random.value);
		}
		

	}

	void OnTriggerExit(Collider c) {
		Debug.Log($"Exit: {c}");
		var check = c.GetComponent<ThirdPersonController>();
		if (check != null) {
			GetComponent<Renderer>().material.color = Color.red;
		}
	}
	
}
