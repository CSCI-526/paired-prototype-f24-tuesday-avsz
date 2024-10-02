using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

public class WalkerAgent : Agent
{
    [Header("Walk Speed")]
    [Range(0.1f, 10)]
    [SerializeField]
    private float m_TargetWalkingSpeed = 10;

    public float MTargetWalkingSpeed
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }

    const float m_maxWalkingSpeed = 10;

    public bool randomizeWalkSpeedEachEpisode;

    private Vector3 m_WorldDirToWalk = Vector3.right;

    [Header("Patrol Circle Settings")]
    public float patrolRadius = 5f; // Radius of the patrol circle
    private int currentTargetIndex = 0; // Current target index along the patrol path
    private Vector3[] patrolPoints; // Array of points along the circle

    [Header("Player Settings")]
    public string playerTag = "Player"; // Tag for the player object
    private GameObject player; // Reference to the player object
    private bool isChasingPlayer = false; // Flag to check if the agent should chase the player


    [Header("Body Parts")]
    public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;

    OrientationCubeController m_OrientationCube;
    DirectionIndicator m_DirectionIndicator;
    JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {


        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(hips);
        m_JdController.SetupBodyPart(chest);
        m_JdController.SetupBodyPart(spine);
        m_JdController.SetupBodyPart(head);
        m_JdController.SetupBodyPart(thighL);
        m_JdController.SetupBodyPart(shinL);
        m_JdController.SetupBodyPart(footL);
        m_JdController.SetupBodyPart(thighR);
        m_JdController.SetupBodyPart(shinR);
        m_JdController.SetupBodyPart(footR);
        m_JdController.SetupBodyPart(armL);
        m_JdController.SetupBodyPart(forearmL);
        m_JdController.SetupBodyPart(handL);
        m_JdController.SetupBodyPart(armR);
        m_JdController.SetupBodyPart(forearmR);
        m_JdController.SetupBodyPart(handR);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetupPatrolCircle(); // Setup the patrol path

        // Find the player object in the scene
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogError("Player object not found! Make sure the Player Tag is set correctly.");
        }
    }

    public override void OnEpisodeBegin()
    {
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        hips.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        MTargetWalkingSpeed = randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;

        currentTargetIndex = 0; // Reset patrol index at the start of the episode
        isChasingPlayer = false; // Reset chasing state
    }

    private void SetupPatrolCircle()
    {
        int numPoints = 4; // Number of patrol points along the circle
        patrolPoints = new Vector3[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            float angle = i * Mathf.PI * 2 / numPoints;
            float x = Mathf.Cos(angle) * patrolRadius;
            float z = Mathf.Sin(angle) * patrolRadius;
            patrolPoints[i] = new Vector3(x, 0, z) + hips.position; // Calculate position based on the circle
        }
    }

    private Vector3 GetNextPatrolPoint()
    {
        currentTargetIndex = (currentTargetIndex + 1) % patrolPoints.Length; // Move to the next point
        return patrolPoints[currentTargetIndex];
    }

    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        sensor.AddObservation(bp.groundContact.touchingGround);
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.position - hips.position));

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = m_OrientationCube.transform.forward;

        var velGoal = cubeForward * MTargetWalkingSpeed;
        var avgVel = GetAvgVelocity();

        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(avgVel));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(velGoal));

        sensor.AddObservation(Quaternion.FromToRotation(hips.forward, cubeForward));
        sensor.AddObservation(Quaternion.FromToRotation(head.forward, cubeForward));
        if (isChasingPlayer)
        {
            sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(player.transform.position));
        }
        else
        {
            sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(patrolPoints[currentTargetIndex]));
        }

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        var continuousActions = actionBuffers.ContinuousActions;
        bpDict[chest].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[spine].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[thighL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[thighR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[shinL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[footR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[footL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[armL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[armR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[forearmL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[head].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);

        bpDict[chest].SetJointStrength(continuousActions[++i]);
        bpDict[spine].SetJointStrength(continuousActions[++i]);
        bpDict[head].SetJointStrength(continuousActions[++i]);
        bpDict[thighL].SetJointStrength(continuousActions[++i]);
        bpDict[shinL].SetJointStrength(continuousActions[++i]);
        bpDict[footL].SetJointStrength(continuousActions[++i]);
        bpDict[thighR].SetJointStrength(continuousActions[++i]);
        bpDict[shinR].SetJointStrength(continuousActions[++i]);
        bpDict[footR].SetJointStrength(continuousActions[++i]);
        bpDict[armL].SetJointStrength(continuousActions[++i]);
        bpDict[forearmL].SetJointStrength(continuousActions[++i]);
        bpDict[armR].SetJointStrength(continuousActions[++i]);
        bpDict[forearmR].SetJointStrength(continuousActions[++i]);
    }

    void UpdateOrientationObjects()
    {
        // Create a temporary GameObject to represent the current patrol point's position.
        GameObject patrolPoint = new GameObject("PatrolPoint");
        if (isChasingPlayer)
        {
            patrolPoint.transform.position = player.transform.position;
        }
        else
        {
            patrolPoint.transform.position = patrolPoints[currentTargetIndex];
        }


        // Use the placeholder GameObject's transform for orientation update.
        m_OrientationCube.UpdateOrientation(hips, patrolPoint.transform);

        if (m_DirectionIndicator)
        {
            m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        }

        // Destroy the temporary GameObject after use to avoid clutter in the scene.
        Destroy(patrolPoint);
    }

    void FixedUpdate()
    {
        UpdateOrientationObjects();

        if (isChasingPlayer && player != null)
        {
            // 平滑转向玩家
            Vector3 directionToPlayer = (player.transform.position - hips.position).normalized;

            // 计算目标旋转角度
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            // 当前的旋转
            Quaternion currentRotation = hips.rotation;

            // 平滑插值旋转，增加旋转速度
            //hips.rotation = Quaternion.Slerp(currentRotation, targetRotation, Time.fixedDeltaTime * 5.0f);

            // 只有在转向接近目标方向时才移动
            if (Quaternion.Angle(currentRotation, targetRotation) < 30.0f) // 减少角度阈值
            {
                //Vector3 currentVelocity = m_JdController.bodyPartsDict[hips].rb.velocity;
                //Vector3 targetVelocity = directionToPlayer * MTargetWalkingSpeed;

                // 使用平滑插值逐渐调整速度
                //m_JdController.bodyPartsDict[hips].rb.velocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 5.0f);
                Debug.Log("WalkerAgent: Moving towards player with stable speed.");
            }
            else
            {
                // 如果还没对准目标，减慢移动速度
                //m_JdController.bodyPartsDict[hips].rb.velocity *= 0.5f;
            }
            float balanceReward = Mathf.Clamp01(Vector3.Dot(Vector3.up, hips.up));
            AddReward(balanceReward * 0.2f);  // 增加平衡奖励的权重

            // 增加追逐的奖励，确保它接近目标
            var matchSpeedReward = GetMatchingVelocityReward(directionToPlayer * MTargetWalkingSpeed, GetAvgVelocity());
            AddReward(matchSpeedReward);
        }
        else
        {
            // 巡逻逻辑
            var cubeForward = m_OrientationCube.transform.forward;
            var matchSpeedReward = GetMatchingVelocityReward(cubeForward * MTargetWalkingSpeed, GetAvgVelocity());

            if (float.IsNaN(matchSpeedReward))
            {
                throw new ArgumentException(
                    "NaN in moveTowardsTargetReward.\n" +
                    $" cubeForward: {cubeForward}\n" +
                    $" hips.velocity: {m_JdController.bodyPartsDict[hips].rb.velocity}\n" +
                    $" maximumWalkingSpeed: {m_maxWalkingSpeed}"
                );
            }

            var headForward = head.forward;
            headForward.y = 0;
            var lookAtTargetReward = (Vector3.Dot(cubeForward, headForward) + 1) * .5F;

            if (float.IsNaN(lookAtTargetReward))
            {
                throw new ArgumentException(
                    "NaN in lookAtTargetReward.\n" +
                    $" cubeForward: {cubeForward}\n" +
                    $" head.forward: {head.forward}"
                );
            }

            AddReward(matchSpeedReward * lookAtTargetReward);

            // 检查 Agent 是否接近当前目标点
            if (Vector3.Distance(hips.position, patrolPoints[currentTargetIndex]) < 1.0f)
            {
                // 移动到下一个巡逻点
                GetNextPatrolPoint();
            }
        }
    }



    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;
        int numOfRb = 0;
        foreach (var item in m_JdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

    public void TouchedTarget()
    {
        AddReward(1f);
    }

    // 方法：开始追逐玩家
    public void StartChasingPlayer()
    {
        isChasingPlayer = true; // 设置追逐标识符为 true
        Debug.Log("WalkerAgent: Start chasing player.");

    }

    // 方法：停止追逐玩家
    public void StopChasingPlayer()
    {
        isChasingPlayer = false; // 设置追逐标识符为 false
    }
}
