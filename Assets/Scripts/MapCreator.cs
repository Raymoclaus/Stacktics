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
	public List<CharController> existingChars;
	public Transform staticHolder, nonStaticHolder;

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
	//affects the amount of tiles each charcontroller needs to keep track of
	public const int distTrack = 2;
	public Vector2 mapLength, mapWidth;
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
		CreateLandMatrix();
		RandomiseLandscape(false);
//		RandomiseLandscape(true);

		if (coords[0][0][0].x <= 1)
		{
			coords[0][0][0] = Vector2.right * 1;
		}
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

	private void CreateLandMatrix()
	{
		//some arbitrary values for creating a default level
		int width = Random.Range((int)mapWidth.x, (int)mapWidth.y), length = Random.Range((int)mapLength.x, (int)mapLength.y), levels = 1;

		for (int i = 0; i < width; i++)
		{
			coords.Add(new List<List<Vector2>>());
			for (int j = 0; j < length; j++)
			{
				coords[i].Add(new List<Vector2>());
				for (int k = 0; k < levels; k++)
				{
					coords[i][j].Add(Vector2.right * 1);
				}
			}
		}
	}

	//Modified Perlin Noise method
	private void RandomiseLandscape(bool includeNext)
	{
		int height = 0, variance = 4;

		for (int x = 0; x < coords.Count; x++)
		{
			for (int z = 0; z < coords[x].Count; z++)
			{
				List<int> surrHeights = new List<int>();

				if (x > 0)
				{
					surrHeights.Add((int)coords[x - 1][z][0].x);
				}
				if (z > 0)
				{
					surrHeights.Add((int)coords[x][z - 1][0].x);
				}
				if (x > 0 && z > 0)
				{
					surrHeights.Add((int)coords[x - 1][z - 1][0].x);
				}
				if (x > 0 && z < coords[x].Count - 1)
				{
					surrHeights.Add((int)coords[x - 1][z + 1][0].x);
				}
				if (includeNext)
				{
					if (x < coords.Count - 1)
					{
						surrHeights.Add((int)coords[x + 1][z][0].x);
					}
					if (z < coords[x].Count - 1)
					{
						surrHeights.Add((int)coords[x][z + 1][0].x);
					}
					if (x < coords.Count - 1 && z > 0)
					{
						surrHeights.Add((int)coords[x + 1][z - 1][0].x);
					}
					if (x < coords.Count - 1 && z < coords[x].Count - 1)
					{
						surrHeights.Add((int)coords[x + 1][z + 1][0].x);
					}
				}

				if (surrHeights.Count > 1)
				{
					height = 0;
					for (int i = 0; i < surrHeights.Count; i++)
					{
						height += surrHeights[i];
					}
					height = Mathf.RoundToInt((float)height / (float)surrHeights.Count);
					height = Random.Range(height - variance, height + variance + 1);
				}
				else
				{
					height = Random.Range(1, variance * 2);
				}

				coords[x][z][0] = Vector2.right * height;
			}
		}
	}

	/* Creates copies of the tile prefab at each coordinate
	 * Also keeps track of the total map size
	 */
	private void CreateLandTiles()
	{
		int count = 0;
		for (int i = 0; i < coords.Count; i++)
		{
			tiles.Add(new List<List<Tile>>());
			for (int j = 0; j < coords[i].Count; j++)
			{
				tiles[i].Add(new List<Tile>());
				for (int k = 0; k < coords[i][j].Count; k++)
				{
					//create new tile
					Tile newTile = Instantiate<Tile>(tilePrefab, staticHolder);
					newTile.Init(new Coords(i, j, k), coords[i][j][k]);
					//store new tile in the tiles nested list
					tiles[i][j].Add(newTile);
					count++;

					mapSize.y = k > mapSize.y ? k : mapSize.y;
				}

				mapSize.z = j > mapSize.z ? j : mapSize.z;
			}

			mapSize.x = i > mapSize.x ? i : mapSize.x;
		}

		mapSize.Scale(tilePrefab.Scale);

		StaticBatchingUtility.Combine(staticHolder.gameObject);
	}

	//returns a list of tiles located at (x, z) coordinates
	public List<Tile> GetTilesAtCoords(int x, int z)
	{
		if (x < 0 || x > mapSize.x || z < 0 || z > mapSize.z)
		{
			return null;
		}
		return tiles[x][z];
	}
	public List<Tile> GetTilesAtCoords(Coords coords)
	{
		if (coords.x < 0 || coords.x > mapSize.x || coords.z < 0 || coords.z > mapSize.z)
		{
			return null;
		}
		return tiles[coords.x][coords.z];
	}

	//get tile at specific coordinates
	public Tile GetSingleTile(int x, int z, int floor)
	{
		List<Tile> tilesAtCoords = GetTilesAtCoords(x, z);
		if (tilesAtCoords == null || floor > tilesAtCoords.Count)
		{
			return null;
		}
		return tiles[x][z][floor];
	}
	public Tile GetSingleTile(Coords coords)
	{
		List<Tile> tilesAtCoords = GetTilesAtCoords(coords.x, coords.z);
		if (tilesAtCoords == null || coords.floor > tilesAtCoords.Count)
		{
			return null;
		}
		return tiles[coords.x][coords.z][coords.floor];
	}

	//if existingList is not null then it will add tiles to that list, otherwise it will return a new list
	public List<List<Tile>> GetTilesAroundCoords(Coords center, int dist, List<List<Tile>> existingList)
	{
		List<List<Tile>> surrTiles = new List<List<Tile>>();
		if (existingList != null)
		{
			surrTiles = existingList;
		}

		int count = 0;
		for (int x = -dist; x <= dist; x++)
		{
			for (int z = -count; z <= count; z++)
			{
				List<Tile> tilesAtCoords = GetTilesAtCoords(center.x + x, center.z + z);
				if (tilesAtCoords != null && !surrTiles.Contains(tilesAtCoords))
				{
					surrTiles.Add(tilesAtCoords);
				}
			}
			count += x >= 0 ? -1 : 1;
		}

		return surrTiles;
	}
}

