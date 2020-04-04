using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class TopDownController : PixelPerfectBehavior {
	
	[Header("Movement")]
	public float speed = 5;

	[Header("Links")]
	public SpriteAnimator spriteAnimator;
	public Collider2D col;

	[Header("Animations")]
	public Vector2 facing = Vector2.down;
	public SpriteAnim left;
	public SpriteAnim right;
	public SpriteAnim up;
	public SpriteAnim down;

	
	private Collider2D[] collisions = new Collider2D[16];
	private Collider2D[] lastCollisions = new Collider2D[16];

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

	private Vector3 Move(Vector3 movement) {
		col = GetComponent<Collider2D>();
		//Physics.BoxCastNonAlloc()
		Vector3 moved = Vector3.zero;
		moved += DoMove(new Vector3(movement.x, 0, 0));
		moved += DoMove(new Vector3(0, movement.y, 0));
		return moved;
	}


	private Vector3 DoMove(Vector3 movement) {
		bool move = true;

		if (col != null) {
			if (col is BoxCollider2D) {
				BoxCollider2D box = col as BoxCollider2D;
				Vector3 point = box.transform.position + (Vector3)box.offset + movement;
				int numCollisions = Physics2D.OverlapBoxNonAlloc(point, box.size, 0, collisions);
				if (numCollisions != 0) {
					for (int i = 0; i < numCollisions; i++) {
						if (collisions[i] == col) { continue; }
						if (!collisions[i].isTrigger) {
							move = false;
						}
					}
				}
			}

		}

		if (move) {
			transform.position = transform.position + movement;
			return movement;
		}
			
		return Vector3.zero;
	}

	private void UpdateAnimation(Vector3 movement) {
		if (spriteAnimator == null) { spriteAnimator = GetComponentInChildren<SpriteAnimator>(); }
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
