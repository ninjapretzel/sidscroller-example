#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
/// <summary> Reasonably polished movement code. </summary>
public class SidescrollController : PixelPerfectBehavior {
	/// <summary> Class to hold information about charging </summary>
	[Serializable]
	public class ChargeInfo {
		public float time = .333f;
		public Color color = Color.yellow;
		public float flashRate = .25f;
		public ChargeInfo() { }
		public ChargeInfo(float time, Color color, float flashRate) { this.time = time; this.color = color; this.flashRate = flashRate; }
	}
#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ChargeInfo))]
	public class SpriteAnimCueDrawer : PropertyDrawer {
		private static readonly GUIContent TIME_PREFIX = new GUIContent("Time");
		private static readonly GUIContent COLOR_PREFIX = new GUIContent("Color");
		private static readonly GUIContent RATE_PREFIX = new GUIContent("Rate");
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.BeginProperty(position, label, property);
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			float x = position.x; float y = position.y; float h = position.height;
			float w1 = 80;
			float w2 = 80;
			float w3 = 80;
			float spacing = 5;
			Rect rect1 = new Rect(x, y, w1, h);
			x += w1 + spacing;
			Rect rect2 = new Rect(x, y, w2, h);
			x += w2 + spacing;
			Rect rect3 = new Rect(x, y, w3, h);
			x += w3 + spacing;

			EditorGUIUtility.labelWidth = 28f;
			EditorGUI.PropertyField(rect1, property.FindPropertyRelative("time"), TIME_PREFIX);
			EditorGUIUtility.labelWidth = 36f;
			EditorGUI.PropertyField(rect2, property.FindPropertyRelative("color"), COLOR_PREFIX);
			EditorGUIUtility.labelWidth = 28f;
			EditorGUI.PropertyField(rect3, property.FindPropertyRelative("flashRate"), RATE_PREFIX);

			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}
	}
