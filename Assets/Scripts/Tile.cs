using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Controls properties of a single tile */
//to be updated for use with pathfinding hueristics
public class Tile : MonoBehaviour
{
	/* Fields */
	#region
	//references to external objects or components
	private MapCreator map;
	public Transform tile;
	private BoxCollider col;
	public PhysicMaterial mat;
	public List<GameObject> sides;

	//coordinates contains x position, z position and floor
	public Coords coordinates;

	//properties of the tile
	public int Size {get { return (int)transform.localScale.y; }}
	public int Offset {get { return (int)transform.position.y; }}
	public int Height {get { return Offset + Size; }}
	public Vector3 Scale {get { return transform.localScale; }}
	#endregion

	public void Init(Coords coordinates, Vector2 sizeOffset)
	{
		map = MapCreator.map;
		//adjust properties of the holder
		this.coordinates = coordinates;
		if (sizeOffset.x <= 0)
		{
			sizeOffset.x = 0;
		}
		transform.position = new Vector3(this.coordinates.x * map.tileScale.x, sizeOffset.y, this.coordinates.z * map.tileScale.z);
		transform.localScale = new Vector3(map.tileScale.x, map.tileScale.y * sizeOffset.x, map.tileScale.z);
		this.coordinates.y = Offset;
		tile.gameObject.SetActive(Scale.y > 0f);
		DeleteSides();
	}

	private void DeleteSides()
	{
		Coords check = coordinates;
		//check if front is covered, and destroy side if so
		if (check.z > 0)
		{
			check.z--;
			CheckAndDestroy(check, sides[4]);
			check.z++;
		}
		//check if left is covered, and destroy side if so
		if (check.x > 0)
		{
			check.x--;
			CheckAndDestroy(check, sides[3]);
			check.x++;
		}
		//check if back is covered, and destroy side if so
		if (check.z < map.coords[check.x].Count - 1)
		{
			check.z++;
			CheckAndDestroy(check, sides[2]);
			check.z--;
		}
		//check if right is covered, and destroy side if so
		if (check.x < map.coords.Count - 1)
		{
			check.x++;
			CheckAndDestroy(check, sides[1]);
			check.x--;
		}
	}

	//Checks the height and offset of adjacent tiles
	private void CheckAndDestroy(Coords check, GameObject side)
	{
		//checks all floors
		for (int i = 0; i < map.coords[check.x][check.z].Count; i++)
		{
			Vector2 coordsCheck = map.coords[check.x][check.z][i];
			//if the offset is equal or lower AND the adjacent still has a greater height, remove the hidden side
			if (coordsCheck.y <= Offset && coordsCheck.x + coordsCheck.y >= Height)
			{
				sides.Remove(side);
				DestroyImmediate(side);
				break;
			}
		}
	}

	public void UpdateCollider(Coords coords)
	{
		if (coordinates.Distance(coords) > MapCreator.distTrack)
		{
			CheckForChars();
		}
		else
		{
			ActivateCollider(true);
		}
	}

	private void CheckForChars()
	{
		bool found = false;
		foreach (CharController chara in map.existingChars)
		{
			if (coordinates.Distance(chara.coordinates) <= MapCreator.distTrack)
			{
				found = true;
				break;
			}
		}
		ActivateCollider(found);
	}

	private void ActivateCollider(bool activate)
	{
		if (activate)
		{
			if (col == null)
			{
				col = tile.gameObject.AddComponent<BoxCollider>();
				col.center = Vector3.zero;
				col.size = Vector3.one;
				col.material = mat;
			}
		}
		else
		{
			if (col != null)
			{
				Destroy(col);
			}
		}
	}
}
