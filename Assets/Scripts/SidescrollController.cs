using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary> Reasonably polished movement code. </summary>
public class SidescrollController : PixelPerfectBehavior {
	#region Input Simplifier
	public Dictionary<string, bool> keys;
	public Dictionary<string, bool> lastKeys;
	public void UpdateKeys() {
		if (keys == null || lastKeys == null) {
			keys = new Dictionary<string, bool>();
			lastKeys = new Dictionary<string, bool>();
			foreach (var pair in binds) { keys[pair.Key] = lastKeys[pair.Key] = false; }
		} else { var swap = keys; keys = lastKeys; lastKeys = swap; }
		foreach (var pair in binds) {
			keys[pair.Key] = Input.GetKey(pair.Value);
		}
	}
	public Dictionary<string, KeyCode> binds = new Dictionary<string, KeyCode>() {
		{ "left", KeyCode.LeftArrow },
		{ "right", KeyCode.RightArrow },
		{ "down", KeyCode.DownArrow},
		{ "up", KeyCode.UpArrow },
		{ "jump", KeyCode.Z },
		{ "shoot", KeyCode.X },
		{ "dash", KeyCode.C },
		{ "melee", KeyCode.V },
		{ "dodge", KeyCode.S },
	};
	public bool Pressed(string key) { return binds.ContainsKey(key) ? keys[key] && !lastKeys[key] : false; }
	public bool Released(string key) { return binds.ContainsKey(key) ? !keys[key] && lastKeys[key] : false; }
	public bool Held(string key) { return binds.ContainsKey(key) ? keys[key] : false; }
	#endregion

	/// <summary> Draw some debugging information </summary>
	public bool DEBUG_DRAW = false;

	[Header("Movement")]
	/// <summary> Units per second walking movement. 1 unit = 16 pixels = 1 tile </summary>
	public float walkSpeed = 7;
	/// <summary> Units per second dashing movement. 1 unit = 16 pixels = 1 tile </summary>
	public float dashSpeed = 16;
	/// <summary> Gravity acceleration per second. </summary>
	public float gravity = 33f;
	/// <summary> Terminal falling velocity. </summary>
	public float terminalVelocity = 40;
	/// <summary> Velocity applied when jumping </summary>
	public float jumpPower = 15.0f;
	/// <summary> Distance to snap to ground surface. Default = 1/16 </summary>
	public float snapDistance = 0.0625f;
	/// <summary> Distance to use to check for collision. Default = 2/16 </summary>
	public float skinWidth = 0.125f;
	/// <summary> Falling rate when clinging to a wall </summary>
	public float clingSpeed = 1f;
	/// <summary> How quickly vertical speed changes when clinging </summary>
	public float clingResponse = 11f;
	/// <summary> Friction applied when grounded </summary>
	public float groundedFriction = 255f;
	/// <summary> Friction applied when in the air </summary>
	public float friction = 30f;
	/// <summary> Delay after physics is applied before control resumes</summary>
	public float controlDelay = .3f;

	/// <summary> Velocity applied when wall kicking </summary>
	public Vector2 kickPower = new Vector2(8, 13);
	/// <summary> Velocity applied when dodging </summary>
	public Vector2 dodgePower = new Vector2(17, 4);

	/// <summary> Character sprite animator </summary>
	[Header("Links")]
	public SpriteAnimator spriteAnimator;
	/// <summary> Effect sprite animator </summary>
	public SpriteAnimator effectAnimator;
	/// <summary> Physics collider </summary>
	public Collider2D col;

	/// <summary> Sprite X direction. 1.0 if right, -1.0 if left. </summary>
	[Header("Animations")]
	public float defaultXFacing = 1.0f;
	/// <summary> Time the shoot pose is held after shooting </summary>
	public float shootTimeout = .25f;
	/// <summary> Time between button presses where combos will connect </summary>
	public float meleeComboTime = 0.8f;
	/// <summary> Percentage melee animation must finish before another melee attack can begin </summary>
	public float meleeComboPercent = .63f;
	/// <summary> Character name, used as a prefix to look up animations for the character </summary>
	public string animationPrefix = "Reimu";
	/// <summary> Last played animation </summary>
	public string currentAnimation = "";

