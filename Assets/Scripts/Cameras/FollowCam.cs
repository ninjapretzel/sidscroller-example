using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FollowCam : MonoBehaviour {
	
	[Serializable] public struct Settings {
		
		[Header("Offset in worldspace, ignores rotation and orbit")]
		public Vector3 worldOffset;
		[Header("Offset applied with rotation, before orbit")]
		public Vector3 offset;
		[Header("Offset applied after orbit")]
		public Vector3 orbitOffset;
		[Header("Distance of orbit")]
		public float orbitDistance;

		[Header("Matches target's rotation?")]
		public bool matchRotation;
		[Header("Angles added to rotation")]
		public Vector3 addRotation;
		[Header("Orbit angles")]
		public Vector3 orbitRotation;

		[Header("Field of view or ortho size used")]
		public float fov;
		[Header("Override to use Orthographic projection ")]
		public bool useOrtho;

		[Header("Camera's movement response")]
		public float moveDampening;
		[Header("Camera's rotation response")]
		public float turnDampening;
		[Header("Camera's field of view response")]
		public float fovDampening;

		

		/* some defaults I guess
		public Vector3 worldOffset = Vector3.zero;
		public Vector3 offset = new Vector3(0f, 1.6f, -5f);
		public Vector3 orbitOffset = new Vector3(0, 0, 0);
		public float orbitDistance = 0;


		public bool matchRotation = true;
		public Vector3 addRotation = Vector3.zero;
		public Vector3 orbitRotation = Vector3.zero;

		public float fov = 75;
		public bool useOrtho = false;

		public float moveDampening = 8f;
		public float turnDampening = 15f;
		public float fovDampening = 8f;
		*/
	}

	public Transform target = null;
	public Transform lastTarget = null;
	public Camera cam;
	public bool useSlerp;
	public bool checkForBlockers;
	public float physicalSize = .03f;

	// I wish C# was JAI
	/* using */ public Settings sets; 
	// Would remove all of theses....
	public Vector3 worldOffset { get { return sets.worldOffset; } set { sets.worldOffset = value; } }
	public Vector3 offset { get { return sets.offset; } set { sets.offset = value; } }
	public Vector3 orbitOffset { get { return sets.orbitOffset; } set { sets.orbitOffset = value; } }
	public float orbitDistance { get { return sets.orbitDistance; } set { sets.orbitDistance = value; } }
	public bool matchRotation { get { return sets.matchRotation; } set { sets.matchRotation = value; } }
	public Vector3 addRotation { get { return sets.addRotation; } set { sets.addRotation = value; } }
	public Vector3 orbitRotation { get { return sets.orbitRotation; } set {sets.orbitRotation = value; } }
	public float fov { get { return sets.fov; } set { sets.fov = value; } }
	public bool useOrtho { get { return sets.useOrtho; } set { sets.useOrtho = value; } }
	public float moveDampening { get { return sets.moveDampening; } set { sets.moveDampening = value; } }
	public float turnDampening { get { return sets.turnDampening; } set { sets.turnDampening = value; } }
	public float fovDampening { get { return sets.fovDampening; } set { sets.fovDampening = value; } }

	// Merged with Pixel Perfect Behavior, need specific timing.
	/// <summary> Global pixel size. </summary>
	public static float globalPixelSize = .0625f;

	[Header("Pixel Perfect Settings")]
	/// <summary> Toggle to disable Pixel Perfect adjustments if needed </summary>
	public bool doPP = true;
	/// <summary> Was PP applied last frame? </summary>
	bool didPP = false;

	/// <summary> Toggle if object needs to get adjusted by half of the pixel size added to X </summary>
	public bool plusHalfPPX = false;

	/// <summary> Toggle if object needs to get adjusted by half of the pixel size added to Y </summary>
	public bool plusHalfPPY = false;

	/// <summary> Current pixelSize </summary>
	public float pixelSize = .0625f;

	/// <summary> Last frame's pixel perfect offset. </summary>
	Vector3 ppOffset;
	/// <summary> Camera pixel zoom </summary>
	public int pixelZoom = 4;


	void Awake() {
		
	}
	
	void Start() {
		
	}
	
	void Update() {
		if (didPP) {
			didPP = false;
			transform.position -= ppOffset;
		}
	}

	void LateUpdate() {
		if (cam == null) { cam = GetComponent<Camera>(); }

		if (target != null) {
			if (target != lastTarget) {
				MoveExactlyToTarget();
			}

			FollowCamSettings setCheck = target.GetComponent<FollowCamSettings>();
			if (setCheck != null) { sets = setCheck.settings; }

			Vector3 lastPosition = transform.position;
			Quaternion lastRotation = transform.rotation;
			MoveExactlyToTarget();
			Vector3 targetPosition = transform.position;
			Quaternion targetRotation = transform.rotation;

			if (checkForBlockers) { // Submethod, check for blockers
				Vector3 center = target.position + 
					sets.worldOffset + sets.orbitOffset + sets.offset;

				Vector3 centerToTarget = targetPosition - center;
				float len = centerToTarget.magnitude;
				RaycastHit rayhit;

				if (Physics.Raycast(center, centerToTarget, out rayhit, len)) {
					var hint = rayhit.collider.GetComponent<FollowCamHint>();
					if (hint != null) {
						// Todo: Flesh out camera hint system?

					} else {
						targetPosition = rayhit.point + rayhit.normal * physicalSize;

					}
				}
			}

			{ // Submethod, animate position/facing
				if (moveDampening > 0) {
					if (useSlerp) {
						transform.position = Vector3.Slerp(lastPosition, targetPosition, Time.deltaTime * moveDampening);
					} else {
						transform.position = Vector3.Lerp(lastPosition, targetPosition, Time.deltaTime * moveDampening);
					}
				} else {
					transform.position = targetPosition;
				}

				if (turnDampening > 0) {
					transform.rotation = Quaternion.Lerp(lastRotation, targetRotation, Time.deltaTime * turnDampening);
				} else {
					transform.rotation = targetRotation;
				}

				if (cam != null) {
					cam.orthographic = useOrtho;

					if (fovDampening > 0) {
						cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, Time.deltaTime * fovDampening); 
						cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, fov, Time.deltaTime * fovDampening);
					} else {
						cam.fieldOfView = fov;
						cam.orthographicSize = fov;
					}
				}
					
			}

		}
		lastTarget = target;

		if (doPP) {
			didPP = true;
			cam.orthographicSize = cam.pixelHeight * pixelSize / pixelZoom / 2f;
			Vector3 pp = transform.position / pixelSize;
			pp.x = Mathf.Floor(pp.x);
			pp.y = Mathf.Floor(pp.y);
			pp.z = Mathf.Floor(pp.z);
			pp *= pixelSize;
			if (plusHalfPPX) { pp.x += pixelSize * .5f; }
			if (plusHalfPPY) { pp.y += pixelSize * .5f; }
			ppOffset = pp - transform.position;
			transform.position = pp;
		}

	}

	private void MoveExactlyToTarget() {
		Vector3 orbitPoint = transform.position = target.position + target.rotation * orbitOffset;
		if (matchRotation) {
			transform.rotation = target.rotation;
			transform.position -= target.forward * orbitDistance;
		} else {
			transform.rotation = Quaternion.identity;
			transform.position -= transform.forward * orbitDistance;
		}

		if (Mathf.Abs(orbitRotation.z) > 0) { transform.RotateAround(orbitPoint, transform.forward, orbitRotation.z); }
		if (Mathf.Abs(orbitRotation.y) > 0) { transform.RotateAround(orbitPoint, transform.up, orbitRotation.y); }
		if (Mathf.Abs(orbitRotation.x) > 0) { transform.RotateAround(orbitPoint, transform.right, orbitRotation.x); }

		transform.position += transform.rotation * offset;
		transform.position += worldOffset;

		transform.Rotate(addRotation);
		
	}
}
