using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class EditorFontFix  : EditorWindow {
	[MenuItem("Misc/Fix Ugly Font Tool")]
	public static void Fix() {
		EditorWindow.GetWindow<EditorFontFix>().Show();
	}

	public Font selected = null;
	
	public void OnGUI() {
		Vector2 size = minSize;
		size.y = 24;
		minSize = size;

		selected = (Font) EditorGUILayout.ObjectField("Font: ", selected, typeof(Font), false);

		if (selected != null) {

			GUISkin skin;
			// this method may only be called during OnGUI()...
			skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			Fix(skin);
			skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game);
			Fix(skin);
			skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
			Fix(skin);
		
		}

	}

	public void Fix(GUISkin skin) {
		Font f = selected;

		skin.font = f;
		skin.box.font = f;
		skin.label.font = f;
		skin.button.font = f;
		skin.horizontalSlider.font = f;
		skin.horizontalSliderThumb.font = f;
		skin.horizontalScrollbar.font = f;
		skin.horizontalScrollbarLeftButton.font = f;
		skin.horizontalScrollbarRightButton.font = f;
		skin.horizontalScrollbarThumb.font = f;
		skin.verticalSlider.font = f;
		skin.verticalSliderThumb.font = f;
		skin.verticalScrollbar.font = f;
		skin.verticalScrollbarUpButton.font = f;
		skin.verticalScrollbarDownButton.font = f;
		skin.verticalScrollbarThumb.font = f;
		skin.toggle.font = f;
		skin.scrollView.font = f;
		skin.textArea.font = f;
		skin.textField.font = f;
		skin.window.font = f;
		foreach (var s in skin.customStyles) {
			s.font = f;
		}
	}


}
