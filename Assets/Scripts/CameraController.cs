using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	//reference to the camera component
	private Camera cam;

	//Define camera modes
	public enum CameraMode {Orthographic, LockedPerspective, FreePerspective};
	private CameraMode camMode = CameraMode.Orthographic;
	public CameraMode CamMode
	{
		get { return camMode; }
		private set { camMode = value; }
	}

	//transition-related stuff
	private bool IsTransitioning
	{
		get { return fadingIn || fadingOut; }
	}
	private bool fadingOut, fadingIn;
	public float fadeTime;
	private float fadeCounter = 0f;
	private Vector2 nearFarClippingLimits = new Vector2();

	//reference to camera target
	public Transform target;

	//orthographic scrolling
	public float orthoScrollPadding, orthoScrollLimit;
	public Vector2 orthoZoomLimits;
	public bool smoothZooming;
	private float zoomTo, zoomCount = 0f, zoomTime = 1f;
	public Vector3 orthographicCenter = new Vector3(0f, 45f, 0f);


	void Start()
	{
		cam = GetComponent<Camera>();
		//get near and far clipping planes
		nearFarClippingLimits.x = cam.nearClipPlane;
		nearFarClippingLimits.y = cam.farClipPlane;
		zoomTo = cam.orthographicSize;
	}

	void Update()
	{
		//only run update method if not in the middle of a transition
		if (CheckTransition())
		{
			return;
		}

		switch(camMode)
		{
		case CameraMode.Orthographic:
			{
				UpdateOrthographic();
				break;
			}
		case CameraMode.LockedPerspective:
			{
				UpdateLocked();
				break;
			}
		case CameraMode.FreePerspective:
			{
				UpdateFree();
				break;
			}
		}
	}

	private bool CheckTransition()
	{
		//if already in a transition then return true
		if (IsTransitioning)
		{
			return true;
		}
		//if player is trying to start a transition then start it and return true
		if (Input.GetKeyDown(KeyCode.Space))
		{
			StartCoroutine(FadeOut());
			return true;
		}
		//otherwise return false
		return false;
	}

	private void UpdateOrthographic()
	{
		CheckOrthographicScrolling();
		CheckOrthographicZooming();
	}

	private void CheckOrthographicScrolling()
	{
		Vector2 mousePos = Input.mousePosition;
		Vector3 direction = Vector3.zero;
		/* if the cursor is near the edges of the screen then scroll camera in that direction */
		//check left side of the screen
		if (mousePos.x < orthoScrollPadding)
		{
			direction.x += -1f;
			direction.z += 1f;
		}
		//check right side of the screen
		if (mousePos.x > Screen.width - orthoScrollPadding)
		{
			direction.x += 1f;
			direction.z += -1f;
		}
		//check top side of the screen
		if (mousePos.y > Screen.height - orthoScrollPadding)
		{
			direction.x += 1f;
			direction.z += 1f;
		}
		//check bottom side of the screen
		if (mousePos.y < orthoScrollPadding)
		{
			direction.x += -1f;
			direction.z += -1f;
		}
		//apply direction to camera's position
		direction.Normalize();
		transform.position += direction * cam.orthographicSize / 30f;
		//check to see if camera has reached scroll limit
		while (Vector3.Distance(orthographicCenter, transform.position) > orthoScrollLimit)
		{
			transform.position = Vector3.MoveTowards(transform.position, orthographicCenter, 0.1f);
		}
	}

	private void CheckOrthographicZooming()
	{
		//check to see if the mouse wheel is being used
		float mouseScroll = -Input.mouseScrollDelta.y;
		if (mouseScroll != 0)
		{
			//reset zoom Count to allow for smooth scrolling
			zoomCount = 0;
			//only apply the scroll if the zoom would remain within limits
			if (zoomTo + mouseScroll >= orthoZoomLimits.x &&
				zoomTo + mouseScroll <= orthoZoomLimits.y)
			{
				zoomTo += mouseScroll;
			}
		}
		//increment zoom counter for smooth scrolling
		zoomCount += Time.deltaTime;
		//if smooth zooming is enabled then gradually zoom to calculated orthographic size
		if (smoothZooming)
		{
			cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomTo, zoomCount / zoomTime);
		}
		//otherwise just set the zoom to what it should be
		else
		{
			cam.orthographicSize = zoomTo;
		}
	}

	private void UpdateLocked()
	{
		
	}

	private void UpdateFree()
	{
		
	}

	public void SetMode(CameraMode mode)
	{
		camMode = mode;
		switch (camMode)
		{
		case CameraMode.Orthographic:
			{
				cam.orthographic = true;
				break;
			}
		case CameraMode.LockedPerspective:
			{
				cam.orthographic = false;
				break;
			}
		case CameraMode.FreePerspective:
			{
				cam.orthographic = false;
				break;
			}
		}
	}

	private IEnumerator FadeOut()
	{
		//start fading out
		fadingOut = true;
		//loop until the clipping planes match after a certain amount of time
		while (fadeCounter < fadeTime / 2f)
		{
			//increment counter
			fadeCounter += Time.deltaTime;
			//adjust camera's far clip plane
			cam.farClipPlane = Mathf.Lerp(cam.farClipPlane, nearFarClippingLimits.x * 1.01f, fadeCounter / (fadeTime / 2f));
			//wait until end of frame before continuing loop
			yield return 0;
		}
		//reset counter, change camera mode and start fading back in
		fadeCounter = 0;
		fadingIn = true;
		fadingOut = false;
		StartCoroutine(FadeIn());
		//check which mode to set to
		if (cam.orthographic)
		{
			if (target != null)
			{
				SetMode(CameraMode.LockedPerspective);
			}
			else
			{
				SetMode(CameraMode.FreePerspective);
			}
		}
		else
		{
			SetMode(CameraMode.Orthographic);
		}
	}

	private IEnumerator FadeIn()
	{
		//loop until the clipping planes match after a certain amount of time
		while (fadeCounter < fadeTime / 2f)
		{
			//increment counter
			fadeCounter += Time.deltaTime;
			//adjust camera's far clip plane
			cam.farClipPlane = Mathf.Lerp(cam.farClipPlane, nearFarClippingLimits.y, fadeCounter / (fadeTime / 2f));
			//wait until end of frame before continuing loop
			yield return 0;
		}
		//reset counter and start fading back in
		fadeCounter = 0;
		fadingIn = false;
	}
}
