using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
	public Text currentCoords, forwardCoords;
	public CharController currentChar;

	void Update()
	{
		if (MapCreator.map != null)
		{
			currentCoords.text = currentChar.coordinates.ToString();
			forwardCoords.text = currentChar.coordinates.Forward(currentChar).ToString();
		}
	}
}
