using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))] // Automatycznie dodaje głośnik do mrówki
public class AntMove : MonoBehaviour
{
    [Header("Cel (Obiekt)")]
    public Transform targetObject;

    [Header("Dźwięki")]
    [Tooltip("Przeciągnij tutaj plik audio, który ma zagrać przy jedzeniu.")]
    public AudioClip eatingSound;

    [Header("Referencje")]
    public Transform player;

    [Header("Ustawienia strachu i ucieczki")]
    public float scareDistance = 3f;
    public float fleeRadius = 4f;
    public float fleeDuration = 5f;

    private NavMeshAgent agent;
    private AudioSource audioSource;
    private bool isFleeing = false;
    private float fleeTimer = 0f;

    // Zabezpieczenie, żeby dźwięk zagrał tylko raz przy dotarciu, a nie co klatkę
    private bool isEating = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        agent.radius = 0.05f;
        agent.height = 0.1f;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (targetObject != null)
        {
            GoToTarget();
        }
    }

    void Update()
    {
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            isEating = false; // Jak ucieka, to nie je

            if (fleeTimer <= 0 || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
            {
                isFleeing = false;
                GoToTarget();
            }
        }
        else
        {
            if (player != null && Vector3.Distance(transform.position, player.position) < scareDistance)
            {
                StartFleeing();
            }
            else if (targetObject != null)
            {
                // Jeśli cel się poruszył, idź za nim
                if (Vector3.Distance(agent.destination, targetObject.position) > 0.5f)
                {
                    GoToTarget();
                    isEating = false; // Przerywamy jedzenie, bo mrówka znowu idzie
                }

                // Sprawdzamy, czy mrówka stoi przy celu
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    // Jeśli jeszcze nie zaczęła jeść, to zagraj dźwięk
                    if (!isEating)
                    {
                        isEating = true;

                        if (eatingSound != null)
                        {
                            // PlayOneShot nakłada na siebie dźwięki i gra je do końca
                            audioSource.PlayOneShot(eatingSound);
                        }
                    }
                }
                else
                {
                    isEating = false; // Jeśli z jakiegoś powodu jest w drodze, resetuj flagę
                }
            }
        }
    }

    void GoToTarget()
    {
        if (targetObject != null)
        {
            agent.SetDestination(targetObject.position);
        }
    }

    void StartFleeing()
    {
        isFleeing = true;
        fleeTimer = fleeDuration;

        Vector3 fleeDirection = (transform.position - player.position).normalized;
        Vector3 targetFleePosition = transform.position + fleeDirection * fleeRadius;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetFleePosition, out hit, fleeRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, scareDistance);

        if (targetObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetObject.position);
        }
    }
}