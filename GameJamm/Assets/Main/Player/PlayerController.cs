using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları (Movement)")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 5f;

    [Header("Stamina Ayarları")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 10f;
    [SerializeField] private float currentStamina;

    [Header("Kamera & Bakış Ayarları (Look)")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.2f;
    public float maxLookAngle = 85f;
    private float xRotation = 0f;
    private float yRotation = 0f;

    [Header("Eğilme Ayarları (Crouch)")]
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    private float originalHeight;
    private Vector3 originalCenter;

    [Header("Zemin Kontrolü (Ground Check)")]
    [Tooltip("Zemin olarak kabul edilecek layer'ları seçin. Player layer'ını dahil etmeyin!")]
    public LayerMask groundMask = ~0; // Default: Everything
    public float groundCheckDistance = 0.15f;
    private bool isGrounded;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Input Actions (Input System)
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private bool isSprinting;
    private bool isCrouching;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Rigidbody'nin devrilmesini engelle
        rb.freezeRotation = true;

        originalHeight = capsuleCollider.height;
        originalCenter = capsuleCollider.center;

        currentStamina = maxStamina;

        // Eğer kamera atanmamışsa alt objelerden bul
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }

        // Mouse imlecini kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yRotation = transform.eulerAngles.y;

        SetupInputs();
    }

    private void SetupInputs()
    {
        // Yürüme (WASD)
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Etrafa Bakma (Mouse)
        lookAction = new InputAction("Look", InputActionType.Value, "<Pointer>/delta");

        // Zıplama (Space)
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.performed += ctx => Jump();

        // Koşma (Shift)
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        sprintAction.performed += ctx => isSprinting = true;
        sprintAction.canceled += ctx => isSprinting = false;

        // Eğilme (C) - Toggle (Aç/Kapa) mantığıyla
        crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/c");
        crouchAction.performed += ctx => ToggleCrouch();
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        crouchAction.Disable();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        Move();
        HandleStamina();
    }

    void LateUpdate()
    {
        Look();
        HandleCameraCrouch();
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting && currentStamina > 0 && input.y > 0) // Sadece ileri giderken koş
        {
            currentSpeed = sprintSpeed;
        }

        Vector3 targetVelocity = moveDirection * currentSpeed;
        
        // Mevcut Y hızını (yerçekimi ve zıplama) koru
        // Unity 6000'de Rigidbody.velocity yerine Rigidbody.linearVelocity kullanılır.
        targetVelocity.y = rb.linearVelocity.y; 
        
        rb.linearVelocity = targetVelocity;
    }

    private void Look()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        yRotation += mouseX;

        if (cameraTransform != null)
        {
            // Kamerayı yukarı/aşağı döndür
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        
        // Karakteri sağa/sola döndür
        // Fizik motoru ile çakışmayı (jitter/lag) önlemek için hem transform hem de rb rotasyonunu aynı anda güncelliyoruz.
        Quaternion targetRotation = Quaternion.Euler(0f, yRotation, 0f);
        transform.rotation = targetRotation;
        rb.rotation = targetRotation;
    }

    private void Jump()
    {
        if (isCrouching)
        {
            ToggleCrouch();
            return;
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    private void ToggleCrouch()
    {
        if (!isCrouching)
        {
            // Eğil
            isCrouching = true;
            SetColliderHeight(crouchHeight);
        }
        else
        {
            // Ayağa kalkmaya çalış (Üstte engel var mı kontrol et)
            if (CanStandUp())
            {
                isCrouching = false;
                SetColliderHeight(originalHeight);
            }
        }
    }

    private void SetColliderHeight(float newHeight)
    {
        capsuleCollider.height = newHeight;
        float bottomY = originalCenter.y - originalHeight / 2f;
        capsuleCollider.center = new Vector3(originalCenter.x, bottomY + newHeight / 2f, originalCenter.z);
    }

    private void HandleCameraCrouch()
    {
        if (cameraTransform != null)
        {
            float targetHeight = isCrouching ? (crouchHeight - 0.2f) : (originalHeight - 0.2f);
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            cameraTransform.localPosition = camPos;
        }
    }

    private bool CanStandUp()
    {
        float bottomY = originalCenter.y - originalHeight / 2f;
        Vector3 bottomPos = transform.TransformPoint(new Vector3(originalCenter.x, bottomY, originalCenter.z));
        
        float radius = capsuleCollider.radius * 0.9f;
        Vector3 origin = bottomPos + Vector3.up * radius;
        float maxDistance = originalHeight - (radius * 2);

        // Yukarı doğru SphereCast atarak tavan kontrolü yap
        return !Physics.SphereCast(origin, radius, Vector3.up, out _, maxDistance, groundMask);
    }

    private void HandleStamina()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isMovingForward = input.y > 0;

        if (isSprinting && isMovingForward && !isCrouching && isGrounded)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina < 0)
            {
                currentStamina = 0;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
        }
    }

    private void CheckGrounded()
    {
        float radius = capsuleCollider.radius * 0.9f;
        float bottomY = capsuleCollider.center.y - capsuleCollider.height / 2f;
        Vector3 origin = transform.TransformPoint(new Vector3(capsuleCollider.center.x, bottomY + radius + 0.05f, capsuleCollider.center.z));

        isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out _, groundCheckDistance, groundMask);
    }

    // Gerekirse UI vb. yerlerden erişim için
    public float CurrentStamina => currentStamina;
    public float StaminaPercentage => currentStamina / maxStamina;
}
