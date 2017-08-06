using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMode {FreeMovement, Waiting, AIControlled}

public class PlayerController : MonoBehaviour
{
	/* Fields */
	#region
	//references to external objects/components
	public CapsuleCollider col;
	public Rigidbody rigid;
	public CameraController cam;

	//control player's state
	public PlayerMode mode = PlayerMode.FreeMovement;

	//related to player movement
	private Vector3 vel = Vector3.zero;
	public float gravity, moveSpeed, jumpForce, deceleration;
	public float speedLimit, fallLimit;
	private float accuracy = 0.01f;
	private bool isGrounded;
	#endregion

	void Update()
	{
		switch(mode)
		{
		case PlayerMode.FreeMovement:
			{
				if (cam.mode == CameraMode.PerspFollowMode && !cam.TransitioningToPerspFollow)
				{
					CheckMovementInput();
					CheckRotationInput();
				}
				PhysicsEffects();
				rigid.velocity = vel;
				break;
			}
		case PlayerMode.Waiting:
			{
				break;
			}
		case PlayerMode.AIControlled:
			{
				break;
			}
		}
	}

	private void PhysicsEffects()
	{
		//push player downwards due to gravity if they are not on the ground
		isGrounded = CheckDirection(Vector3.down * accuracy);
		if (!isGrounded)
		{
			vel.y -= Time.deltaTime * gravity;
		}
		//reset downward velocity if grounded
		else if (vel.y < 0)
		{
			vel.y = 0;
		}
		Debug.Log(isGrounded);

		//stop horizontal velocity if colliding with a wall
//		if (CheckDirection(Vector3.right * (Mathf.Abs(vel.x) / vel.x) * accuracy))
//		{
//			vel.x = 0;
//		}
//		if (CheckDirection(Vector3.forward * (Mathf.Abs(vel.z) / vel.z) * accuracy))
//		{
//			vel.z = 0;
//		}

		//gradually slow down horizontal movement
		vel.x = Mathf.MoveTowards(vel.x, 0f, Time.deltaTime * deceleration);
		vel.z = Mathf.MoveTowards(vel.z, 0f, Time.deltaTime * deceleration);
	}

	private void CheckMovementInput()
	{
		//disallow player to do vertical movement except via a jump
		Vector3 fwd = transform.forward * moveSpeed * Time.deltaTime;
		Vector3 rht = transform.right * moveSpeed * Time.deltaTime;

		//get horizontal movement input
		if (Input.GetKey(KeyCode.W))
		{
			vel += fwd;
		}
		if (Input.GetKey(KeyCode.A))
		{
			vel -= rht;
		}
		if (Input.GetKey(KeyCode.S))
		{
			vel -= fwd;
		}
		if (Input.GetKey(KeyCode.D))
		{
			vel += rht;
		}

		//limit the speed that the player can move
		if (Mathf.Abs(vel.x) > speedLimit)
		{
			vel.x = speedLimit * Mathf.Abs(vel.x) / vel.x;
		}
		if (Mathf.Abs(vel.z) > speedLimit)
		{
			vel.z = speedLimit * Mathf.Abs(vel.z) / vel.z;
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

		//apply rotation
		transform.eulerAngles += Vector3.up * mouseMove.x;
	}

	private bool CheckDirection(Vector3 direction)
	{
		return Physics.CheckCapsule(
			col.bounds.center + direction + Vector3.down * (col.bounds.extents.y - col.radius),
			col.bounds.center + direction + Vector3.up * (col.bounds.extents.y - col.radius),
			col.radius, 1 << LayerMask.NameToLayer("Ground"));
	}
}
