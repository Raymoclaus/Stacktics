﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CharCtrlMode {FreeMovement, Waiting, AIControlled}

public class CharController : MonoBehaviour
{
	/* Fields */
	#region
	//references to external objects/components
	public BoxCollider col;
	public Rigidbody rigid;
	public Transform body;
	public CameraController cam;
	public Transform camLookAt;

	//control player's state
	[HideInInspector]
	public CharCtrlMode mode;
	[HideInInspector]
	public bool cameraFollowing;

	//related to player movement
	private Vector3 vel = Vector3.zero;
	public float gravity, moveSpeed, jumpForce, deceleration;
	public float fallLimit;
	private float accuracy = 0.01f;
	private bool isGrounded;
	public Vector3 rotation = Vector3.zero;
	#endregion

	void Start()
	{
		mode = CharCtrlMode.Waiting;
	}

	void Update()
	{
		switch(mode)
		{
		case CharCtrlMode.FreeMovement:
			{
				if (cameraFollowing)
				{
					CheckRotationInput();
				}
				CheckMovementInput();
				PhysicsEffects();
				rigid.velocity = vel;
				break;
			}
		case CharCtrlMode.Waiting:
			{
				PhysicsEffects();
				rigid.velocity = vel;
				break;
			}
		case CharCtrlMode.AIControlled:
			{
				break;
			}
		}
	}

	private void PhysicsEffects()
	{
		//push player downwards due to gravity if they are not on the ground
		isGrounded = CheckDirection(Vector3.down * (0.5f + accuracy));
		if (!isGrounded)
		{
			vel.y -= Time.deltaTime * gravity;
		}
		//reset downward velocity if grounded
		else if (vel.y < 0)
		{
			vel.y = 0;
		}

		//limit falling speed
		if (vel.y < -fallLimit)
		{
			vel.y = -fallLimit;
		}

		//unfortunate Unity bug requires this
		//Occasionally rigidbody gets stuck on colliders even when it doesn't look like it should
		if (CheckStuck())
		{
			transform.position += Vector3.up * accuracy;
			transform.position += vel * accuracy;
		}

		//gradually slow down horizontal movement
		vel.x = Mathf.MoveTowards(vel.x, 0f, Time.deltaTime * deceleration);
		vel.z = Mathf.MoveTowards(vel.z, 0f, Time.deltaTime * deceleration);
	}

	private void CheckMovementInput()
	{
		//disallow player to do vertical movement except via a jump
		Vector3 fwd = body.forward * moveSpeed;
		Vector3 rht = body.right * moveSpeed;

		//get horizontal movement input
		if (Input.GetKey(KeyCode.W))
		{
			vel.x = fwd.x;
			vel.z = fwd.z;
		}
		if (Input.GetKey(KeyCode.A))
		{
			vel.x = -rht.x;
			vel.z = -rht.z;
		}
		if (Input.GetKey(KeyCode.S))
		{
			vel.x = -fwd.x;
			vel.z = -fwd.z;
		}
		if (Input.GetKey(KeyCode.D))
		{
			vel.x = rht.x;
			vel.z = rht.z;
		}

		//check for jump input
		if (isGrounded && Input.GetKeyDown(KeyCode.Space))
		{
			vel.y = jumpForce;
		}
	}

	private void CheckRotationInput()
	{
		//get horizontal mouse input
		Vector2 mouseMove = cam.cam.ScreenToViewportPoint(new Vector2(Input.GetAxis("Mouse X"), 0f));

		//invert horizontal input based on settings
		mouseMove.x *= cam.invertedX ? -1f : 1f;

		//adjust rotation speed based on settings
		mouseMove.x *= cam.freeRotateSpeed;

		//calculate rotation
		rotation += Vector3.up * mouseMove.x;

		//apply rotation
		body.eulerAngles += Vector3.up * mouseMove.x;
	}

	private bool CheckDirection(Vector3 direction)
	{
		return Physics.CheckBox(col.bounds.center + direction,
			col.bounds.extents - Vector3.one / 2F,
			transform.rotation,
			1 << LayerMask.NameToLayer("Ground"));
	}

	private bool CheckStuck()
	{
		bool[] check = new bool[2];
		Vector3 velCheck = vel;
		velCheck.Normalize();
		velCheck *= accuracy;

		check[0] = Physics.CheckBox(col.bounds.center + velCheck,
			col.bounds.extents,
			transform.rotation,
			1 << LayerMask.NameToLayer("Ground"));
		check[1] = Physics.CheckBox(col.bounds.center + velCheck,
			col.bounds.extents - Vector3.up * accuracy,
			transform.rotation,
			1 << LayerMask.NameToLayer("Ground"));

		return check[0] && !check[1];
	}
}