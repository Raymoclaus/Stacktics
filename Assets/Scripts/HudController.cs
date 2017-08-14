using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
	public Text currentCoords;
	public Transform currentChar;

	void Update()
	{
		if (MapCreator.map != null)
		{
			currentCoords.text = new Coords(currentChar.position).ToString();
		}
	}
}
