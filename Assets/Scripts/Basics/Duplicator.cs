using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Duplicator : MonoBehaviour {
	
	public int number = 30;
	public Vector3 area = Vector3.one;

	void Start() {
		
		for (int i = 0; i < number; i++) {

			var copy = Instantiate(this, transform.position + Vector3.Scale(Random.insideUnitSphere, area), Random.rotation);
			Destroy(copy.GetComponent<Duplicator>());

		}

	}
	
}
