using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private float moveInput;
    public GameObject groundRayObject;
    public float jumpForce;
    public bool jumpOn;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
       
        moveInput = Input.GetAxisRaw("Horizontal");

        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (jumpOn)
                rb.linearVelocity = Vector2.up * jumpForce;
            else
                return;
        }
    }

    void FixedUpdate()
    {
       
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        float rayLength = 3f; 
        int rayCount = 5;     
        float coneAngle = 45f; 

       
        Vector2 centerDir = -groundRayObject.transform.up;

       
        bool hitSomething = false;

        for (int i = 0; i < rayCount; i++)
        {
           
            float angleOffset = Mathf.Lerp(-coneAngle / 2, coneAngle / 2, (float)i / (rayCount - 1));

            
            Vector2 dir = Quaternion.Euler(0, 0, angleOffset) * centerDir;

            
            RaycastHit2D hit = Physics2D.Raycast(groundRayObject.transform.position, dir, rayLength);

           
            Debug.DrawRay(groundRayObject.transform.position, dir * rayLength, Color.red);

            if (hit.collider != null)
            {
                hitSomething = true;
            }
        }

        jumpOn = hitSomething;
    }


}
