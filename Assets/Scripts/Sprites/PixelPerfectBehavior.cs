using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary> Base behavior class for anything intending to be pixel-perfect. </summary>
public class PixelPerfectBehavior : MonoBehaviour {
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

	/// <summary> Applies pixel perfection to this object's position. Intended to be called in LateUpdate, or somehow before rendering. </summary>
	public void ApplyPixelPerfect() {
		if (doPP) {
			didPP = true;
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

	/// <summary> Resets pixel perfection, Intended to be called at the begining of Update, or somehow after rendering. </summary>
	public void ResetPixelPerfect() {
		if (didPP) {
			didPP = false;
			transform.position -= ppOffset;
		}
	}
}
