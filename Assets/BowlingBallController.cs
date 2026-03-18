using UnityEngine;
using UnityEngine.UI;
using System.IO.Ports;

public class BowlingController : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "COM3";
    public int baudRate = 115200;
    
    [Header("Bowling Settings")]
    public float forwardSpeed = 15f; // Added forward speed
    public float horizontalRange = 3f;
    public Transform startPoint;
    public Slider directionSlider;
    public Camera mainCamera;
    public bool followSliderWhileMoving = true; // Added slider following bool
    public float sliderFollowSpeed = 8f; // Added follow speed
    
    [Header("Smoothing")]
    [Range(1, 20)] public float responseSpeed = 12f;
    
    private SerialPort serial;
    private float targetPos;
    private float currentPos;
    private Rigidbody rb;
    private bool isLaunched;
    private Vector3 initialDirection; // Added for forward movement

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        InitializeSerial();
    }

    void InitializeSerial()
    {
        try {
            serial = new SerialPort(portName, baudRate) {
                ReadTimeout = 10
            };
            serial.Open();
            serial.DiscardInBuffer();
        } catch {
            Debug.Log("Using keyboard controls (A/D to move, Space to launch)");
        }
    }

    void Update()
    {
        ReadSerialData();
        
        // Smooth the movement
        currentPos = Mathf.Lerp(currentPos, targetPos, responseSpeed * Time.deltaTime);
        directionSlider.value = currentPos;
        
        if (!isLaunched)
        {
            // Pre-launch positioning
            float zPos = Mathf.Lerp(-horizontalRange, horizontalRange, currentPos);
            transform.position = new Vector3(
                startPoint.position.x, 
                startPoint.position.y, 
                zPos
            );
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                LaunchBall();
            }
        }
        else if (followSliderWhileMoving)
        {
            // In-flight slider following
            float targetZ = Mathf.Lerp(-horizontalRange, horizontalRange, currentPos);
            float smoothZ = Mathf.Lerp(transform.position.z, targetZ, Time.deltaTime * sliderFollowSpeed);
            rb.velocity = new Vector3(
                initialDirection.x, 
                0f, 
                (smoothZ - transform.position.z) * 10f
            );
        }
        
        // Keyboard fallback
        if (serial == null || !serial.IsOpen) {
            if (Input.GetKey(KeyCode.A)) targetPos = Mathf.Clamp01(targetPos - 0.02f);
            if (Input.GetKey(KeyCode.D)) targetPos = Mathf.Clamp01(targetPos + 0.02f);
            if (Input.GetKeyDown(KeyCode.Space) && !isLaunched) LaunchBall();
        }
    }

    void LaunchBall()
    {
        isLaunched = true;
        initialDirection = transform.right * forwardSpeed;
        rb.velocity = initialDirection;
    }

    void ReadSerialData()
    {
        if (serial != null && serial.IsOpen) {
            try {
                string data = serial.ReadLine();
                if (float.TryParse(data, out float newValue)) {
                    targetPos = newValue;
                }
            } catch {
                serial.DiscardInBuffer();
            }
        }
    }
    void OnDestroy() {
        if (serial != null && serial.IsOpen) {
            serial.Close();
        }
    }


    // ... rest of your collision and reset methods ...

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pin"))
        {
            Debug.Log("Hit pin!");
            serial.WriteLine("score\n"); // Nyalakan LED
            mainCamera.backgroundColor = Color.red;
            Invoke(nameof(ResetBackground), 0.1f);

            Rigidbody pinRb = collision.gameObject.GetComponent<Rigidbody>();
            if (pinRb != null)
            {
                Vector3 forceDir = (collision.transform.position - transform.position).normalized;
                forceDir.y = 1f;
                pinRb.AddForce(forceDir * 8f, ForceMode.Impulse);
                pinRb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }
    }

    private void ResetBackground()
    {
        mainCamera.backgroundColor = Color.black;
    }

    public void ResetBall()
    {
        isLaunched = false;
        transform.position = startPoint.position;
    }
}