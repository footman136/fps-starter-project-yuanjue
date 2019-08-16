using System.Collections;
using Improbable.Gdk.Core;
using Improbable.Gdk.Health;
using Improbable.Gdk.Subscriptions;
using Pickups;
using UnityEngine;
using Improbable.Gdk.Movement;

using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Improbable.Gdk.StandardTypes;
using Improbable;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class PlayerResManagerServer : MonoBehaviour
    {
        [Require] private PlayerResWriter writer;
        [Require] private PlayerResCommandReceiver commandReceiver;
        [Require] private WorldCommandSender worldCommandSender;

        [Require] private EntityId entityID;

        private void OnEnable()
        {
            commandReceiver.OnSpawnSoldierRequestReceived += OnRequestSpawnSoldier;

        }


        private void OnRequestSpawnSoldier(PlayerRes.SpawnSoldier.ReceivedRequest request)
        {
            //扣除资源生成士兵，100资源一个
            uint needResValue = 100;

            if (writer.Data.ResValue < needResValue)
                return;

            uint newvale = writer.Data.ResValue - needResValue;
            var update = new PlayerRes.Update
            {
                ResValue = new Improbable.Gdk.Core.Option<uint>(newvale)
            };
            writer.SendUpdate(update);

            var exampleEntity = FpsEntityTemplates.Soldier(new Vector3f(request.Payload.X, request.Payload.Y, request.Payload.Z), entityID.Id);
            var request1 = new WorldCommands.CreateEntity.Request(exampleEntity);
            worldCommandSender.SendCreateEntityCommand(request1, OnCreateEntityResponse);
        }

        private void OnCreateEntityResponse(WorldCommands.CreateEntity.ReceivedResponse response)
        {
            if (response.StatusCode == StatusCode.Success)
            {
                var createdEntityId = response.EntityId.Value;
                // handle success
                Debug.Log("create success");

            }
            else
            {
                // handle failure
                Debug.Log("failure");

            }
        }

        public void addRes(uint value)
        {

            uint newvale = writer.Data.ResValue + value;
            var update = new PlayerRes.Update
            {
                ResValue = new Option<uint>(newvale)
            };

            writer.SendUpdate(update);
        }
    }
}
