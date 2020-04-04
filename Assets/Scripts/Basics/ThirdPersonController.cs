using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonController : MonoBehaviour {

	public float speed = 5;
	public float jumpPower = 6;
	public float gravity = 9.8f;
	public float snapDistance = .1f;

	public float maxPitch = 88.88f;
	public float minPitch = 20f;
	public Vector2 sensitivity = new Vector2(30, 20);
	
	Vector3 velocity;
	float yaw;
	float pitch;
	Transform head;
	CharacterController controller;

	void Awake() {
		head = transform.Find("Head");
		controller = GetComponent<CharacterController>();

	}

	void Start() {


	}

	void Update() {

		Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		Vector3 turnInput = new Vector3(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"), 0);
		bool jumpInput = Input.GetKeyDown(KeyCode.Space);
		bool jumpStopInput = Input.GetKeyUp(KeyCode.Space);

		if (controller.isGrounded) {
			velocity.y = -snapDistance;
			if (jumpInput) { 
				velocity.y = jumpPower; 
			}
		} else {
			velocity.y -= gravity * Time.deltaTime;
		}
		
		// If you want to be able to stop jumping by releasing a key
		//if (jumpStopInput && velocity.y > 0) {
		//	velocity.y = 0;
		//}



		yaw += turnInput.x * sensitivity.x;
		pitch += turnInput.y * sensitivity.y;
		pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

		transform.rotation = Quaternion.Euler(0, yaw, 0);
		head.localRotation = Quaternion.Euler(pitch, 0, 0);

		Vector3 movement = Vector3.zero;
		movement += transform.rotation * moveInput * speed;
		movement += velocity;

		CollisionFlags flags = controller.Move(movement * Time.deltaTime);

		if ((flags & CollisionFlags.Above) != CollisionFlags.None && velocity.y > 0) {
			velocity.y = 0;
		}

	}

}
