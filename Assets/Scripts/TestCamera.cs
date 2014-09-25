using UnityEngine;
using System.Collections;

public class TestCamera : MonoBehaviour
{
	private bool mIsFollowing = false;
	
	// Follow target
	private Transform mTarget;
	private float mDistance = 30.0f;
	private float mHeight = 800.0f;
	private float mHeightDamping = 2.0f;
	private float mRotationDamping = 3.0f;
	
	// Gesture
	// one finger
	private Vector2 mLastPoint;
	// two fingers
	private float mPreviousDistanceBetweenFingers = 0;
	private float zoomNearLimit = 100;
	private float zoomFarLimit = 4500;
	private float zoomScreenToWorldRatio = 25.0f;

	// don't change these
	private float distWeight;
	private float zoomDistance;
	private float zoomSpeed = 0;
#if UNITY_EDITOR
	private float moveSpeed = 0.1f;
#else
	private float moveSpeed = 0.4f;
#endif
	
	// Use this for initialization
	void Start ()
	{
	
	}
	
	void Awake()
	{
		Application.targetFrameRate = 60;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if ( !mIsFollowing )
		{
#if UNITY_EDITOR
        	if ( Input.GetMouseButtonDown(0) )
			{
				mLastPoint = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
			}
			else if ( Input.GetMouseButton( 0 ) )
			{
				Vector2 deltaV2 = new Vector2( Input.mousePosition.x, Input.mousePosition.y ) - mLastPoint;
				Vector3 deltaV3 = new Vector3( deltaV2.y, 0, -deltaV2.x );
				transform.position += deltaV3 * moveSpeed;
			}
			else if ( Input.GetMouseButtonUp( 0 ) )
			{
				mLastPoint = Vector2.zero;
			}
			else if ( Input.GetAxis( "Mouse ScrollWheel" ) != 0 )
			{
				float wheelDelta = Input.GetAxis( "Mouse ScrollWheel" );
				// zoom in 
				if ( wheelDelta > 0 && zoomDistance > zoomNearLimit )
				{
					zoomSpeed = ( wheelDelta * 6.0f ) * Time.deltaTime * zoomScreenToWorldRatio;
					transform.position += transform.forward * zoomSpeed;
					zoomDistance = transform.position.y;
				}
				// zoom out
				else if ( wheelDelta < 0 && zoomDistance < zoomFarLimit )
				{
					zoomSpeed = ( wheelDelta * 6.0f ) * Time.deltaTime * zoomScreenToWorldRatio;
					transform.position += transform.forward * zoomSpeed;
					zoomDistance = transform.position.y;
				}
			}
#else
			if ( Input.touchCount == 1 )
			{
				mPreviousDistanceBetweenFingers = 0;
				distWeight = 0;
				zoomSpeed = 0;
				Touch touch1 = Input.GetTouch( 0 );
				
				if ( touch1.phase == TouchPhase.Moved )
				{
					if ( mLastPoint == Vector2.zero )
					{
                        Debug.Log( "touch phase is moved, but last point is zero..." );
					}
					
					Vector2 deltaV2 = touch1.position - mLastPoint;
					Vector3 deltaV3 = new Vector3( deltaV2.y, 0, -deltaV2.x );
					transform.position += deltaV3 * moveSpeed;
				}
				mLastPoint = touch1.position;
			}
			else if ( Input.touchCount == 2 )
			{
				mLastPoint = Vector2.zero;
				Touch touch1 = Input.GetTouch( 0 );
				Touch touch2 = Input.GetTouch( 1 );
				
				if ( touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved )
				{
					Vector2 touchDirection1 = touch1.deltaPosition.normalized;
					Vector2 touchDirection2 = touch2.deltaPosition.normalized;
					float dot = Vector2.Dot( touchDirection1, touchDirection2 );
					float fingerDistance = Vector2.Distance( touch1.position, touch2.position );
					
					if ( dot < -0.7f && mPreviousDistanceBetweenFingers != 0 )
					{
						float pinchDelta = fingerDistance - mPreviousDistanceBetweenFingers;
						if ( Mathf.Abs( pinchDelta ) > 2 )
						{
							// if pinch out, zoom in 
							if ( fingerDistance > mPreviousDistanceBetweenFingers && zoomDistance > zoomNearLimit )
							{
								zoomSpeed += ( pinchDelta + pinchDelta * 0.25f ) * Time.deltaTime * zoomScreenToWorldRatio;
								transform.position += transform.forward * zoomSpeed;
								zoomDistance = transform.position.y;
							}
							// if pinch in, zoom out
							else if ( fingerDistance < mPreviousDistanceBetweenFingers && zoomDistance < zoomFarLimit )
							{
								zoomSpeed += ( pinchDelta + pinchDelta * 0.25f ) * Time.deltaTime * zoomScreenToWorldRatio;
								transform.position += transform.forward * zoomSpeed;
								zoomDistance = transform.position.y;
							}
							
						}
					}
					
					// record last distance, for delta distances
					mPreviousDistanceBetweenFingers = fingerDistance;
					
					// compensate for distance (ej. orbit slower when zoomed in; faster when out)
					distWeight = (zoomDistance - zoomNearLimit) / (zoomFarLimit - zoomNearLimit);
					distWeight = Mathf.Clamp01(distWeight);
				}
				else
				{
					mPreviousDistanceBetweenFingers = 0;
					distWeight = 0;
					zoomSpeed = 0;
				}
				
			}
#endif
		}
		
		
	}
	
	void OnGUI()
	{
		if ( GUI.Button( new Rect( 10, 10, 100, 50 ), "zoom out" ) )
		{
			camera.transform.position = new Vector3( camera.transform.position.x, camera.transform.position.y + 50, camera.transform.position.z );
		}
		
		if ( GUI.Button( new Rect( 10, 70, 100, 50 ), "zoom in" ) )
		{
			camera.transform.position = new Vector3( camera.transform.position.x, camera.transform.position.y - 50, camera.transform.position.z );
		}
	}
	
	void LateUpdate()
	{
		if ( mIsFollowing )
		{
			// Early out if we don't have a target
			if ( !mTarget )
				return;
			
			// Calculate the current rotation angles
			float wantedRotationAngle = mTarget.eulerAngles.y;
			float wantedHeight = mTarget.position.y + mHeight;
				
			float currentRotationAngle = transform.eulerAngles.y;
			float currentHeight = transform.position.y;
			
			// Damp the rotation around the y-axis
			currentRotationAngle = Mathf.LerpAngle( currentRotationAngle, wantedRotationAngle, mRotationDamping * Time.deltaTime );
		
			// Damp the height
			currentHeight = Mathf.Lerp( currentHeight, wantedHeight, mHeightDamping * Time.deltaTime );
		
			// Convert the angle into a rotation
			Quaternion currentRotation = Quaternion.Euler( 0, currentRotationAngle, 0 );
			
			// Set the position of the camera on the x-z plane to:
			// distance meters behind the target
			transform.position = mTarget.position;
			transform.position -= currentRotation * Vector3.forward * mDistance;
		
			// Set the height of the camera
			transform.position = new Vector3( transform.position.x, currentHeight, transform.position.z );
			
			// Always look at the target
			transform.LookAt( mTarget );
		}
	}
}
