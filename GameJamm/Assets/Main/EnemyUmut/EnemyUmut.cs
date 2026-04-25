using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyUmut : MonoBehaviour
{
    [System.Serializable]
    public class RoomData
    {
        [Tooltip("Bölümün konumu (transform.position)")]
        public Vector3 position;
        [Tooltip("Bu bölümde ne kadar bekleyeceği (patrolTime)")]
        public float patrolTime;
        [Tooltip("Bu bölümde devriye atılacak ekstra noktalar (isteğe bağlı)")]
        public Vector3[] patrolPoints;
    }

    [Header("Room & Patrol Settings")]
    public RoomData[] rooms;
    public int inRoom = 0;
    public bool isActivePatrol = false;
    
    [Header("Vision & Aggro Settings")]
    public LayerMask visionMask; // Oyuncu ve engelleri içermeli
    public Transform rayOrigin; // Göz veya merkez
    [HideInInspector] public bool isPlayerInAwarenessArea = false;

    [Header("Investigation Settings")]
    public float investigateWaitTime = 3f;
    public GameObject controlObjectPrefab; // Ray kesildiğinde çıkacak obje

    private NavMeshAgent agent;
    private PlayerController player;
    private GameObject currentControlObject;
    
    private int currentPatrolPointIndex = 0;
    private Vector3 lastPatrolPoint;

    public enum EnemyState {
        WaitingInRoom,
        MovingToRoom,
        PatrollingRoom,
        Chasing,
        Investigating
    }
    public EnemyState currentState = EnemyState.WaitingInRoom;

    private float roomTimer = 0f;
    private float investigateTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindAnyObjectByType<PlayerController>();
        
        if (rayOrigin == null) rayOrigin = transform;

        if (rooms.Length > 0)
        {
            lastPatrolPoint = rooms[0].position;
            currentState = EnemyState.MovingToRoom;
        }
    }

    void FixedUpdate()
    {
        CheckVision();
    }

    void Update()
    {
        CheckPlayerRoom();

        switch (currentState)
        {
            case EnemyState.MovingToRoom:
                HandleMovingToRoom();
                break;
            case EnemyState.WaitingInRoom:
                HandleWaitingInRoom();
                break;
            case EnemyState.PatrollingRoom:
                HandlePatrollingRoom();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Investigating:
                HandleInvestigating();
                break;
        }
    }

    private void CheckPlayerRoom()
    {
        if (player == null || rooms.Length == 0) return;

        // Sadece normal durumlarda oda kontrolü yap (Kovalarken veya araştırırken bölme)
        if (currentState == EnemyState.Chasing || currentState == EnemyState.Investigating)
            return;

        if (player.inRoom == inRoom)
        {
            isActivePatrol = true;
            if (currentState != EnemyState.PatrollingRoom)
            {
                currentState = EnemyState.PatrollingRoom;
                currentPatrolPointIndex = 0;
                
                // Eğer bölümün devriye noktaları varsa ilkine git, yoksa ana pozisyona git
                if (rooms[inRoom].patrolPoints != null && rooms[inRoom].patrolPoints.Length > 0)
                {
                    lastPatrolPoint = rooms[inRoom].patrolPoints[0];
                    agent.SetDestination(lastPatrolPoint);
                }
                else
                {
                    lastPatrolPoint = rooms[inRoom].position;
                    agent.SetDestination(lastPatrolPoint);
                }
            }
        }
        else
        {
            isActivePatrol = false;
            // Eğer devriyedeyken oyuncu odadan çıkarsa, normal bekleme/ilerleme rutinine dön
            if (currentState == EnemyState.PatrollingRoom)
            {
                currentState = EnemyState.MovingToRoom;
                agent.SetDestination(rooms[inRoom].position);
            }
        }
    }

    private void HandleMovingToRoom()
    {
        if (rooms.Length == 0) return;

        agent.SetDestination(rooms[inRoom].position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = EnemyState.WaitingInRoom;
            roomTimer = 0f;
        }
    }

    private void HandleWaitingInRoom()
    {
        if (rooms.Length == 0) return;

        // patrolTime kadar bekle
        roomTimer += Time.deltaTime;
        if (roomTimer >= rooms[inRoom].patrolTime)
        {
            // inRoom int değişkenini +1 arttır
            inRoom++;
            if (inRoom >= rooms.Length)
            {
                inRoom = 0; // Başa sar
            }
            
            currentState = EnemyState.MovingToRoom;
        }
    }

    private void HandlePatrollingRoom()
    {
        if (rooms.Length == 0 || !isActivePatrol) return;

        RoomData currentRoom = rooms[inRoom];
        
        // Bölüm içinde patrol noktaları varsa gez
        if (currentRoom.patrolPoints != null && currentRoom.patrolPoints.Length > 0)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentPatrolPointIndex = (currentPatrolPointIndex + 1) % currentRoom.patrolPoints.Length;
                lastPatrolPoint = currentRoom.patrolPoints[currentPatrolPointIndex];
                agent.SetDestination(lastPatrolPoint);
            }
        }
        else
        {
            // Nokta yoksa sadece odanın merkezinde dur (veya gezinme mantığını buraya ekleyebilirsiniz)
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // Ekstra nokta yoksa olduğu yerde kalır, ancak timer işlemeye devam edebilir
                // İsteğe bağlı olarak burada da timer işletebiliriz
            }
        }
    }

    private void CheckVision()
    {
        if (player == null) return;

        // Oyuncuya sürekli ray gönder
        Vector3 dirToPlayer = (player.transform.position - rayOrigin.position).normalized;
        bool canSeePlayerViaRay = false;

        // Sürekli ray gönder ve değip değmediğini kontrol et (sonsuz uzunlukta)
        if (Physics.Raycast(rayOrigin.position, dirToPlayer, out RaycastHit hit, Mathf.Infinity, visionMask))
        {
            if (hit.collider.gameObject == player.gameObject || hit.collider.GetComponentInParent<PlayerController>() != null)
            {
                canSeePlayerViaRay = true;
            }
        }

        if (canSeePlayerViaRay)
        {
            if (currentState != EnemyState.Chasing)
            {
                // Eğer kovalama durumunda değilsek, farkındalık alanında (trigger) olup olmadığına bak
                if (isPlayerInAwarenessArea)
                {
                    // Trigger'da, agroyu al
                    currentState = EnemyState.Chasing;
                    if (currentControlObject != null)
                    {
                        Destroy(currentControlObject);
                    }
                }
            }
            // Zaten agrolu ise ray kesilene kadar kovalama devam eder
        }
        else if (currentState == EnemyState.Chasing)
        {
            // Ray kesildiğinde rayin kesildiği yere (oyuncunun son konumuna) kontrol objesi çağır
            SpawnControlObject(player.transform.position);
            currentState = EnemyState.Investigating;
            investigateTimer = 0f;
        }
    }

    private void HandleChasing()
    {
        if (player != null)
        {
            agent.SetDestination(player.transform.position);
            lastPatrolPoint = player.transform.position; // Güvenlik amaçlı
        }
    }

    private void HandleInvestigating()
    {
        if (currentControlObject != null)
        {
            agent.SetDestination(currentControlObject.transform.position);

            // Kontrol objesine ulaştığında
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // 3 saniye bekle
                investigateTimer += Time.deltaTime;
                if (investigateTimer >= investigateWaitTime)
                {
                    // 3 saniye içinde ray oyuncuya değmedi (değseydi CheckVision'dan Chasing'e geçerdi)
                    Destroy(currentControlObject);
                    
                    // En son takip ettiği takip noktasına navmeshle gider
                    if (lastPatrolPoint != null)
                    {
                        agent.SetDestination(lastPatrolPoint);
                    }
                    
                    // Patrole veya bekleme durumuna geri dön
                    currentState = isActivePatrol ? EnemyState.PatrollingRoom : EnemyState.MovingToRoom;
                }
            }
        }
        else
        {
            currentState = isActivePatrol ? EnemyState.PatrollingRoom : EnemyState.MovingToRoom;
        }
    }

    private void SpawnControlObject(Vector3 position)
    {
        if (currentControlObject != null)
        {
            Destroy(currentControlObject);
        }

        if (controlObjectPrefab != null)
        {
            currentControlObject = Instantiate(controlObjectPrefab, position, Quaternion.identity);
        }
        else
        {
            currentControlObject = new GameObject("ControlObject");
            currentControlObject.transform.position = position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (player != null && (other.gameObject == player.gameObject || other.GetComponentInParent<PlayerController>() != null))
        {
            isPlayerInAwarenessArea = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (player != null && (other.gameObject == player.gameObject || other.GetComponentInParent<PlayerController>() != null))
        {
            isPlayerInAwarenessArea = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null) rayOrigin = transform;

        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayOrigin.position, player.transform.position);
        }
    }
}
