using System.Collections;
using Improbable.Gdk.Core;
using Improbable.Gdk.Health;
using Improbable.Gdk.Subscriptions;
using Soldiers;
using UnityEngine;
using Improbable.Gdk.Movement;
using System;
using Pickups;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class SoldierManagerClient : MonoBehaviour
    {
        private LinkedEntityComponent link;
        [Require] private SoldierManagerCommandSender commandSender;

        private void OnEnable()
        {
            link = GetComponent<LinkedEntityComponent>();
        }

        //移动士兵命令
        public void sendMoveToCmd(Vector3 pos)
        {
            commandSender.SendMoveToCommand(link.EntityId, new SoldierPos
            {
                X = pos.x,
                Y = pos.y,
                Z = pos.z
            }, onMoveToCommandResponse);
        }

        private void onMoveToCommandResponse(SoldierManager.MoveTo.ReceivedResponse response)
        {

        }

        //射击命令
        public void sendShotToCmd(Vector3 pos)
        {
            commandSender.SendShotToCommand(link.EntityId, new SoldierPos
            {
                X = pos.x,
                Y = pos.y,
                Z = pos.z
            });
        }

    }
}
