using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class places tiles when the level starts and keeps track of each tile */
public class MapCreator : MonoBehaviour
{
	/* Fields */
	#region
	//references to external objects
	public CameraController camCtrl;
	public Tile tilePrefab;

	//nested list of coordinates to place tiles. Each Vector2 determines vertical size and vertical offset
	//coords[horizontal level (x)][z inset][vertical level (y)]
	private List<List<List<Vector2>>> coords = new List<List<List<Vector2>>>();
	//references to all the tiles created by this class
	private List<List<List<Tile>>> tiles = new List<List<List<Tile>>>();
	//keep record of tile size
	public Vector3 tileSize
	{
		get
		{
			return tiles.Count != 0 ? tiles[0][0][0].Scale : Vector3.one;
		}
	}
	//keep track of the size of the map
	private Vector3 mapSize;
	//center of the map
	public Vector3 Center
	{
		get
		{
			return new Vector3(mapSize.x / 2f, mapSize.y, mapSize.z / 2f);
		}
	}
	#endregion

	void Start()
	{
		//if no coordinates are provided create a basic default map of coordinates
//		CreateDefaultMatrix ();
		CreateRandomMatrix();
		//once the coords have been filled out, instantiate copies of the landTile at each coordinate
		CreateLandTiles();
		//set the center position of the camera
		if (camCtrl != null)
		{
			camCtrl.mapCenter = Center;
			camCtrl.orthoFreeTarget.position = Center;
			camCtrl.SetMode(CameraMode.OrthoFreeMode);
		}
	}

	/* This creates a flat 10x10 plane of coordinates with 1 floor */
	private void CreateDefaultMatrix()
	{
		//some arbitrary values for creating a default level
		int width = 10, length = 10, levels = 1;
		//default vertical size of 1 and vertical offset of 0
		Vector2 defaultProperties = Vector2.right;

		for (int i = 0; i < width; i++)
		{
			coords.Add(new List<List<Vector2>>());
			for (int j = 0; j < length; j++)
			{
				coords[i].Add(new List<Vector2>());
				for (int k = 0; k < levels; k++)
				{
					coords[i][j].Add(defaultProperties);
				}
			}
		}
	}

	private void CreateRandomMatrix()
	{
		//some arbitrary values for creating a default level
		int width = Random.Range(10, 20), length = Random.Range(10, 20), levels = 1;

		for (int i = 0; i < width; i++)
		{
			coords.Add(new List<List<Vector2>>());
			for (int j = 0; j < length; j++)
			{
				coords[i].Add(new List<Vector2>());
				for (int k = 0; k < levels; k++)
				{
					coords[i][j].Add(new Vector2(Random.Range(1, 5), 0f));
				}
			}
		}
	}

	/* Creates copies of the tile prefab at each coordinate
	 * Also keeps track of the total map size
	 */
	private void CreateLandTiles()
	{
		for (int i = 0; i < coords.Count; i++)
		{
			tiles.Add(new List<List<Tile>>());
			for (int j = 0; j < coords[i].Count; j++)
			{
				tiles[i].Add(new List<Tile>());
				for (int k = 0; k < coords[i][j].Count; k++)
				{
					//create new tile
					Tile newTile = (Tile)Instantiate(tilePrefab, transform);
					newTile.Init(new Vector3(i, j, k), coords[i][j][k]);
					//store new tile in the tiles nested list
					tiles[i][j].Add(newTile);

					mapSize.y = k > mapSize.y ? k : mapSize.y;
				}

				mapSize.z = j > mapSize.z ? j : mapSize.z;
			}

			mapSize.x = i > mapSize.x ? i : mapSize.x;
		}

		mapSize.Scale(tilePrefab.Scale);
	}
}
