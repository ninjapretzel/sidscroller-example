using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SimpleProjectile : PixelPerfectBehavior {
	[Header("Projectile Settings")]
	public Vector2 velocity = Vector2.right * 10;
	public float power = 5;
	public float lifetime = 3;
	
	void Update() {
		ResetPixelPerfect();

		lifetime -= Time.deltaTime;
		if (lifetime == 0) {
			Die();
			return;
		}

		Vector3 moveAttempt = velocity * Time.deltaTime;
		Vector3 moveGet = Move(moveAttempt);
		if (moveGet != moveAttempt) {
			Die();
			return;
		}
		

	}

	void Die() {
		Destroy(gameObject);
	}

	void LateUpdate() {
		ApplyPixelPerfect();
	}
	
}