	#region animation "auto" properties
	public SpriteAnim Climb { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim ClimbDone { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Dash { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Falling { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Float { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Hurt { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Idle { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim JumpBack { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Melee { get { return LoadSpriteAnim(MemberName() + meleePose); } }
	public SpriteAnim MeleeAir { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim MeleeClimb { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim MeleeWallCling { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Moving { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Rising { get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim WallCling { get { return LoadSpriteAnim(MemberName()); } }
	/// <summary> Helpful macro that grabs the calling member name of anything that calls it. 
	/// <para>Makes it easier to make properties utilizing the <see cref="data"/> field, eg </para> <para><code>
	/// public <see cref="JsonObject"/> Attributes { get { return data.Get&lt;<see cref="JsonObject"/>&gt;(MemberName()); } }
	/// </code></para></summary>
	/// <param name="caller"> Autofilled by compiler </param>
	/// <returns> Name of member calling this method. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string MemberName([CallerMemberName] string caller = null) { return caller; }

	#endregion

	/// <summary> Loads an animation by name. </summary>
	/// <param name="animName"> Animation name to load </param>
	/// <returns> SpriteAnim loaded from a resource, or <see cref="defaultAnim"/> if none was loaded. </returns>
	private SpriteAnim LoadSpriteAnim(string animName) {
		if (lastShoot < shootTimeout) {
			SpriteAnimAsset shootAsset = Resources.Load<SpriteAnimAsset>(animationPrefix + animName + "Shoot");
			if (shootAsset != null) { 
				currentAnimation = animName + "Shoot";
				return shootAsset.data; 
			}
		}
		SpriteAnimAsset asset = Resources.Load<SpriteAnimAsset>(animationPrefix + animName);
		currentAnimation = animName;
		return asset != null ? asset.data : defaultAnim;
	}
	/// <summary> Default animation to use if none was loaded. </summary>
	public SpriteAnim defaultAnim;
	// internal state
	// internal lets other things in the same assembly access them
	// but doesn't serialize them unless we tell it to.
	/// <summary> Current velocity. </summary>
	Vector3 velocity;
	/// <summary> Current input vector. </summary>
	Vector3 input;
	/// <summary> Current facing direction. 1.0f if facing right, -1.0f if facing left. </summary>
	float facing = 1.0f;
	/// <summary> Current movement vector. </summary>
	Vector3 movement;
	/// <summary> Current applied movement vector. </summary>
	Vector3 moved;
	/// <summary> Is the character currently on the ground this frame? </summary>
	bool isGrounded;
	/// <summary> Did the character bump their head this frame? </summary>
	bool bumpedHead;
	/// <summary> Does the character have a wall on their left this frame? </summary>
	bool wallOnLeft;
	/// <summary> Does the character have a wall on their right this frame? </summary>
	bool wallOnRight;
	/// <summary> Is the character currently dashing? </summary>
	bool dashing = false;
	/// <summary> Const for melee input recieved state  </summary>
	const int MELEE_STARTING = 0;
	/// <summary> Const for melee animation playing  </summary>
	const int MELEE_PLAYING = 1;
	/// <summary> Const for not meleeing </summary>
	const int MELEE_DONE = 2;
	/// <summary> Current melee state </summary>
	int meleeState = MELEE_DONE;
	/// <summary> Chosen melee pose </summary>
	int meleePose = 1;
	/// <summary> Timer since the last melee attack was used. </summary>
	float lastMelee = 3;
	/// <summary> Timer since the last shot was fired. </summary>
	float lastShoot = 3;
	/// <summary> Timer since the last dodge was used. </summary>
	float lastDodge = 3;
	/// <summary> Timer since the last wallkick was used. </summary>
	float lastKick = 3;
	

	/// <summary> Can the character actually move right now? </summary>
	bool canMove = true;
	/// <summary> Is the character wall-clinging? </summary>
	public bool clinging { get { return !isGrounded && (clingingLeft || clingingRight); } }
	/// <summary> Is the character wall-clinging to the left? </summary>
	public bool clingingLeft { get { return (input.x < 0 && wallOnLeft); } }
	/// <summary> Is the character wall-clinging to the right? </summary>
	public bool clingingRight { get { return (input.x > 0 && wallOnRight); } }

	/// <summary> Preallocated array for collisions </summary>
	private Collider2D[] collisions = new Collider2D[16];
	// <summary> Preallocated array for collisions </summary>
	//private Collider2D[] lastCollisions = new Collider2D[16];
	/// <summary> Preallocated array for collisions </summary>
	private RaycastHit2D[] raycastHits = new RaycastHit2D[16];
	// <summary> Preallocated array for collisions </summary>
	//private RaycastHit2D[] lastRaycastHits = new RaycastHit2D[16];

	/// <summary> See if we are currently grounded.</summary>
	public bool CheckGrounded() { return velocity.y <= 0 && IsTouching(Vector2.down * skinWidth); }
	/// <summary> Check if we will touch the ground during the next frame </summary>
	public bool CheckWillTouchGround() { return velocity.y <= 0 && IsTouching(new Vector2(0, velocity.y * Time.deltaTime)); }
	/// <summary> See if we are touching the ceiling </summary>
	public bool CheckBumpedHead() { return velocity.y > 0 && IsTouching(Vector2.up * skinWidth); }
	/// <summary> See if we are touching a wall on the left </summary>
	public bool CheckWallOnLeft() { return IsTouching(Vector3.left * skinWidth); }
	/// <summary> See if we are touching a wall on the right </summary>
	public bool CheckWallOnRight() { return IsTouching(Vector3.right * skinWidth); }
	
	/// <summary> Called by Unity on load. </summary>
	void Awake() {
		Application.targetFrameRate = 60;	
	}
	
	/// <summary> Called by Unity before first frame. </summary>
	void Start() {
		UpdateAnimation(Vector3.zero, Vector3.zero);
		if (spriteAnimator != null) { spriteAnimator.anim = Idle; }
	}
	
	/// <summary> Called by Unity every frame. </summary>
	void Update() {
		ResetPixelPerfect();
		UpdateKeys();
		
		input = Vector3.zero;
		if (Held("left")) { input.x -= 1; }
		if (Held("right")) { input.x += 1; }
		input = input.normalized;
		if (input.x != 0) {
			facing = Mathf.Sign(input.x);
			if (clinging) { facing = -facing; }
		}

		canMove = true;
		if (meleeState <= MELEE_PLAYING) {
			canMove = !isGrounded && !clinging;
		}

		lastShoot += Time.deltaTime;
		lastMelee += Time.deltaTime;
		lastDodge += Time.deltaTime;
		lastKick += Time.deltaTime;
		if (Pressed("shoot")) {
			lastShoot = 0;
		}
		if (velocity.y > 0 && Released("jump")) {
			velocity.y = 0;
		}
		
		if ((meleeState == MELEE_DONE || spriteAnimator.percent > meleeComboPercent) && Pressed("melee")) {
			meleeState = MELEE_STARTING;
			if (isGrounded && lastMelee < meleeComboTime && meleePose < 3) {
				meleePose += 1;
			} else {
				meleePose = 1;
			}
			lastMelee = 0;
		}

		if (isGrounded) {
			velocity.y = 0;
			dashing = Held("dash") && input.x != 0;

			if (canMove && Pressed("jump")) {
				velocity.y = jumpPower;
			} else if (canMove && Pressed("dodge")) {
				lastDodge = 0;
				velocity.x = dodgePower.x * -facing;
				velocity.y = dodgePower.y;
			}
		} else {
			if (clinging) {
				velocity.y = Mathf.Lerp(velocity.y, -clingSpeed, Time.deltaTime * clingResponse);
				if (canMove && Pressed("jump")) {
					lastKick = 0;
					velocity.x = kickPower.x * facing;
					velocity.y = kickPower.y;
				}
			} else {
				velocity.y -= gravity * Time.deltaTime;
			}
			if (bumpedHead) { velocity.y = 0; }
		}

		if (lastDodge > 0 && lastKick > 0) {
			velocity.x = Mathf.MoveTowards(velocity.x, 0, (isGrounded ? groundedFriction : friction) * Time.deltaTime);
			if (lastDodge < 1.0 && Released("dodge")) {
				velocity.x = 0;
			}
		}

		// if (Mathf.Abs(velocity.x) < .1) { velocity.x = 0; }
		if (velocity.y > terminalVelocity) { velocity.y = terminalVelocity; }
		movement = Vector3.zero;
		if (lastDodge >= controlDelay && lastKick >= controlDelay) {
			movement = input * (dashing ? dashSpeed : walkSpeed);
		}
		movement += velocity;

		if (!canMove) {
			movement.x = 0;
		}

		moved = Move(movement * Time.deltaTime);
		if (moved.x == 0 && velocity.x != 0) {
			velocity.x = 0;
		}

		isGrounded = CheckGrounded();
		if (velocity.y < 0 && CheckWillTouchGround()) {
			velocity.y *= .5f;
		}

		bumpedHead = CheckBumpedHead();
		wallOnLeft = CheckWallOnLeft();
		wallOnRight = CheckWallOnRight();

		UpdateAnimation(moved, movement);
		UpdateEffectAnimation();
		DebugDraw();
	}

	/// <summary> Called by Unity every frame, after every object has had its Update. </summary>
	void LateUpdate() {
		ApplyPixelPerfect();
	}

	/// <summary> Update the animation based on applied and requested movement. </summary>
	private void UpdateAnimation(Vector3 moved, Vector3 movement) {
		
		if (spriteAnimator == null) { 
			Transform check = transform.Find("Sprite");
			if (check != null) { spriteAnimator = check.GetComponentInChildren<SpriteAnimator>();  }
		}
		if (spriteAnimator == null) { return; }
		spriteAnimator.flipX = facing != Mathf.Sign(defaultXFacing);
		if (meleeState == MELEE_STARTING) {
			//Debug.Log("Melee message recieved");
			SpriteAnim meleeAnim = Melee;
			if (!isGrounded) { 
				meleeAnim = MeleeAir; 
				if (clinging) {
					meleeAnim = MeleeWallCling;
				}
			}
			//Debug.Log("chose " + meleeAnim);
			spriteAnimator.Play(meleeAnim, null, 0);
			meleeState = MELEE_PLAYING;
			return;
		}
		if (meleeState == MELEE_PLAYING) {
			SpriteAnim meleeAnim = Melee;
			if (!isGrounded) {
				meleeAnim = MeleeAir;
				if (clinging) {
					meleeAnim = MeleeWallCling;
				}
			}
			spriteAnimator.anim = meleeAnim;
			
			if (spriteAnimator.percent > 0.90f) {
				meleeState = MELEE_DONE;
			} else {
				return;
			}
		}
		
		
		if (moved.x == 0) {
			spriteAnimator.anim = Idle;
		} else {
			spriteAnimator.anim = Moving;	
			if (dashing) {
				spriteAnimator.anim = Dash;
			}
		}

		if (isGrounded) {
			lastDodge = 3;
		} else {
			spriteAnimator.anim = velocity.y > 0 ? Rising : Falling;

			if (lastDodge < 1) {
				spriteAnimator.anim = JumpBack;
			}
			if (clinging) {
				spriteAnimator.anim = WallCling;
				dashing = false;
			} 
		}

	}

	/// <summary> Match melee effect animation </summary>
	private void UpdateEffectAnimation() {
		if (effectAnimator == null) {
			Transform check = transform.Find("Effect");
			if (check != null) { effectAnimator = check.GetComponentInChildren<SpriteAnimator>(); }
		}
		if (effectAnimator == null) {
			return;
		}
		effectAnimator.animTimeout = spriteAnimator.animTimeout;
		effectAnimator.animRate = spriteAnimator.animRate;
		effectAnimator.flipX = spriteAnimator.flipX;
		if (currentAnimation.Contains("Melee")) {
			effectAnimator.anim = LoadSpriteAnim(currentAnimation + "Slash");
		} else {
			effectAnimator.anim = null;
		}
	}

	/// <summary> Draws some debug information in the editor. </summary>
	private void DebugDraw() {
		
		if (DEBUG_DRAW) {
			Vector3 a = transform.position + 2 * Vector3.up + Vector3.left;
			Vector3 c = a + Vector3.right * 2;
			float line = .1f;
			void NextLine() { a.y += line; c.y += line; }
			void DrawThinBar(float p, Color front, Color? back = null) {
				Color backc = back ?? Color.black;
				p = Mathf.Clamp01(p);
				Vector3 b = Vector3.Lerp(a, c, p);
				Debug.DrawLine(a, b, front);
				Debug.DrawLine(b, c, backc);
				NextLine();
			}
			DrawThinBar(lastMelee / meleeComboTime, Color.red);
			DrawThinBar(lastShoot / shootTimeout, Color.cyan);

			DrawThinBar(spriteAnimator.percent % 1.0f, 
				(meleeState == MELEE_PLAYING && spriteAnimator.percent < meleeComboPercent) ? Color.yellow 
				: Color.white);
			

		}
	}



	/// <summary> Attempt to move the character, and get how far they actually moved. </summary>
	/// <param name="movement"> Requested movement </param>
	/// <returns> Applied movement </returns>
	private Vector3 Move(Vector3 movement) {
		col = GetComponent<Collider2D>();
		//Physics.BoxCastNonAlloc()
		Vector3 moved = Vector3.zero;
		moved += DoMove(new Vector3(0, movement.y, 0));
		moved += DoMove(new Vector3(movement.x, 0, 0));
		return moved;
	}
	/// <summary> Move the character until they are in contact with some surface. </summary>
	/// <param name="movement"> Requested movement </param>
	/// <returns> Applied movement </returns>
	private Vector3 DoMoveUntilTouching(Vector3 movement) {
		bool move = true;
		float maxDist = movement.magnitude;
		Vector3 target = transform.position + movement;
		
		if (col != null) {
			if (col is BoxCollider2D) {
				BoxCollider2D box = col as BoxCollider2D;
				Vector3 point = box.transform.position + (Vector3)box.offset + movement;

				//
				int numCollisions = Physics2D.BoxCastNonAlloc(box.transform.position + (Vector3)box.offset, box.size, 0, Vector3.down, raycastHits, maxDist);
				if (numCollisions != 0) {
					for (int i = 0; i < numCollisions; i++) {
						if (raycastHits[i].collider == col) { continue; }
						if (!raycastHits[i].collider.isTrigger) {
							maxDist = Mathf.Min(maxDist, raycastHits[i].distance);
							// target = raycastHits[i].centroid;
							if (maxDist == 0) { move = false; break; }
							//move = false;
						}
					}
				}
			}
		}

		if (move && maxDist > skinWidth) {
			//transform.position = target;
			transform.position = transform.position + movement.normalized * (maxDist - skinWidth);
			return movement.normalized * maxDist;
		}

		return Vector3.zero;
	}
	/// <summary> Attempt to move the character, and get how far they actually moved. `</summary>
	/// <param name="movement"> Requested movement. </param>
	/// <returns> Applied movement </returns>
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

	/// <summary> Check if the character would touch something when their collider is swept along a direction </summary>
	/// <param name="sweep"> Sweep direction/distance vector </param>
	/// <returns> True if they would hit something, false otherwise. </returns>
	public bool IsTouching(Vector3 sweep) {
		if (col != null) {
			if (col is BoxCollider2D) {
				BoxCollider2D box = col as BoxCollider2D;
				Vector3 point = box.transform.position + (Vector3)box.offset;
				//float adjWidth = 1f;
				

				if (DEBUG_DRAW) { // Draw the touching check.
					void DrawBox(Vector3 p, Vector3 s, Color? color = null) {
						Color c = color ?? Color.white;
						Vector3 c1 = s; 
						Vector3 c2 = s; c2.x *= -1;
						Vector3 c3 = s; c3.x *= -1; c3.y *= -1;
						Vector3 c4 = s; c4.y *= -1;

						Debug.DrawLine(p + c1, p + c2, c);
						Debug.DrawLine(p + c3, p + c2, c);
						Debug.DrawLine(p + c3, p + c4, c);
						Debug.DrawLine(p + c1, p + c4, c);
					}
					DrawBox(point, box.size * .5f);
					DrawBox(point+sweep, box.size * .5f, Color.cyan);
				}

				Vector2 adjSize = box.size;
				//adjSize.x *= adjWidth;

				int numCollisions = Physics2D.BoxCastNonAlloc(point, adjSize, 0, sweep, raycastHits, sweep.magnitude + snapDistance);
				if (numCollisions > 0) {
					int lowest = -1;
					float lowestDistance = sweep.magnitude + snapDistance;

					for (int i = 0; i < numCollisions; i++) {
						if (raycastHits[i].collider == col) { continue; } // Skip own collider.
						if (!raycastHits[i].collider.isTrigger) {

							if (raycastHits[i].distance < lowestDistance) {
								lowest = i;
								lowestDistance = raycastHits[i].distance;
							}

						}
					}

					if (lowest >= 0) {
						return true;
					}

				}
			}
		}

		return false;
	}
}
