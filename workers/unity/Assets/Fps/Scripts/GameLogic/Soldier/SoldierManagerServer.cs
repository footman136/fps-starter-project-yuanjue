using System.Collections;
using Improbable.Gdk.Core;
using Improbable.Gdk.Health;
using Improbable.Gdk.Subscriptions;
using Pickups;
using Soldiers;
using UnityEngine;
using Improbable.Gdk.Movement;

using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Improbable.Gdk.StandardTypes;
using Improbable;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class SoldierManagerServer : MonoBehaviour
    {
        [Require] private SoldierManagerCommandReceiver commandReceiver;
        [Require] private EntityId entityID;

        private void OnEnable()
        {
            commandReceiver.OnMoveToRequestReceived += OnMoveToCmd;
            commandReceiver.OnShotToRequestReceived += OnShotToCmd;

        }


        private void OnMoveToCmd(SoldierManager.MoveTo.ReceivedRequest request)
        {
            Debug.Log("OnMoverToCmd");

            //找到所有的士兵，发送移动命令
            var soliders = FindObjectsOfType<SoldierAIServer>();

            foreach (SoldierAIServer soldier in soliders) {

                if(entityID.Id == soldier.soldierDataReader.Data.OwnEntityId)
                    soldier.MoveTo(new Vector3(request.Payload.X+Random.Range(-2,2), request.Payload.Y, request.Payload.Z + Random.Range(-2, 2)));
            }
        }

        private void OnShotToCmd(SoldierManager.ShotTo.ReceivedRequest request)
        {
            Debug.Log("OnMoverToCmd");

            //找到所有的士兵，发送射击命令
            var soliders = FindObjectsOfType<SoldierAIServer>();

            foreach (SoldierAIServer soldier in soliders)
            {
                if (entityID.Id == soldier.soldierDataReader.Data.OwnEntityId)
                    soldier.ShotTo(new Vector3(request.Payload.X , request.Payload.Y, request.Payload.Z ));
            }
        }

    }
}
