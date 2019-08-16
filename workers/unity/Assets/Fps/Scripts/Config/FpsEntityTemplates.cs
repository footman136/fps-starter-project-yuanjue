using System.Text;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Guns;
using Improbable.Gdk.Health;
using Improbable.Gdk.Movement;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using Improbable.Gdk.Session;
using Improbable.Gdk.StandardTypes;

using Pickups;
using Soldiers;


namespace Fps
{
    public static class FpsEntityTemplates
    {

        public static EntityTemplate HealthPickup(Vector3f position, uint healthValue)
        {
            // Create a HealthPickup component snapshot which is initially active and grants "heathValue" on pickup.
            var healthPickupComponent = new Pickups.HealthPickup.Snapshot(true, healthValue);

            var entityTemplate = new EntityTemplate();
            entityTemplate.AddComponent(new Position.Snapshot(new Coordinates(position.X, position.Y, position.Z)), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Metadata.Snapshot("HealthPickup"), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(healthPickupComponent, WorkerUtils.UnityGameLogic);
            entityTemplate.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient);
            entityTemplate.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return entityTemplate;
        }



        public static EntityTemplate DeploymentState()
        {
            const uint sessionTimeSeconds = 300;

            var position = new Position.Snapshot();
            var metadata = new Metadata.Snapshot { EntityType = "DeploymentState" };

            var template = new EntityTemplate();
            template.AddComponent(position, WorkerUtils.UnityGameLogic);
            template.AddComponent(metadata, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Session.Snapshot(Status.LOBBY), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Deployment.Snapshot(), WorkerUtils.DeploymentManager);
            template.AddComponent(new Timer.Snapshot(0, sessionTimeSeconds), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.DeploymentManager, WorkerUtils.UnityClient, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        public static EntityTemplate Spawner(Coordinates spawnerCoordinates)
        {
            var position = new Position.Snapshot(spawnerCoordinates);
            var metadata = new Metadata.Snapshot("PlayerCreator");

            var template = new EntityTemplate();
            template.AddComponent(position, WorkerUtils.UnityGameLogic);
            template.AddComponent(metadata, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new PlayerCreator.Snapshot(), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        public static EntityTemplate Player(string workerId, byte[] args)
        {
            var client = EntityTemplate.GetWorkerAccessAttribute(workerId);

            var (spawnPosition, spawnYaw, spawnPitch) = SpawnPoints.GetRandomSpawnPoint();

            var serverResponse = new ServerResponse
            {
                Position = spawnPosition.ToIntAbsolute()
            };

            var rotationUpdate = new RotationUpdate
            {
                Yaw = spawnYaw.ToInt1k(),
                Pitch = spawnPitch.ToInt1k()
            };

            var pos = new Position.Snapshot { Coords = spawnPosition.ToSpatialCoordinates() };
            var serverMovement = new ServerMovement.Snapshot { Latest = serverResponse };
            var clientMovement = new ClientMovement.Snapshot { Latest = new ClientRequest() };
            var clientRotation = new ClientRotation.Snapshot { Latest = rotationUpdate };
            var shootingComponent = new ShootingComponent.Snapshot();
            var gunComponent = new GunComponent.Snapshot { GunId = PlayerGunSettings.DefaultGunIndex };
            var gunStateComponent = new GunStateComponent.Snapshot { IsAiming = false };
            var healthComponent = new HealthComponent.Snapshot
            {
                Health = PlayerHealthSettings.MaxHealth,
                MaxHealth = PlayerHealthSettings.MaxHealth,
            };

            var healthRegenComponent = new HealthRegenComponent.Snapshot
            {
                CooldownSyncInterval = PlayerHealthSettings.SpatialCooldownSyncInterval,
                DamagedRecently = false,
                RegenAmount = PlayerHealthSettings.RegenAmount,
                RegenCooldownTimer = PlayerHealthSettings.RegenAfterDamageCooldown,
                RegenInterval = PlayerHealthSettings.RegenInterval,
                RegenPauseTime = 0,
            };

            var sessionQuery = InterestQuery.Query(Constraint.Component<Session.Component>());
            var checkoutQuery = InterestQuery.Query(Constraint.RelativeCylinder(150));

            var interestTemplate = InterestTemplate.Create().AddQueries<ClientMovement.Component>(sessionQuery, checkoutQuery);
            var interestComponent = interestTemplate.ToSnapshot();

            var playerName = Encoding.ASCII.GetString(args);

            var playerStateComponent = new PlayerState.Snapshot
            {
                Name = playerName,
                Kills = 0,
                Deaths = 0,
            };


            var playerResComponent = new Pickups.PlayerRes.Snapshot( 0 );
            var soldierManagerComponent = new Soldiers.SoldierManager.Snapshot();


            var template = new EntityTemplate();
            template.AddComponent(pos, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.UnityGameLogic);
            template.AddComponent(serverMovement, WorkerUtils.UnityGameLogic);
            template.AddComponent(clientMovement, client);
            template.AddComponent(clientRotation, client);

            template.AddComponent(shootingComponent, client);
            template.AddComponent(gunComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(gunStateComponent, client);
            template.AddComponent(healthComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(healthRegenComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(playerStateComponent, WorkerUtils.UnityGameLogic);

            template.AddComponent(interestComponent, WorkerUtils.UnityGameLogic);

            template.AddComponent(playerResComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(soldierManagerComponent, WorkerUtils.UnityGameLogic);


            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityClient, WorkerUtils.UnityGameLogic, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        public static EntityTemplate Soldier(Vector3f position ,long ownentityid)
        {

            var healthComponent = new HealthComponent.Snapshot
            {
                Health = PlayerHealthSettings.MaxHealth,
                MaxHealth = PlayerHealthSettings.MaxHealth,
            };

            var healthRegenComponent = new HealthRegenComponent.Snapshot
            {
                CooldownSyncInterval = PlayerHealthSettings.SpatialCooldownSyncInterval,
                DamagedRecently = false,
                RegenAmount = PlayerHealthSettings.RegenAmount,
                RegenCooldownTimer = PlayerHealthSettings.RegenAfterDamageCooldown,
                RegenInterval = PlayerHealthSettings.RegenInterval,
                RegenPauseTime = 0,
            };


            var playerStateComponent = new PlayerState.Snapshot
            {
                Name = "Soldier",
                Kills = 0,
                Deaths = 0,
            };


            var serverResponse = new ServerResponse
            {
                Position = position.ToUnityVector3().ToIntAbsolute()
            };

            var rotationUpdate = new RotationUpdate
            {
                Yaw = 0,
                Pitch = 0
            };

            var soldierData = new SoldierData.Snapshot
            {
                OwnEntityId = ownentityid
            };


            var serverMovement = new ServerMovement.Snapshot { Latest = serverResponse };
            var clientMovement = new ClientMovement.Snapshot { Latest = new ClientRequest() };
            var clientRotation = new ClientRotation.Snapshot { Latest = rotationUpdate };


            var shootingComponent = new ShootingComponent.Snapshot();
            var gunComponent = new GunComponent.Snapshot { GunId = PlayerGunSettings.DefaultGunIndex };
            var gunStateComponent = new GunStateComponent.Snapshot { IsAiming = false };


            var entityTemplate = new EntityTemplate();
            entityTemplate.AddComponent(new Position.Snapshot(new Coordinates(position.X, position.Y, position.Z)), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Metadata.Snapshot("Soldier"), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);

            entityTemplate.AddComponent(shootingComponent, WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(gunComponent, WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(gunStateComponent, WorkerUtils.UnityGameLogic);

            entityTemplate.AddComponent(healthComponent, WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(healthRegenComponent, WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(playerStateComponent, WorkerUtils.UnityGameLogic);


            entityTemplate.AddComponent(serverMovement, WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(clientMovement, WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(clientRotation, WorkerUtils.UnityGameLogic);

            entityTemplate.AddComponent(soldierData, WorkerUtils.UnityGameLogic);

            


            entityTemplate.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient);
            entityTemplate.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
            return entityTemplate;
        }

    }
}
