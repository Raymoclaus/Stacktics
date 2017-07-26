using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class places tiles when the level starts */
public class MapReader : MonoBehaviour
{
	/* Variable Declaration */
	#region
	//reference to land tile
	public Transform landTile;
	//nested list of coordinates to place tiles. Each Vector2 determines vertical size and vertical offset
	//coords[vertical level (y)][horizontal level (x)][z inset]
	private List<List<List<Vector2>>> coords = new List<List<List<Vector2>>>();
	//references to all the tiles created by this class
	private List<List<List<Transform>>> tiles = new List<List<List<Transform>>>();
	//store the width/height/depth of the landTile prefab
	private Vector3 tileSize;
	//center of the map
	public Vector3 Center { get { return new Vector3(coords[0].Count / 2f * tileSize.x, 0f, coords[0][0].Count / 2f * tileSize.z); } }
	#endregion


	void Start()
	{
		//get the size of the tile prefab
		tileSize = landTile.localScale;
		//if no coordinates are provided create a basic default map of coordinates
		CreateDefaultCoordinates ();
		//once the coords have been filled out, instantiate copies of the landTile at each coordinate
		CreateLandTiles();
	}

	/* This creates a flat 10x10 plane of coordinates with 1 floor */
	private void CreateDefaultCoordinates()
	{
		//some arbitrary values for creating a default level
		int levels = 2, width = 10, length = 10;
		//default vertical size of 1 and vertical offset of 0
		Vector2 defaultProperties = Vector2.right;

		for (int i = 0; i < levels; i++)
		{
			coords.Add(new List<List<Vector2>>());
			for (int j = 0; j < width; j++)
			{
				coords[i].Add(new List<Vector2>());
				for (int k = 0; k < length; k++)
				{
					coords[i][j].Add(defaultProperties);
				}
			}
		}
	}

	/* Creates copies of the tile prefab at each coordinate */
	private void CreateLandTiles()
	{
		for (int i = 0; i < coords.Count; i++)
		{
			tiles.Add(new List<List<Transform>>());
			for (int j = 0; j < coords[i].Count; j++)
			{
				tiles[i].Add(new List<Transform>());
				for (int k = 0; k < coords[i][j].Count; k++)
				{
					//Determine vertical size and vertical offset of current individual tile coordinate
					Vector3 individualSize = tileSize;
					individualSize.y = coords[i][j][k].x;
					float verticalOffset = coords[i][j][k].y;
					//create new tile
					Transform newTile = Instantiate(landTile);
					//set the x position (j), y position (i = floor) and z position (k)
					newTile.position = new Vector3(
						j * individualSize.x + individualSize.x,
						individualSize.y / 2f + verticalOffset,
						k * individualSize.z + individualSize.z);
					//store new tile in the tiles nested list
					tiles[i][j].Add(newTile);
				}
			}
		}
	}
}
