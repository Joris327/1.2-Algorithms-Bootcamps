using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 3f;
    public LayerMask groundLayer; // Layer for ground detection
    private Rigidbody rb;

    private bool isGrounded;

    public GameObject grenadePrefab;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

        Move();
        Jump();
        CheckForGrenades();
        UpdateRotation();
        
        CheckGroundStatus();
    }

    void CheckForGrenades()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            GameObject grenade = Instantiate(grenadePrefab, transform.position + (transform.forward+transform.up)*0.5f, Quaternion.identity);
            grenade.GetComponent<Grenade>().SetThrowDirection(transform.forward+transform.up);
        }
    }

    private void UpdateRotation()
    {
        Vector3 direction = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if(direction.magnitude > 0.1f)
        {
            Quaternion newRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, Time.deltaTime * 10); 
        }
    }

    private void Move()
    {
        float moveInputHorizontal = Input.GetAxis("Horizontal");
        float moveInputVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveInputHorizontal, 0, moveInputVertical) * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
        
        //Draw an arrow to visualize the movement vector
        
        //ADD CODE HERE
        DebugExtension.DebugArrow(transform.position, rb.linearVelocity);
    }

    private void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    private void CheckGroundStatus()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, .6f, groundLayer);
        
        //Draw a line to visualize the raycast
        
        //ADD CODE HERE
        Debug.DrawLine(transform.position, transform.position + (Vector3.down * .6f), Color.red);
    }
    
}
