using UnityEngine;
using System.Collections;

public class RobotControl : MonoBehaviour 
{
	public float maxSpeed = 10;
	bool facingRight = true;
	
	public bool allowAirControl = true;
	bool grounded = false;
	public Transform groundCheck;
	float groundRadius = 0.2f;
	
	public LayerMask whatIsGround;
	public float accelerationSpeed;
	public float jumpForce;
	public float maximumXJumpSpeed;
	
	// Debug Rendering
	public bool debug = false;
	public GameObject debugPrefab;
	private GameObject debugAnim;

	private Vector3 debugOffset;
	
	bool hasMoved = false;
	
	private Vector2 touchOrigin = -Vector2.one;
	
	Animator anim;

	// Use this for initialization
	void Start () 
	{
		anim = GetComponent<Animator>();

		if(debug)
		{
			debugOffset = new Vector3(8,3,0);
			debugAnim = (GameObject)UnityEngine.Object.Instantiate(debugPrefab, GetComponent<Transform>().position, Quaternion.identity);
		}
	}
	
	void checkOnGround()
	{
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, whatIsGround);
		anim.SetBool("Ground", grounded);
		anim.SetFloat("vSpeed", GetComponent<Rigidbody2D>().velocity.y);
	}
	
	
	bool isJumpActivated()
	{	
		#if UNITY_STANDALONE || UNITY_WEBPLAYER
		 return Input.GetAxis("Jump") > 0;
		 
		#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
		 
		//Check if Input has registered more than zero touches
        if (Input.touchCount > 0)
        {
            //Store the first touch detected.
            Touch myTouch = Input.touches[0];

            if(myTouch.phase == TouchPhase.Moved )
			{
				hasMoved = true;
            }
		    else if (myTouch.phase == TouchPhase.Ended)
            {
				return !hasMoved;
			}
			  
        }
            
		return false;	
        #endif //End of mobile platform dependendent compilation section started above with #elif)
	}
	
	bool canJump()
	{
		return grounded && isJumpActivated();
	}
	
	void Update()
	{
		if(canJump())
		{
			anim.SetBool("Ground", false);
			GetComponent<Rigidbody2D>().AddForce(new Vector2(0, jumpForce));
		}
		
		if(debug)
		{
			debugAnim.GetComponent<Animator>().SetBool("Jump", isJumpActivated());
			debugAnim.GetComponent<Animator>().SetFloat("XMove", getXMove());
			debugAnim.GetComponent<Animator>().SetFloat("YMove", getYMove());
		}
	}
	
	float getXMove()
	{
		float move = 0;
		 #if UNITY_STANDALONE || UNITY_WEBPLAYER
		 move = Input.GetAxis("Horizontal");
		 
		 #elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
		 
		//Check if Input has registered more than zero touches
            if (Input.touchCount > 0)
            {
                //Store the first touch detected.
                Touch myTouch = Input.touches[0];
                
                //Check if the phase of that touch equals Began
                if (myTouch.phase == TouchPhase.Began)
                {
					hasMoved = false;
                }
				
				else if(myTouch.phase == TouchPhase.Moved || myTouch.phase == TouchPhase.Stationary)
				{
					if(myTouch.phase == TouchPhase.Moved)
						hasMoved = true;

					float RobotX= transform.position.x;
					
					Debug.Log("Robot :" + RobotX);
					Debug.Log("Input :" + myTouch.position.x);
					
					move = myTouch.position.x < RobotX ? -1 : 1;
				}
            }

        #endif //End of mobile platform dependendent compilation section started above with #elif
			
		return move;
	}
	
	float getYMove()
	{
		float move = 0;
		 #if UNITY_STANDALONE || UNITY_WEBPLAYER
		 move = Input.GetAxis("Vertical");
		 
		 #elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
		 
		//Check if Input has registered more than zero touches
            if (Input.touchCount > 0)
            {
                //Store the first touch detected.
               Touch myTouch = Input.touches[0];

               if(myTouch.phase == TouchPhase.Moved )
			   {
					hasMoved = true;
               }
			  else if (myTouch.phase == TouchPhase.Moved || myTouch.phase == TouchPhase.Stationary)
              {
				float RobotY= transform.position.y;
					
				move = myTouch.position.y < RobotY ? -1 : 1;
			  }
			  
            }
            
            #endif //End of mobile platform dependendent compilation section started above with #elif
			
		return move;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		checkOnGround();
		
		float move = getXMove();
		float moveY = getYMove();
		
		anim.SetFloat("speed", Mathf.Abs(move));
		anim.SetBool("crouch", moveY < -0.5);
		
		if(debug)
		{
			debugAnim.GetComponent<Animator>().SetBool("Crouch", moveY < -0.5);
			debugAnim.GetComponent<Animator>().SetFloat("Speed", Mathf.Abs(move));
		}
		
		if(grounded)
		{
			GetComponent<Rigidbody2D>().velocity = new Vector2(move * maxSpeed, GetComponent<Rigidbody2D>().velocity.y);
		}
		else if(allowAirControl)
		{
			float offset = move * accelerationSpeed;
			float moveX = GetComponent<Rigidbody2D>().velocity.x + offset;
			if(moveX > 0)
			{
				moveX = Mathf.Min(moveX, maximumXJumpSpeed);
			}
			else
			{
				moveX = Mathf.Max(moveX, -maximumXJumpSpeed);
			}
			GetComponent<Rigidbody2D>().velocity = 
			new Vector2(moveX, GetComponent<Rigidbody2D>().velocity.y);
		}

		if(debug)
		{
			debugAnim.GetComponent<Transform>().position = GetComponent<Transform>().position + debugOffset;
		}
		
		anim.SetFloat("xSpeed", Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x));

		if(move > 0 && !facingRight)
			Flip();
		else if (move < 0 && facingRight)
			Flip();
	}
	
	void Flip()
	{
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
