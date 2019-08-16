using System.Collections;
using Fps;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Guns;
using Improbable.Gdk.Health;
using Improbable.Gdk.Movement;
using UnityEngine;
using UnityEngine.AI;

using Soldiers;

public class SoldierAIServer : MonoBehaviour
{

    private ClientMovementDriver movementDriver;
    private ClientShooting shooting;
    private LinkedEntityComponent spatial;
    private NavMeshAgent agent;
    private GameLogicWorkerConnector coordinator;

    private Vector3 anchorPoint;
    private const float MovementRadius = 50f;
    private const float NavMeshSnapDistance = 5f;
    private const float MinRemainingDistance = 0.3f;

    private bool jumpNext;
    private bool sprintNext;

    private int similarPositionsCount = 0;
    private Vector3 lastPosition;


    private Bounds worldBounds;


    [Require] public SoldierDataReader soldierDataReader;
    [Require] private ShootingComponentWriter shootingWriter;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        movementDriver = GetComponent<ClientMovementDriver>();
        shooting = GetComponent<ClientShooting>();
        coordinator = FindObjectOfType<GameLogicWorkerConnector>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.Warp(transform.position);
        anchorPoint = transform.position;
        worldBounds = coordinator.GetWorldBounds();

    }

    private void OnEnable()
    {
        spatial = GetComponent<LinkedEntityComponent>();

    }



    private void Update()
    {
        Update_LookingForTarget();
        Update_ShootingTarget();
    }
    private bool isShooting = false;
    private void Update_ShootingTarget()
    {
        if (!isShooting)
            return;

        var gunOrigin = transform.position + Vector3.up;
        var targetCenter = target;

        var targetRotation = Quaternion.LookRotation(targetCenter - gunOrigin);
        var rotationAmount = Quaternion.RotateTowards(transform.rotation, targetRotation, 10f);

        var destination = transform.position;

        MoveTowards(destination, MovementSpeed.Run, rotationAmount, false);

        if (shooting.IsShooting(true) && Mathf.Abs(Quaternion.Angle(targetRotation, transform.rotation)) < 5)
        {
            Transform tmp = null;

            var info = shooting.FireShot(200f, new Ray(gunOrigin, transform.forward),out tmp);
            shooting.InitiateCooldown(0.2f);

            //士兵不攻击自己人
            bool canshot = true;
            if (tmp != null) {
                var solider = tmp.GetComponent<SoldierAIServer>();
                if (solider != null) {
                    if (soldierDataReader.Data.OwnEntityId == solider.soldierDataReader.Data.OwnEntityId)
                        canshot = false;
                }
            }

            if(canshot)
                shootingWriter.SendShotsEvent(info);

        }

    }


    private void Update_LookingForTarget()
    {        
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 0.5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
        else if (agent.remainingDistance < MinRemainingDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            !agent.hasPath)
        {

            //SetRandomDestination();

        }
        else if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            var velocity = agent.desiredVelocity;
            velocity.y = 0;
            if (velocity != Vector3.zero)
            {
                var rotation = Quaternion.LookRotation(velocity, Vector3.up);
                var speed = sprintNext ? MovementSpeed.Sprint : MovementSpeed.Run;
                MoveTowards(transform.position + velocity, speed, rotation, jumpNext);
                jumpNext = false;
            }
        }

        agent.nextPosition = transform.position;
    }


    private void MoveTowards(Vector3 destination, MovementSpeed speed, Quaternion rotation, bool jump = false)
    {
        var agentPosition = agent.nextPosition;
        var direction = (destination - agentPosition).normalized;
        var desiredVelocity = direction * movementDriver.GetSpeed(speed);

        // Setting nextPosition will move the agent towards destination, but is constrained by the navmesh
        agent.nextPosition += desiredVelocity * Time.deltaTime;

        // Getting nextPosition here will return the constrained position of the agent
        var actualDirection = (agent.nextPosition - agentPosition).normalized;

        movementDriver.ApplyMovement(actualDirection, rotation, speed, jump);
    }

    public void MoveTo(Vector3 pos) {
        var destination = pos;
        if (NavMesh.SamplePosition(destination, out var hit, NavMeshSnapDistance, NavMesh.AllAreas))
        {
            if (worldBounds.Contains(hit.position))
            {
                agent.isStopped = false;
                agent.nextPosition = transform.position;
                agent.SetDestination(hit.position);
            }
        }
        isShooting = false;

    }

    private Vector3 target;
    public void ShotTo(Vector3 pos)
    {
        target = pos;
        isShooting = true;
    }

}
