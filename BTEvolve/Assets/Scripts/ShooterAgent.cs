using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ShooterAgent : MonoBehaviour {

    // Private variables
    // Settings
    private Collider m_collider;
    private GameObject m_bulletPrefab;
    private StaticGlobals m_globals;
    private float m_bulletTimer = 0.0f;
    private float m_reloadTimer = 0.0f;
    private int m_maxHealth = 100;
    private int m_health = 100;
    private int m_totalDamageTaken = 0;
    private int m_bulletMax = 10;
    private int m_bulletAmount = 10;

    // AI related privates
    private NavMeshAgent m_navAgent;
    private AgentState m_myState = AgentState.patroling;
    private Vector3 m_targetEnemy = Vector3.zero;
    private Vector3 m_walkingDestinaton = Vector3.zero;
    private GameObject m_targetHealthPack = null;
    private bool m_healthPackFound = false;
    private bool m_reloading = false;
    private bool m_tookDamage = false;
    private bool m_enemyLost = true;

    // Public adjustable variables
    [Header("Turns on and off certain debugging variables.")]
    public bool destinationLogs = true;
    public bool invincible = false;
    [Header("Adjustable parameters for the agents behaviour.")]
    public float rotationSpeed = 1.0f;
    public float shootingCooldown = 0.2f;
    public float reloadTime = 2.0f;
    public float bulletForce = 10.0f;
    public float visionRange = 20.0f;
    public bool destroyOnDeath = true;
    public Transform testDestination;
    public LayerMask obstructionMask;
    [Tooltip("Buttons for debugging purposes.")]
    public bool damageAgent = false;
    public bool registerShot = false;

    // Crossover communication with events
    private MatchSimulator m_simulator;

    public enum AgentState
    {
        patroling, kiting, dead
    }

    #region Misc initialization
    public void ResetValues()
    {
        // Reset AI values.
        m_health = m_maxHealth;
        m_totalDamageTaken = 0;
        m_bulletAmount = m_bulletMax;
        m_navAgent.ResetPath();
        m_myState = AgentState.patroling;
        m_healthPackFound = false;
        m_reloading = false;
        m_tookDamage = false;
        m_enemyLost = true;
        // Also reset physics of rigidbody.
        Rigidbody body = GetComponent<Rigidbody>();
        body.velocity = body.angularVelocity = Vector3.zero;
    }
    
    // Trigger related functions
    private void OnTriggerEnter(Collider collision)
    {
        // Check if the colliding object is a bullet and if it's not yours.
        // If so, take damage and destroy bullet.
        BulletBehaviour be_bullet = collision.gameObject.GetComponent<BulletBehaviour>();
        if (collision.tag != transform.name && be_bullet != null)
        {
            m_tookDamage = true;
            if (!invincible)
            {
                // Update health and record overall damage taken.
                m_health -= be_bullet.GetDamage();
                if (m_health <= 0)
                {
                    // Add the previous health to current damage, 
                    // making sure to not include negative damage taken.
                    m_totalDamageTaken += (be_bullet.GetDamage() + m_health);
                    m_health = 0;
                }
                else
                    m_totalDamageTaken += be_bullet.GetDamage();
            }


            Destroy(be_bullet.gameObject);
        }
        else if (collision.tag == "healthPack")
        {
            m_health += 50;
            if (m_health > m_maxHealth)
                m_health = m_maxHealth;
            Destroy(collision.gameObject);
            m_healthPackFound = false;
        }
    }

    // Start by initializing variables
    private void Awake()
    {
        m_health = m_maxHealth;
        m_globals = GameObject.FindGameObjectWithTag("globals").GetComponent<StaticGlobals>();
        if (m_globals == null)
            Debug.LogError("No globals gameobject was found.");

        m_collider = GetComponent<Collider>();
        if (m_collider == null)
            Debug.LogError("Missing collider on shooter agent.");

        m_bulletPrefab = Resources.Load("bullet") as GameObject;
        if (m_bulletPrefab == null)
            Debug.LogError("No bullet prefab was found in shooter agent.");

        m_navAgent = GetComponent<NavMeshAgent>();
        if (m_navAgent == null)
            Debug.LogError("No navmesh agent found on agent.");

        m_simulator = GameObject.FindGameObjectWithTag("matchSimulator").GetComponent<MatchSimulator>();
        if (m_simulator == null)
            Debug.LogError("No MatchSimulator object was found in ShooterAgent");
    }
    #endregion
    
    // Find nearest healthpack and walk towards it.
    public bool GetHealthPack()
    {
        if (!m_healthPackFound)
        {
            GameObject[] healthPacks = GameObject.FindGameObjectsWithTag("healthPack");

            if (healthPacks.Length >= 1)
            {
                Vector3 cheapestPosition = healthPacks[0].transform.position;
                // Loop for finding hp within shortest possible distance.
                foreach (GameObject hp in healthPacks)
                {
                    float distance = Vector3.Magnitude(hp.transform.position - transform.position);
                    if (distance < Vector3.Magnitude(cheapestPosition - transform.position))
                    {
                        m_targetHealthPack = hp;
                        cheapestPosition = hp.transform.position;
                    }
                }

                m_healthPackFound = true;
                // Cancel previous path and walk towards healthpack instead.
                CancelPath();
                WalkTowards(cheapestPosition);
            }
            else
            {
                return false;
            }

        }
        else
        {
            if (m_targetHealthPack == null)
                m_healthPackFound = false;
        }
        return true;
    }

    // Turn towards and shoot against target position.
    public void ShootAt(Vector3 target)
    {
        // Calculate angle of attack
        Vector3 targetDir = (new Vector3(target.x,transform.position.y,target.z) - transform.position).normalized;
        m_bulletTimer += Time.deltaTime;

        // If agent is finished rotating and the bullet-timer is up...
        if (AimTowards(targetDir) 
            && m_bulletTimer >= shootingCooldown 
            && m_bulletAmount > 0
            && !m_reloading)
        {
            m_bulletTimer = 0.0f;
            --m_bulletAmount;
            // Instantiate new bullet
            GameObject newBullet = GameObject.Instantiate(m_bulletPrefab);
            newBullet.transform.SetPositionAndRotation(transform.position, Quaternion.LookRotation(targetDir, Vector3.up));
            newBullet.GetComponent<Rigidbody>().AddForce(targetDir * bulletForce);
            newBullet.transform.tag = transform.name;
        }
    }

    // Reloads weapon when called (if bulletamount is not at max)
    public bool Reload()
    {
        if (!m_reloading && m_bulletAmount < m_bulletMax)
        {
            m_reloading = true;
        }
        else
        {
            // Count time since started reloading
            m_reloadTimer += Time.deltaTime;
            if (m_reloadTimer >= reloadTime)
            {
                // Set bulletamount to max and deactivate reloading.
                m_bulletAmount = m_bulletMax;
                m_reloadTimer = 0.0f;
                m_reloading = false;
            }
        }

        return m_reloading;
    }

    // Set a random destination within the navmesh and bounds of the plane.
    public void SetRandomDestination()
    {
        // First, randomize a point in the bounds of the plane.
        Vector3 minPos = m_globals.GetPlaneMin();
        Vector3 maxPos = m_globals.GetPlaneMax();
        float xPos = Random.Range(minPos.x, maxPos.x);
        float zPos = Random.Range(minPos.z, maxPos.z);
        Vector3 planePos = new Vector3(xPos, transform.position.y, zPos);

        // Then, sample a point nearest to randomized one within the navmesh.
        NavMeshHit hit;
        NavMesh.SamplePosition(planePos, out hit, Mathf.Infinity, NavMesh.AllAreas);
        m_walkingDestinaton = hit.position;
    }

    // Turn and walk towards target position using Unity navmesh system.
    public void WalkTowards(Vector3 target)
    {
        if (!m_navAgent.hasPath)
        {
            m_navAgent.SetDestination(new Vector3(target.x, transform.position.y, target.z));
        }
    }

    // Cancel the current navmesh path
    public void CancelPath()
    {
        m_navAgent.ResetPath();
    }

    // Rotate towards target position.
    private bool AimTowards(Vector3 direction)
    {
        // Disable rotation of agent when navigating
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
        // Check if agent has reached its rotation target
        bool aimedIn = Quaternion.Angle(transform.rotation, targetRotation) < 1.0f ? true : false;
        //m_navAgent.updateRotation = aimedIn;

        return aimedIn;
    }

    // Check for enemy agents within vision.
    public bool EnemyVisible()
    {
        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");

        foreach (GameObject eAgent in agents)
        {
            // Ignore self
            if (eAgent.transform.name != transform.name)
            {
                // Calculate angle and length towards enemy
                Vector3 enemyDir = eAgent.transform.position - transform.position;
                float enemyDistance = enemyDir.magnitude;
                Vector3.Normalize(enemyDir);
                float enemyAngle = Vector3.Angle(transform.forward, enemyDir);

                // If the enemy is within range and angle, perform obstruction test...
                if (enemyDistance <= visionRange && Mathf.Abs(enemyAngle) <= 90.0f)
                {
                    RaycastHit hit = new RaycastHit();

                    // If raycast finds agent, vision towards enemy is not obsctructed.
                    if (Physics.Raycast(transform.position, enemyDir, out hit, Mathf.Infinity, obstructionMask))
                    {
                        if (hit.transform.tag == "agent")
                        {
                            m_targetEnemy = eAgent.transform.position;
                            m_navAgent.updateRotation = false;
                            m_enemyLost = false;
                            return true;
                        }
                    }
                }
            }
        }
        // If no enemy is found, set it so that the navAgent is allowed to rotate the agent towards its walking direction.
        m_navAgent.updateRotation = true;
        // If no enemy could be identified, return false.
        // Keep in mind that the last seen position of the enemy is still stored!
        return false;
    }
    // Cancel current path and turn the agent around.
    public void TurnAround()
    {
        // Reset assumed variables
        //m_healthPackFound = false;
        CancelPath();
        // Calculate turnaround position.
        Vector3 turnAround = transform.position - (transform.forward * 2.0f);
        WalkTowards(turnAround);
    }
    // Avoid bullets by moving from side to side.
    public bool Kite()
    {
        if (!m_navAgent.hasPath || m_myState == AgentState.patroling)
        {
            CancelPath();
            m_myState = AgentState.kiting;
            // Randomize which direction to kite.
            float randomSign = Mathf.Sign(Random.Range(-1.0f, 1.0f));
            float randomAngle = Random.Range(45.0f, 180.0f);
            // Calculate kiting position
            Vector3 kiteVec = transform.position - (Quaternion.AngleAxis(randomSign * randomAngle, Vector3.up) * transform.forward) * 3.0f;
            NavMeshHit hit;
            // Find suitable navmesh point
            NavMesh.SamplePosition(kiteVec, out hit, Mathf.Infinity, NavMesh.AllAreas);
            // Move towards that point.
            WalkTowards(hit.position);
            return true;
        }
        return false;
    }

    #region Get Functions
    public AgentState StateOfAgent { get { return m_myState; } set { m_myState = value; } }
    public int TotalDamageTaken { get { return m_totalDamageTaken; } }
    public int Health { get { return m_health; } }
    public int Bullets { get { return m_bulletAmount; } }
    public bool EnemyLost { get { return m_enemyLost;  } set { m_enemyLost = value; } }
    public Vector3 EnemyPosition { get { return m_targetEnemy; } set { m_targetEnemy = value; } }
    public Vector3 WalkingDestination { get { return m_walkingDestinaton; } }
    public bool HasPath() { return m_navAgent.hasPath; }
    public bool HasFoundHealthpack() { return m_healthPackFound;  }
    // Check if taken damage. If so, return true and mark as false.
    public bool HasTakenDamage() { bool takenDamage = m_tookDamage == true ? true : false; m_tookDamage = false; return takenDamage;  }
    public bool AtEnemyPosition () { return Vector3.Distance(transform.position, m_targetEnemy) <= 0.5f ? true : false; }
    #endregion

    public void SetNavpos(Vector3 position)
    {
        m_navAgent.Warp(position);
    }
    public void SetRot(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    // Used for "button" update.
    private void Update()
    {
        if (damageAgent)
        {
            m_health -= 10;
            damageAgent = false;
        }
        if (registerShot)
        {
            m_tookDamage = true;
            registerShot = false;
        }
    }

    // Main loop for updating the agent.
    private void FixedUpdate()
    {
        //if (!HasPath())
        //{
        //    SetRandomDestination();
        //    WalkTowards(m_walkingDestinaton);
        //}

        if (m_health <= 0 && m_myState != AgentState.dead
            && m_simulator.MatchInProgress)
        {
            //Debug.Log(transform.gameObject.name + " died! ");
            m_myState = AgentState.dead;
            m_simulator.EndMatch();
            if (destroyOnDeath)
                Destroy(this.gameObject);
        }

        //if (m_myState == AgentState.playing)
        //{
        //    if (Bullets <= 0)
        //        Reload();
        //    if (EnemyVisible())
        //    {
        //        ShootAt(m_targetEnemy);

        //        if (destinationLogs)
        //            Debug.Log("Kiting");
        //        Kite();
        //    }

        //    if (m_health > 50 && !EnemyVisible())
        //    {
        //        if (destinationLogs)
        //            Debug.Log("Walking");
        //        if (testDestination != null)
        //            WalkTowards(testDestination.transform.position);
        //        else
        //            WalkTowards(Vector3.zero);
        //    }
        //    else if (m_health <= 50)
        //    {
        //        if (destinationLogs)
        //            Debug.Log("Walking towards hp");
        //        if (!m_healthPackFound)
        //            CancelPath();

        //        GetHealthPack();
        //    }
        //    if (HasTakenDamage() && !EnemyVisible())
        //    {
        //        if (destinationLogs)
        //            Debug.Log("Taking damage!");
        //        TurnAround();
        //    }


        //}
    }
}
