using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameChar : CharController
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
	//make "current variables" such as "currentAtk", the normal stuff will act like a constant almost
	#endregion

	public override void CoordinatesChanged()
	{
		
	}

	public virtual void InflictDamage(GameChar other)
	{
		other.TakeDamage(new float());
	}

	public virtual void TakeDamage(float damage)
	{
		
	}

	public virtual void TakeTrueDamage(float damage)
	{
		
	}
}
