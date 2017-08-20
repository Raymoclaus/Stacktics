using System.Collections;
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
	public float gravity = 50f, moveSpeed = 50f, jumpForce = 20f, deceleration = 30f, fallLimit = 30f, speedLimit = 8f;
	private float accuracy = 0.01f;
	private bool isGrounded;
	[HideInInspector]
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
				break;
			}
		case CharCtrlMode.Waiting:
			{
				PhysicsEffects();
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
		//limit horizontal speed
		Vector2 speedCheck = new Vector2(vel.x, vel.z);
		if (Vector2.Distance(Vector2.zero, speedCheck) > speedLimit)
		{
			speedCheck.Normalize();
			speedCheck *= speedLimit;
			vel.x = speedCheck.x;
			vel.z = speedCheck.y;
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

		//apply velocity to rigidbody
		Vector3 calculatedVelocity = body.forward * vel.z + body.right * vel.x;
		calculatedVelocity.y = vel.y;
		rigid.velocity = calculatedVelocity;
	}

	public virtual void CheckMovementInput()
	{
		//get horizontal movement input
		if (Input.GetKey(KeyCode.W))
		{
			vel.z += Time.deltaTime * moveSpeed;
		}
		if (Input.GetKey(KeyCode.A))
		{
			vel.x -= Time.deltaTime * moveSpeed;
		}
		if (Input.GetKey(KeyCode.S))
		{
			vel.z -= Time.deltaTime * moveSpeed;
		}
		if (Input.GetKey(KeyCode.D))
		{
			vel.x += Time.deltaTime * moveSpeed;
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
		rotation.y += mouseMove.x;

		//make sure it doesn't go above a certain threshold
		if (rotation.y > 360f)
		{
			rotation.y -= 360f;
		}
		if (rotation.y < 0f)
		{
			rotation.y += 360f;
		}

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
