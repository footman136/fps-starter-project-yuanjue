using System.Collections.Generic;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Health;
using UnityEngine;

using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using Improbable;

namespace Fps
{
    public class VisibilityAndCollision : MonoBehaviour
    {
        [Require] private HealthComponentReader health;

        private bool isVisible = true;

        private CharacterController characterController;
        [SerializeField] private List<Renderer> renderersToIgnore = new List<Renderer>();

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            health.OnHealthUpdate += HealthUpdated;
            UpdateVisibility();
        }

        private void HealthUpdated(float newHealth)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (health == null)
            {
                return;
            }

            var visible = (health.Data.Health > 0);

            if (visible == isVisible)
            {
                return;
            }

            isVisible = visible;

            if (characterController)
            {
                characterController.enabled = visible;
            }

            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderersToIgnore.Contains(childRenderer))
                {
                    childRenderer.enabled = visible;
                }
            }

            if (!needDestroy)
                return;

            if (isDestroy)
                return;

            //删除实体
            isDestroy = true;
            var linkentity = GetComponent<LinkedEntityComponent>();
            var request = new WorldCommands.DeleteEntity.Request(linkentity.EntityId);
            m_CommandSender.SendDeleteEntityCommand(request);

        }

        //销毁控制
        public bool needDestroy = false;
        private bool isDestroy = false;
        [Require] private WorldCommandSender m_CommandSender;

    }
}
