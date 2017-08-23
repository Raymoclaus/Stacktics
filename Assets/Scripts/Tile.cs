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
	public MeshFilter meshFil;
	public MeshRenderer meshRend;

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
		this.coordinates.y = Height;
		tile.gameObject.SetActive(Scale.y > 0f);
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
