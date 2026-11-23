using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TacticalBotAI : MonoBehaviour
{
    public enum BotState { Patrol, Combat, InvestigateLKP }

    [Header("References")]
    public Transform playerTarget;
    public Transform eyePoint;
    public LayerMask obstructionMask;

    [Header("Health & Combat")]
    public float maxHealth = 100f;
    public float detectionRange = 35f;
    public float fieldOfView = 110f;
    public float reactionTime = 0.22f;
    public float fireRate = 0.14f;
    public float botAccuracySpread = 0.045f;
    public float damagePerBullet = 22f;

    [Header("Smooth Movement & Tactics")]
    public float patrolRadius = 15f;
    public float moveSpeed = 4.8f;
    public float strafeDuration = 0.65f; // How long bot strafes smoothly
    public float burstDuration = 0.45f;  // How long bot stops to fire an accurate burst
    public float memoryDuration = 4.5f;

    private NavMeshAgent agent;
    private BotState currentState = BotState.Patrol;
    private float currentHealth;
    private Vector3 lastKnownPlayerPos;
    private float timeSinceSawPlayer = 999f;
    private float reactionTimer = 0f;
    private float nextFireTime = 0f;
    private float combatCycleTimer = 0f;
    private bool isStrafing = true;
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = 28f; // Snappy CS-style acceleration
        agent.angularSpeed = 0f;  // We handle rotation smoothly ourselves
        currentHealth = maxHealth;

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }
        if (eyePoint == null) eyePoint = transform;

        PickNewPatrolPoint();
    }

    void Update()
    {
        if (isDead || playerTarget == null) return;

        bool canSeePlayer = CheckLineOfSight();

        if (canSeePlayer)
        {
            lastKnownPlayerPos = playerTarget.position;
            timeSinceSawPlayer = 0f;
            if (currentState != BotState.Combat)
            {
                currentState = BotState.Combat;
                combatCycleTimer = 0f;
                isStrafing = false; // Counter-strafe immediately on first sight!
            }
        }
        else
        {
            timeSinceSawPlayer += Time.deltaTime;
            reactionTimer = 0f;

            if (currentState == BotState.Combat)
            {
                currentState = BotState.InvestigateLKP;
                agent.isStopped = false;
                agent.SetDestination(lastKnownPlayerPos);
            }
            else if (currentState == BotState.InvestigateLKP && timeSinceSawPlayer > memoryDuration)
            {
                currentState = BotState.Patrol;
                PickNewPatrolPoint();
            }
        }

        switch (currentState)
        {
            case BotState.Patrol:
                HandlePatrol();
                break;
            case BotState.Combat:
                HandleCombat();
                break;
            case BotState.InvestigateLKP:
                HandleInvestigate();
                break;
        }
    }

    private bool CheckLineOfSight()
    {
        Vector3 dirToPlayer = (playerTarget.position + Vector3.up * 0.5f) - eyePoint.position;
        float dist = dirToPlayer.magnitude;

        if (dist > detectionRange) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer) > fieldOfView * 0.5f && timeSinceSawPlayer > 2f)
            return false;

        if (Physics.Raycast(eyePoint.position, dirToPlayer.normalized, out RaycastHit hit, dist, obstructionMask))
        {
            if (hit.transform.root == playerTarget.root) return true;
        }
        return false;
    }

    private void HandlePatrol()
    {
        agent.isStopped = false;
        SmoothFaceDirection(agent.desiredVelocity);

        if (!agent.pathPending && agent.remainingDistance < 1.2f)
        {
            PickNewPatrolPoint();
        }
    }

    private void HandleCombat()
    {
        // Always smoothly face the player during combat
        SmoothFaceDirection(playerTarget.position - transform.position);

        reactionTimer += Time.deltaTime;
        if (reactionTimer < reactionTime) return;

        // Alternate smoothly between strafing sideways and stopping to fire a burst
        combatCycleTimer += Time.deltaTime;

        if (isStrafing)
        {
            agent.isStopped = false;
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                Vector3 strafeDir = transform.right * (Random.value > 0.5f ? 3.5f : -3.5f);
                if (NavMesh.SamplePosition(transform.position + strafeDir, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                }
            }

            if (combatCycleTimer >= strafeDuration)
            {
                isStrafing = false;
                combatCycleTimer = 0f;
                agent.isStopped = true;
                agent.velocity = Vector3.zero; // Clean CS counter-strafe stop!
            }
        }
        else
        {
            // Stopped: fire an accurate burst
            agent.isStopped = true;
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                FireAtPlayer();
            }

            if (combatCycleTimer >= burstDuration)
            {
                isStrafing = true;
                combatCycleTimer = 0f;
            }
        }
    }

    private void HandleInvestigate()
    {
        agent.isStopped = false;
        SmoothFaceDirection(lastKnownPlayerPos - transform.position);
    }

    private void SmoothFaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
        }
    }

    private void FireAtPlayer()
    {
        Vector3 aimTarget = playerTarget.position + Vector3.up * 0.4f;
        Vector3 shootDir = (aimTarget - eyePoint.position).normalized;

        shootDir += transform.right * Random.Range(-botAccuracySpread, botAccuracySpread);
        shootDir += transform.up * Random.Range(-botAccuracySpread, botAccuracySpread);
        shootDir.Normalize();

        Debug.DrawRay(eyePoint.position, shootDir * 25f, Color.red, 0.2f);

        if (Physics.Raycast(eyePoint.position, shootDir, out RaycastHit hit, detectionRange))
        {
            if (hit.transform.root == playerTarget.root)
            {
                Debug.Log("Bot hit Player for " + damagePerBullet + " dmg!");

                PlayerHealth pHealth = hit.transform.GetComponentInParent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damagePerBullet);
                }
            }
        }
    }

    private void PickNewPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void TakeDamage(float amount, bool isHeadshot)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (playerTarget != null)
        {
            lastKnownPlayerPos = playerTarget.position;
            timeSinceSawPlayer = 0f;
            currentState = BotState.Combat;
        }

        if (currentHealth <= 0f)
        {
            isDead = true;

            if (CSKillCard.Instance != null)
            {
                CSKillCard.Instance.TriggerKill();
            }

            agent.enabled = false; // Release NavMesh lock so it falls cleanly
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false; // Prevent dead body from blocking shots or player
            }
            StartCoroutine(SmoothDeathFall(isHeadshot));
        }
    }

    private IEnumerator SmoothDeathFall(bool isHeadshot)
    {
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(isHeadshot ? -85f : 85f, 0f, 0f);
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 0.45f;

        float elapsed = 0f;
        float duration = 0.28f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease-out curve so it drops fast and settles smoothly
            t = 1f - Mathf.Pow(1f - t, 3f);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }
}