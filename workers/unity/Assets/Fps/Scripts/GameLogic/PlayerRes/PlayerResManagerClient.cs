using System.Collections;
using Improbable.Gdk.Core;
using Improbable.Gdk.Health;
using Improbable.Gdk.Subscriptions;
using Pickups;
using UnityEngine;
using Improbable.Gdk.Movement;
using System;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class PlayerResManagerClient : MonoBehaviour
    {
        [Require] private PlayerResReader reader;
        [Require] private PlayerResCommandSender commandSender;

        private InGameScreenManager inGameManager;
        private LinkedEntityComponent link;

        private void Awake()
        {

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

        private void OnEnable()
        {
            link = GetComponent<LinkedEntityComponent>();
            reader.OnUpdate += OnPlayerResComponentUpdated;
            inGameManager.txtResValue.text = "" + reader.Data.ResValue + " 吃血包 C造兵 F移动 G攻击"; 
        }

        private void OnPlayerResComponentUpdated(PlayerRes.Update update)
        {
            // Check whether a specific property was updated.
            if (!update.ResValue.HasValue)
            {
                return;
            }

            // do something with the new CurrentHealth value
            inGameManager.txtResValue.text = "" + update.ResValue.Value + " 吃血包 C造兵 F移动 G攻击";

        }

        private void Update()
        {
            //inGameManager.txtResValue.text = "" + reader.Data.ResValue;

        }




        public void sendSpawnSoldierCmd(Vector3 pos)
        {
            Debug.Log("sendSpawnSoldierCmd");

            commandSender.SendSpawnSoldierCommand(link.EntityId, new SoldierPos
            {
                X = pos.x,
                Y = pos.y,
                Z = pos.z
            }, onSpawnSoldierCommandResponse);
        }

        private void onSpawnSoldierCommandResponse(PlayerRes.SpawnSoldier.ReceivedResponse response)
        {
            Debug.Log("onSpawnSoldierCommandResponse");
        }

    }
}
