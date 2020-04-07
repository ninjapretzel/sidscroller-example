using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// Daemon module for some events. 
public partial class Daemon {

	public bool fading { get; set; } = false;
	public float fadeAmt { get; set; } = 0;
	public Color fadeColor { get; set; } = Color.black;

	private void DrawFade() {
		if (fadeAmt > 0) {
			Color c = fadeColor;
			c.a = fadeAmt;
			GUI.color = c;
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Resources.Load<Texture2D>("pixel"));
		}
	}
	
	public IEnumerator FadeOut(float time, Color? color = null) {
		if (fading) { yield break; }
		if (color != null) { fadeColor = color.Value; }

		float timeout = 0;
		while (time > 0 && timeout < time) {
			timeout += Time.unscaledDeltaTime;
			fadeAmt = timeout / time;
			yield return new WaitForEndOfFrame();
		}
		fadeAmt = 1;
		fading = false;
	}

	public IEnumerator FadeIn(float time) {
		if (fading) { yield break; }
		fading = true;
		float timeout = 0;
		while (time > 0 && timeout < time) {
			timeout += Time.unscaledDeltaTime;
			fadeAmt = 1.0f - timeout / time;
			yield return new WaitForEndOfFrame();
		}
		fadeAmt = 0;
		fading = false;
	}

	public void LockMovement() {
		Game.allowMovement = false;
	}

	public void UnlockMovement() {
		Game.allowMovement = true;
	}



}

public partial class Game {
	public static IEnumerator FadeIn(float time) { return Daemon.main.FadeIn(time); }
	public static IEnumerator FadeOut(float time) { return Daemon.main.FadeOut(time); }
	public static void LockMovement() { Daemon.main.LockMovement(); }
	public static void UnlockMovement() { Daemon.main.UnlockMovement(); }
}
