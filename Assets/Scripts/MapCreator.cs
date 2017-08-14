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
	public static MapCreator map;

	//nested list of coordinates to place tiles. Each Vector2 determines vertical size and vertical offset
	//coords[horizontal level (x)][z inset][vertical level (y)]
	private List<List<List<Vector2>>> coords = new List<List<List<Vector2>>>();
	//references to all the tiles created by this class
	public List<List<List<Tile>>> tiles = new List<List<List<Tile>>>();
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
		//Enforce this MapCreator instance as a Singleton
		if (map == null)
		{
			map = this;
		}
		else
		{
			Destroy(gameObject);
		}

		//if no coordinates are provided create a basic default map of coordinates
		CreateRandomMatrix();
		//once the coords have been filled out, instantiate copies of the landTile at each coordinate
		CreateLandTiles();
		//set the center position of the camera
		if (camCtrl != null)
		{
			camCtrl.mapCenter = Center;
			camCtrl.orthoTarget.position = Center;
			camCtrl.SetMode(CameraMode.OrthoFreeMode);
		}
	}

	private void CreateRandomMatrix()
	{
		//some arbitrary values for creating a default level
		int width = Random.Range(10, 20), length = Random.Range(10, 20), levels = 2;

		for (int i = 0; i < width; i++)
		{
			coords.Add(new List<List<Vector2>>());
			for (int j = 0; j < length; j++)
			{
				coords[i].Add(new List<Vector2>());
				for (int k = 0; k < levels; k++)
				{
					int randSize = Random.Range(1, 9);

					if (k > 0 && Random.Range(0, 4) > 0)
					{
						randSize = 0;
					}

					coords[i][j].Add(new Vector2(randSize, k * 8));
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
					if (coords[i][j][k].x > 0)
					{
						//create new tile
						Tile newTile = (Tile)Instantiate(tilePrefab, transform);
						newTile.Init(new Coords(i, j, k), coords[i][j][k]);
						//store new tile in the tiles nested list
						tiles[i][j].Add(newTile);

						mapSize.y = k > mapSize.y ? k : mapSize.y;
					}
				}

				mapSize.z = j > mapSize.z ? j : mapSize.z;
			}

			mapSize.x = i > mapSize.x ? i : mapSize.x;
		}

		mapSize.Scale(tilePrefab.Scale);
	}
}

public struct Coords
{
	public int x, z, y, floor;

	public Coords(int x, int z, int floor)
	{
		this.x = x;
		this.z = z;
		this.y = 1;
		this.floor = floor;
	}

	public Coords(int x, int z, int y, int floor)
	{
		this.x = x;
		this.z = z;
		this.y = y;
		this.floor = floor;
	}

	public Coords(Vector3 pos)
	{
		Vector3 size = MapCreator.map.tileSize;

		x = (int)(pos.x / size.x);
		z = (int)(pos.z / size.z);
		y = 1;

		List<Tile> floors = MapCreator.map.tiles[x][z];

		int f = 0;
		for (int i = floors.Count - 1; i >= 0; i--)
		{
			if (floors[i].Height < (int)pos.y)
			{
				f = i + 1;
				y = floors[i].Height;
				break;
			}
		}
		floor = f;
	}

	public override string ToString()
	{
		return string.Format("Coordinates: ({0}, {1}, {2})\nFloor: {3}", x, z, y, floor);
	}
}
