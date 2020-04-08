using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary> Holds static game state and methods to manipulate active state via events </summary>
public static partial class Game {
	
	public static GameState state;
	
	public static bool flipX = false;
	public static bool flipY = true;
	public static bool allowMovement = true;
	public static float lastFacing = 1.0f;
	public static Vector3 teleportOffset = Vector3.zero;

	public static void Initialize() {
		state = new GameState();

		

	}

	public static void TryLoadSlot(int slot) {
		// Todo: check for a save in the given slot, and load it if it exists.
	}

	public static void Save(int slot) {

	}
	
	public static void TeleportPlayer(string map, Vector3 location) {
		state.map = map;
		if (flipX) { location.x *= -1; }
		if (flipY) { location.y *= -1; }
		state.teleport = location + teleportOffset;
	}
	
}

/// <summary> Game state/save data </summary>
public class GameState {

	public string map;
	public Vector3 point;
	public Vector3? teleport;

}
