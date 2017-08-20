using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Controls properties of a single tile */
//to be updated for use with pathfinding hueristics
public class Tile : MonoBehaviour
{
	/* Fields */
	#region
	//reference to tile prefab
	public Transform tile;
	//reference to physics material to add to collider
	public PhysicMaterial mat;
	//coordinates contains x position, z position and floor
	public Coords coordinates;
	public int Size
	{
		get
		{
			return (int)transform.localScale.y;
		}
	}
	public int Offset
	{
		get
		{
			return (int)transform.position.y;
		}
	}
	public int Height
	{
		get
		{
			return Offset + Size;
		}
  	}
	public Vector3 Scale
	{
		get
		{
			return tile.localScale;
		}
	}
	#endregion

	public void Init(Coords coordinates, Vector2 sizeOffset)
	{
		//adjust properties of the holder
		this.coordinates = coordinates;
		if (sizeOffset.x < 0)
		{
			sizeOffset.x = 0;
		}
		transform.position = new Vector3(this.coordinates.x * tile.localScale.x, sizeOffset.y, this.coordinates.z * tile.localScale.z);
		transform.localScale = new Vector3(1f, sizeOffset.x, 1f);
		tile.gameObject.SetActive(sizeOffset.x > 0);
		this.coordinates.y = Height;
		BoxCollider col = tile.gameObject.AddComponent<BoxCollider>();
		col.material = mat;
	}
}
