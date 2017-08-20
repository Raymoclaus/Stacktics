using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameChar : CharController
{
	/* Fields */
	#region
	//character stats
	[Header("Character Stats")]
	public int move;
	public int jump;
	public float atk, mAtk, def, mDef, spd, hp, nrg, mana;
	public float fireAtk, waterAtk, iceAtk, earthAtk, windAtk, thunderAtk, lightAtk, darkAtk;
	public float fireRes, waterRes, iceRes, earthRes, windRes, thunderRes, lightRes, darkRes;
	public float statusRes, debuffRes;
	#endregion
}