public struct Coords
{
	//required values for coordinates
	public int x, z, y, floor;

	//empty coordinates are used for when coordinates are not found or don't exist
	public static Coords Empty
	{
		get
		{
			return new Coords(-1, -1, -1, -1);
		}
	}

	//return Coords when y value is unknown
	public Coords(int x, int z, int floor)
	{
		this.x = x;
		this.z = z;
		this.y = 1;
		this.floor = floor;
	}

	//return Coords given all parameters
	public Coords(int x, int z, int y, int floor)
	{
		this.x = x;
		this.z = z;
		this.y = y;
		this.floor = floor;
	}

	//return Coords based on the position given
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

	//return coords of adjacent tile based on given a character
	public Coords Forward(CharController character)
	{
		//grab required variables
		float charRotation = character.rotation.y;
		Vector3 size = MapCreator.map.tileSize;

		//check if looking forward
		if (charRotation <= 45f || charRotation >= 315f)
		{
			return new Coords(character.transform.position + Vector3.forward * size.z);
		}
		//check if looking right
		if (charRotation >= 45f && charRotation <= 135f)
		{
			return new Coords(character.transform.position + Vector3.right * size.x);
		}
		//check if looking back
		if (charRotation >= 135f && charRotation <= 225f)
		{
			return new Coords(character.transform.position - Vector3.forward * size.z);
		}
		//check if looking left
		if (charRotation >= 225f && charRotation <= 315f)
		{
			return new Coords(character.transform.position - Vector3.right * size.x);
		}

		//if nothing was found return empty Coords
		return Empty;
	}

	public int Distance(Coords other)
	{
		return Mathf.Abs(x - other.x) + Mathf.Abs(z - other.z);
	}

	public override string ToString()
	{
		return string.Format("Coordinates: ({0}, {1}, {2})\nFloor: {3}", x, z, y, floor);
	}

	public bool IsSameXZ(Coords other)
	{
		if (x != other.x)
		{
			return false;
		}
		if (z != other.z)
		{
			return false;
		}
		return true;
	}

	public bool IsSameCoord(Coords other)
	{
		if (!IsSameXZ(other))
		{
			return false;
		}
		return floor == other.floor;
	}
}