#endif
		#region Input Simplifier
		/// <summary> Current key states </summary>
		public Dictionary<string, bool> keys;
	/// <summary> Last frame's key states </summary>
	public Dictionary<string, bool> lastKeys;
	/// <summary> Updates the keystates by swapping dictionaries and checking keybinds. </summary>
	public void UpdateKeys() {
		// Initialize key dictionaries if they do not exist yet.
		if (keys == null || lastKeys == null) {
			keys = new Dictionary<string, bool>();
			lastKeys = new Dictionary<string, bool>();
			foreach (var pair in binds) { keys[pair.Key] = lastKeys[pair.Key] = false; }
		} else { var swap = keys; keys = lastKeys; lastKeys = swap; } // Else, swap em
		// Loop over all binds and sample this frame's inputs
		foreach (var pair in binds) {
			keys[pair.Key] = Input.GetKey(pair.Value);
		}
	}
	/// <summary> Keybinds, key is the action name, value is the key for that action. </summary>
	public Dictionary<string, KeyCode> binds = new Dictionary<string, KeyCode>() {
		{ "left",	KeyCode.J },
		{ "right",	KeyCode.L },
		{ "down",	KeyCode.K },
		{ "up",		KeyCode.I },
		{ "jump",	KeyCode.Z },
		{ "shoot",	KeyCode.X },
		{ "dash",	KeyCode.C },
		{ "melee",	KeyCode.V },
		{ "dodge",	KeyCode.S },
	};
	/// <summary> Is the given action pressed this frame? </summary>
	public bool Pressed(string action) { return binds.ContainsKey(action) ? keys[action] && !lastKeys[action] : false; }
	/// <summary> Is the given action released this frame? </summary>
	public bool Released(string action) { return binds.ContainsKey(action) ? !keys[action] && lastKeys[action] : false; }
	/// <summary> Is the given action held this frame? </summary>
	public bool Held(string action) { return binds.ContainsKey(action) ? keys[action] : false; }
	#endregion

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

	/// <summary> Can the character use melee attacks? </summary>
	[Header("Capabilities")]
	public bool canMelee = false;
	/// <summary> Can the character dodge backwards? </summary>
	public bool canDodge = false;
	/// <summary> Can the character swim in water? </summary>
	public bool canSwim = false;
	/// <summary> Can the character cling against walls and wall jump? </summary>
	public bool canWallCling = false;
	/// <summary> Can the character dash? </summary>
	public bool canDash = false;
	/// <summary> Can the character shoot? </summary>
	public bool canShoot = false;
	/// <summary> Prefab name of projectile to use </summary>
	public string shotPrefabName = "Shot";
	/// <summary> Can the character charge their shot? </summary>
	public bool canCharge = false;

	/// <summary> Times for stages of charging </summary>
	public ChargeInfo[] chargeInfos = { 
		new ChargeInfo(.333f, new Color(1.5f, 1.5f, 1f, 1f), .25f),
		new ChargeInfo(2.00f, new Color(1.5f, 2.5f, 1f, 1f), .15f), 
	};

	/// <summary> Character sprite animator </summary>
	[Header("Links")]
	/// <summary> Effect sprite animator </summary>
	public SpriteAnimator effectAnimator;

	/// <summary> Sprite X direction. 1.0 if right, -1.0 if left. </summary>
	[Header("Animations")]
	public float defaultXFacing = 1.0f;
	/// <summary> Time the shoot pose is held after shooting </summary>
	public float shootPoseTimeout = .25f;
	/// <summary> Offset to create projectiles at </summary>
	public Vector2 shootOffset = new Vector2(1f, 0f);
	
	/// <summary> Time between button presses where combos will connect </summary>
	public float meleeComboTime = 0.8f;
	/// <summary> Percentage melee animation must finish before another melee attack can begin </summary>
	public float meleeComboPercent = .63f;
	/// <summary> Character name, used as a prefix to look up animations for the character </summary>
	public string animationPrefix = "Reimu";
	/// <summary> Last played animation </summary>
	public string currentAnimation = "";

	#region animation "auto" properties
	// These use the name of the member to dynamically load a resource.
	// There is no magic going on, the relevant helper method is below.
	// "MemberName()" returns the name of the method/property it is used within.
	// All of the names here line up with a resource object. 
	public SpriteAnim Climb				{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim ClimbDone			{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Dash				{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Falling			{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim FireCharge		{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Float				{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Hurt				{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Idle				{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim JumpBack			{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim MeleeAir			{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim MeleeClimb		{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim MeleeWallCling	{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Moving			{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim Rising			{ get { return LoadSpriteAnim(MemberName()); } }
	public SpriteAnim WallCling			{ get { return LoadSpriteAnim(MemberName()); } }

	// This one is slightly different, appends a number.
	public SpriteAnim Melee				{ get { return LoadSpriteAnim(MemberName() + meleePose); } } 
	
	/// <summary> Helpful macro that grabs the calling member name of anything that calls it. 
	/// <para>Makes it easier to make properties utilizing the <see cref="data"/> field, eg </para> <para><code>
	/// public <see cref="JsonObject"/> Attributes { get { return data.Get&lt;<see cref="JsonObject"/>&gt;(MemberName()); } }
	/// </code></para></summary>
	/// <param name="caller"> Autofilled by compiler </param>
	/// <returns> Name of member calling this method. </returns>
	public static string MemberName([CallerMemberName] string caller = null) { return caller; }
	#endregion

	/// <summary> Loads an animation by name. </summary>
	/// <param name="animName"> Animation name to load </param>
	/// <returns> SpriteAnim loaded from a resource, or <see cref="defaultAnim"/> if none was loaded. </returns>
	private SpriteAnim LoadSpriteAnim(string animName) {
		if (lastShoot < shootPoseTimeout) {
			SpriteAnimAsset shootAsset = Resources.Load<SpriteAnimAsset>(animationPrefix + "/" + animName + "Shoot");
			if (shootAsset != null) { 
				currentAnimation = animName + "Shoot";
				return shootAsset.data; 
			}
		}
		SpriteAnimAsset asset = Resources.Load<SpriteAnimAsset>(animationPrefix + "/" + animName);
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
	[NonSerialized] public float facing = 1.0f;
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
	/// <summary> Time spent charging the current shot </summary>
	public float chargeTime = 0;

	/// <summary> Can the character actually move right now? </summary>
	bool canMove = true;
	/// <summary> Is the character wall-clinging? </summary>
	public bool clinging { get { return canWallCling && !isGrounded && (clingingLeft || clingingRight); } }
	/// <summary> Is the character wall-clinging to the left? </summary>
	public bool clingingLeft { get { return (input.x < 0 && wallOnLeft); } }
	/// <summary> Is the character wall-clinging to the right? </summary>
	public bool clingingRight { get { return (input.x > 0 && wallOnRight); } }
	
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
		//UpdateAnimation(Vector3.zero, Vector3.zero);
	}
	
	/// <summary> Called by Unity before first frame. </summary>
	void Start() {
		UpdateAnimation(Vector3.zero, Vector3.zero);
		isGrounded = CheckGrounded();
		facing = Game.lastFacing;
		// if (spriteAnimator != null) { spriteAnimator.anim = Idle; }
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
		if (meleeState <= MELEE_PLAYING) { canMove = !isGrounded && !clinging; }
		if (currentAnimation == "FireCharge") { canMove = false; }

		lastShoot += Time.deltaTime;
		lastMelee += Time.deltaTime;
		lastDodge += Time.deltaTime;
		lastKick += Time.deltaTime;

		if (canShoot) {
			// if (Held("shoot")) { lastShoot = 0; }
			if (Pressed("shoot")) {
				lastShoot = 0;
				chargeTime = 0;
				Fire();
			} else if (canCharge && Held("shoot")) {
				chargeTime += Time.deltaTime;
			}

			if (canCharge && Released("shoot") && chargeInfos.Length > 0) {
				if (chargeTime > chargeInfos[0].time) {
					lastShoot = 0;
				} else {
					chargeTime = 0;
				}
			}
		}

		if (velocity.y > 0 && Released("jump")) {
			velocity.y = 0;
		}
		
		if (canMelee) {
			if ((meleeState == MELEE_DONE || spriteAnimator.percent > meleeComboPercent) && Pressed("melee")) {
				meleeState = MELEE_STARTING;
				if (isGrounded && lastMelee < meleeComboTime && meleePose < 3) {
					meleePose += 1;
				} else {
					meleePose = 1;
				}
				lastMelee = 0;
			}
		}

		if (isGrounded) {
			velocity.y = 0;
			dashing = canDash && Held("dash") && input.x != 0;

			if (canMove && Pressed("jump")) {
				velocity.y = jumpPower;
			} else if (canDodge && canMove && Pressed("dodge")) {
				lastDodge = 0;
				velocity.x = dodgePower.x * -facing;
				velocity.y = dodgePower.y;
			}
		} else {
			if (canWallCling && clinging) {
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
		Vector3 movementFromInput = Vector3.zero;
		if (lastDodge >= controlDelay && lastKick >= controlDelay) {
			movementFromInput = input * (dashing ? dashSpeed : walkSpeed);
			movement = movementFromInput;
		}
		movement += velocity;

		if (!canMove) {
			movement.x = 0;
		}

		moved = Move(movement * Time.deltaTime);
		if (moved.x == 0 && velocity.x != 0) {
			velocity.x *= .5f;
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

	public void Fire() {
		int charged = 0;
		if (chargeTime > 0 && chargeInfos != null) {
			for (int i = 0; i < chargeInfos.Length; i++) {
				if (chargeTime > chargeInfos[i].time) { charged = i + 1; }
			}
		}
		chargeTime = 0;

		Transform shotPrefab = Resources.Load<Transform>(shotPrefabName + (charged > 0 ? "" + charged : ""));
		Vector3 offset = shootOffset;
		offset.x *= facing;
		Transform shot = Instantiate(shotPrefab, transform.position + offset, Quaternion.identity);

		SimpleProjectile proj = shot.GetComponent<SimpleProjectile>();
		if (proj != null) {
			proj.velocity.x *= facing;
			proj.spriteAnimator.flipX = spriteAnimator.flipX;
		}
	}
	public void Shoot(string arg) {
		Fire();
	}

	/// <summary> Update the animation based on applied and requested movement. </summary>
	private void UpdateAnimation(Vector3 moved, Vector3 movement) {
		
		if (spriteAnimator == null) { 
			Transform check = transform.Find("Sprite");
			if (check != null) { spriteAnimator = check.GetComponentInChildren<SpriteAnimator>();  }
		}
		if (spriteAnimator == null) { return; }
		spriteAnimator.flipX = facing != Mathf.Sign(defaultXFacing);


		if (chargeTime > 0 && chargeInfos.Length > 0) {
			ChargeInfo info = chargeInfos[0];
			for (int i = 0; i < chargeInfos.Length; i++) {
				if (chargeTime > chargeInfos[i].time) {
					info = chargeInfos[i];
				}
			}
			float point = (chargeTime % info.flashRate) / info.flashRate;
			
			spriteAnimator.spriteRenderer.color = (point < .5f ? Color.white : info.color);
		} else {
			spriteAnimator.spriteRenderer.color = Color.white;
		}

		if (currentAnimation == "FireCharge") { 
			if (spriteAnimator.percent < .99) { return; }
		}
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
			if (lastShoot == 0 && chargeTime > 0) {
				spriteAnimator.Play(FireCharge, null, 0);
			}
		} else {
			spriteAnimator.anim = velocity.y > 0 ? Rising : Falling;

			if (lastDodge < 1) {
				spriteAnimator.anim = JumpBack;
			}
			if (clinging) {
				spriteAnimator.anim = WallCling;
				dashing = false;
			}
			if (lastShoot == 0 && chargeTime > 0) {
				Fire();
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
			DrawThinBar(lastShoot / shootPoseTimeout, Color.cyan);

			DrawThinBar(spriteAnimator.percent % 1.0f, 
				(meleeState == MELEE_PLAYING && spriteAnimator.percent < meleeComboPercent) ? Color.yellow 
				: Color.white);
			

		}
	}


	
}
