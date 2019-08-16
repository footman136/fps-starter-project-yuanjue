using System;
using System.Collections;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Guns;
using Improbable.Gdk.Health;
using Improbable.Gdk.Movement;
using Improbable.Gdk.StandardTypes;
using UnityEngine;
using Pickups;



using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using Improbable;


using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using Unity.Entities;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace Fps
{
    public class FpsDriver : MonoBehaviour
    {
        [System.Serializable]
        private struct CameraSettings
        {
            public float PitchSpeed;
            public float YawSpeed;
            public float MinPitch;
            public float MaxPitch;
        }

        private CommandSystem commandSystem;


        [Require] private ClientMovementWriter authority;
        [Require] private ServerMovementReader serverMovement;
        [Require] private GunStateComponentWriter gunState;
        [Require] private HealthComponentReader health;
        [Require] private HealthComponentCommandSender commandSender;
        [Require] private EntityId entityId;

        [Require] private ShootingComponentWriter shootingWriter;



        private ClientMovementDriver movement;
        private ClientShooting shooting;
        private ShotRayProvider shotRayProvider;
        private FpsAnimator fpsAnimator;
        private GunManager currentGun;

        [SerializeField] private Transform pitchTransform;
        [SerializeField] private new Camera camera;

        [SerializeField] private CameraSettings cameraSettings = new CameraSettings
        {
            PitchSpeed = 1.0f,
            YawSpeed = 1.0f,
            MinPitch = -80.0f,
            MaxPitch = 60.0f
        };

        private bool isRequestingRespawn;
        private Coroutine requestingRespawnCoroutine;

        private IControlProvider controller;
        private InGameScreenManager inGameManager;

        private void Awake()
        {


            movement = GetComponent<ClientMovementDriver>();
            shooting = GetComponent<ClientShooting>();
            shotRayProvider = GetComponent<ShotRayProvider>();
            fpsAnimator = GetComponent<FpsAnimator>();
            fpsAnimator.InitializeOwnAnimator();
            currentGun = GetComponent<GunManager>();
            controller = GetComponent<IControlProvider>();

            var uiManager = GameObject.FindGameObjectWithTag("OnScreenUI")?.GetComponent<UIManager>();
            if (uiManager == null)
            {
                throw new NullReferenceException("Was not able to find the OnScreenUI prefab in the scene.");
            }

            inGameManager = uiManager.InGameManager;
            if (inGameManager == null)
            {
                throw new NullReferenceException("Was not able to find the in-game manager in the scene.");
            }
        }

        private PlayerResManagerClient playerResManager;
        private SoldierManagerClient soldierManager;

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            serverMovement.OnForcedRotationEvent += OnForcedRotation;
            health.OnRespawnEvent += OnRespawn;

            playerResManager = GetComponent<PlayerResManagerClient>();
            soldierManager = GetComponent<SoldierManagerClient>();


        }




        [Require] private WorldCommandSender m_CommandSender;


        private void CreateSoldier()
        {

            //获得射线位置
            var ray = shotRayProvider.GetShotRay(gunState.Data.IsAiming, camera);
            var shotpos = shooting.getShotPos(currentGun.CurrentGunSettings.ShotRange, ray);

            playerResManager.sendSpawnSoldierCmd(shotpos);
        }

        private void MoveSoldier()
        {

            //获得射线位置
            var ray = shotRayProvider.GetShotRay(gunState.Data.IsAiming, camera);
            var shotpos = shooting.getShotPos(currentGun.CurrentGunSettings.ShotRange, ray);

            soldierManager.sendMoveToCmd(shotpos);
        }

        private void ShotSoldier()
        {

            //获得射线位置
            var ray = shotRayProvider.GetShotRay(gunState.Data.IsAiming, camera);
            var shotpos = shooting.getShotPos(currentGun.CurrentGunSettings.ShotRange, ray);

            soldierManager.sendShotToCmd(shotpos);
        }

        private List<EntityId> listHealthPick = new List<EntityId>();

        private void OnCreateEntityResponse(WorldCommands.CreateEntity.ReceivedResponse response)
        {
            if (response.StatusCode == StatusCode.Success)
            {
                var createdEntityId = response.EntityId.Value;
                // handle success
                Debug.Log("create success");
                listHealthPick.Add(createdEntityId);

            }
            else
            {
                // handle failure
                Debug.Log("failure");

            }
        }

        public void DelExampleEntity()
        {

            //获得射线位置
            var ray = shotRayProvider.GetShotRay(gunState.Data.IsAiming, camera);
            var linkentity = shooting.getShotEnityID(currentGun.CurrentGunSettings.ShotRange, ray);

            if (linkentity == null)
                return;

            var healthpickup = linkentity.transform.GetComponent<HealthPickupClientVisibility>();

            if (healthpickup == null)
                return;


            var request = new WorldCommands.DeleteEntity.Request(linkentity.EntityId);

            m_CommandSender.SendDeleteEntityCommand(request, OnDelEntityResponse);

        }


        public void delHealthPickEntity(EntityId id)
        {



            var request = new WorldCommands.DeleteEntity.Request( id );

            m_CommandSender.SendDeleteEntityCommand(request, OnDelEntityResponse);

        }


        private void OnDelEntityResponse(WorldCommands.DeleteEntity.ReceivedResponse response)
        {
            if (response.StatusCode == StatusCode.Success)
            {
                // handle success
                Debug.Log("del success");
            }
            else
            {
                // handle failure
                Debug.Log("failure");

            }
        }

        private void createSomeHealthPickup(int n)
        {
            //随机生成大量血包
            int i = 0;
            for (i = 0; i < n; i++)
            {
                var (spawnPosition, spawnYaw, spawnPitch) = SpawnPoints.GetRandomSpawnPoint();
                CreateHealthPickupByPos(spawnPosition);
            }
        }

        public void CreateHealthPickupByPos(Vector3 pos)
        {


            var exampleEntity = FpsEntityTemplates.HealthPickup(Vector3f.FromUnityVector(pos), 100);
            var request = new WorldCommands.CreateEntity.Request(exampleEntity);

            m_CommandSender.SendCreateEntityCommand(request, OnCreateEntityResponse);

        }


        private void delAllHealthPickup()
        {
            //删除所有血包实体
            foreach (EntityId id in listHealthPick ) {
                delHealthPickEntity(id);
            }
        }

        private void Update()
        {

            //创建一些血包
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                createSomeHealthPickup(30);
            }

            //删除所有血包实体
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                delAllHealthPickup();
            }



            //动态创建实体
            if (Input.GetKeyDown(KeyCode.C))
            {
                CreateSoldier();
            }

            //移动到位置
            if (Input.GetKeyDown(KeyCode.F))
            {
                MoveSoldier();
            }

            //向目标位置射击
            if (Input.GetKeyDown(KeyCode.G))
            {
                ShotSoldier();
            }


            if (controller.MenuPressed)
            {
                inGameManager.TryOpenSettingsMenu();
            }

            // Don't allow controls if in the menu.
            if (inGameManager.InEscapeMenu)
            {
                // Still apply physics.
                movement.ApplyMovement(Vector3.zero, transform.rotation, MovementSpeed.Run, false);
                Animations(false);
                return;
            }

            if (isRequestingRespawn)
            {
                return;
            }

            if (health.Data.Health == 0)
            {
                if (controller.RespawnPressed)
                {
                    isRequestingRespawn = true;
                    requestingRespawnCoroutine = StartCoroutine(RequestRespawn());
                }

                return;
            }

            // Movement
            var toMove = transform.rotation * controller.Movement;

            // Rotation
            var yawDelta = controller.YawDelta;
            var pitchDelta = controller.PitchDelta;

            // Modifiers
            var isAiming = controller.IsAiming;
            var isSprinting = controller.AreSprinting;

            var isJumpPressed = controller.JumpPressed;

            // Events
            var shootPressed = controller.ShootPressed;
            var shootHeld = controller.ShootHeld;


            // Update the pitch speed with that of the gun if aiming.
            var yawSpeed = cameraSettings.YawSpeed;
            var pitchSpeed = cameraSettings.PitchSpeed;
            if (isAiming)
            {
                yawSpeed = currentGun.CurrentGunSettings.AimYawSpeed;
                pitchSpeed = currentGun.CurrentGunSettings.AimPitchSpeed;
            }

            //Mediator
            var movementSpeed = isAiming
                ? MovementSpeed.Walk
                : isSprinting
                    ? MovementSpeed.Sprint
                    : MovementSpeed.Run;
            var yawChange = yawDelta * yawSpeed;
            var pitchChange = pitchDelta * -pitchSpeed;
            var currentPitch = pitchTransform.transform.localEulerAngles.x;
            var newPitch = currentPitch + pitchChange;
            if (newPitch > 180)
            {
                newPitch -= 360;
            }

            newPitch = Mathf.Clamp(newPitch, -cameraSettings.MaxPitch, -cameraSettings.MinPitch);
            pitchTransform.localRotation = Quaternion.Euler(newPitch, 0, 0);
            var currentYaw = transform.eulerAngles.y;
            var newYaw = currentYaw + yawChange;
            var rotation = Quaternion.Euler(newPitch, newYaw, 0);

            //Check for sprint cooldown
            if (!movement.HasSprintedRecently)
            {
                HandleShooting(shootPressed, shootHeld);
            }

            Aiming(isAiming);

            var wasGroundedBeforeMovement = movement.IsGrounded;
            movement.ApplyMovement(toMove, rotation, movementSpeed, isJumpPressed);
            Animations(isJumpPressed && wasGroundedBeforeMovement);
        }

        private IEnumerator RequestRespawn()
        {
            while (true)
            {
                commandSender?.SendRequestRespawnCommand(entityId, new Empty());
                yield return new WaitForSeconds(2);
            }
        }

        private void OnRespawn(Empty _)
        {
            StopCoroutine(requestingRespawnCoroutine);
            isRequestingRespawn = false;
        }

        private void HandleShooting(bool shootingPressed, bool shootingHeld)
        {
            if (shootingPressed)
            {
                shooting.BufferShot();
            }

            var isShooting = shooting.IsShooting(shootingHeld);
            if (isShooting)
            {
                FireShot(currentGun.CurrentGunSettings);
            }
        }       

        private void FireShot(GunSettings gunSettings)
        {
            Transform tmp = null;
            var ray = shotRayProvider.GetShotRay(gunState.Data.IsAiming, camera);
            var info = shooting.FireShot(gunSettings.ShotRange, ray, out tmp);
            shooting.InitiateCooldown(gunSettings.ShotCooldown);
            
            shootingWriter.SendShotsEvent(info);
    }

        private void Aiming(bool shouldBeAiming)
        {
            if (shouldBeAiming != gunState.Data.IsAiming)
            {
                var update = new GunStateComponent.Update
                {
                    IsAiming = shouldBeAiming
                };
                gunState.SendUpdate(update);
            }
        }

        private void Animations(bool isJumping)
        {
            fpsAnimator.SetAiming(gunState.Data.IsAiming);
            fpsAnimator.SetGrounded(movement.IsGrounded);
            fpsAnimator.SetMovement(transform.position, Time.deltaTime);
            fpsAnimator.SetPitch(pitchTransform.transform.localEulerAngles.x);

            if (isJumping)
            {
                fpsAnimator.Jump();
            }
        }

        private void OnForcedRotation(RotationUpdate forcedRotation)
        {
            var newPitch = Mathf.Clamp(forcedRotation.Pitch.ToFloat1k(), -cameraSettings.MaxPitch,
                -cameraSettings.MinPitch);
            pitchTransform.localRotation = Quaternion.Euler(newPitch, 0, 0);
        }
    }
}
