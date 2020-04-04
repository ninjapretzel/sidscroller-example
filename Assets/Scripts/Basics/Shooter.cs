using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Shooter : MonoBehaviour {

	public Rigidbody bullet;
	public float force = 35;
	public Vector3 offset = Vector3.forward;

	void Awake() {
		
	}
	
	void Start() {
		
	}
	
	void Update() {
		
		if (Input.GetMouseButtonDown(0)) {
			var shot = Instantiate(bullet, transform.position + transform.rotation * offset, transform.rotation);
			shot.velocity = transform.forward * force;

		}

	}
	
}
