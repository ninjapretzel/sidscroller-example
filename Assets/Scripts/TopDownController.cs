using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class TopDownController : PixelPerfectBehavior {
	
	[Header("Movement")]
	public float speed = 5;


	[Header("Animations")]
	public Vector2 facing = Vector2.down;
	public SpriteAnim left;
	public SpriteAnim right;
	public SpriteAnim up;
	public SpriteAnim down;
	
	void Awake() {
		
		
	}
	
	void Start() {

		UpdateAnimation(Vector3.zero);
		if (spriteAnimator != null) { spriteAnimator.anim = down; }
	}

	void OnDrawGizmos() {

	}
	
	void Update() {

		ResetPixelPerfect();
		Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);
		input = input.normalized;
		Vector3 movement = input * speed * Time.deltaTime;
		movement = Move(movement);
		
		UpdateAnimation(movement);
		

	}

	void LateUpdate() {
		ApplyPixelPerfect();
	}
	

	private void UpdateAnimation(Vector3 movement) {
		if (spriteAnimator == null) { return; }

		if (movement.magnitude == 0) {
			spriteAnimator.animRate = 0.0f;
		} else {
			spriteAnimator.animRate = 1.0f;
			Vector2 f = movement.normalized;
			Vector2 fa = new Vector2(Mathf.Abs(f.x), Mathf.Abs(f.y));
			if (fa.x > fa.y) {
				facing = f.x > 0 ? Vector2.right : Vector2.left;
			} else {
				facing = f.y > 0 ? Vector2.up: Vector2.down;
			}
		}

		SpriteAnim selected = down;
		if (facing.x < 0) { selected = left; }
		if (facing.x > 0) { selected = right; }
		if (facing.y > 0) { selected = up; }
		if (facing.y < 0) { selected = down; }
		
		spriteAnimator.anim = selected;
		
			

	}
	
}
