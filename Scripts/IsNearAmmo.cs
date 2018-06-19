using UnityEngine;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using Opsive.ThirdPersonController;
using BehaviorDesigner.Runtime.Tasks.ThirdPersonController;

namespace Opsive.DeathmatchAIKit.AI.Conditions
{
    [TaskCategory("Deathmatch AI Kit")]
    [TaskDescription("Is the agent near ammo and needs that ammo?")]
    [TaskIcon("Assets/Deathmatch AI Kit/Editor/Images/Icons/DeathmatchAIKitIcon.png")]
    public class IsNearAmmo : Conditional
    {
        [Tooltip("The agent needs the weapon if the inventory current has less than the specified amount")]
        [SerializeField] protected SharedFloat m_PickupAmount = 5;
        [Tooltip("The agent needs the power weapon if the inventory current has less than the specified amount")]
        [SerializeField] protected SharedFloat m_PowerWeaponPickupAmount = 2;
        [Tooltip("Should the agent check for ammo using a distance check?")]
        [SerializeField] protected SharedBool m_DistanceCheck;
        [Tooltip("The agent will seek the ammo if performing a distance check and is within the specified distance")]
        [SerializeField] protected SharedFloat m_Distance = 10;
        [Tooltip("The ItemType of the ammo that the agent is near")]
        [SharedRequired] [SerializeField] protected SharedItemType m_ItemType;

        // Internal variables
        private ItemType m_FoundItemType;
        private List<ItemType> m_ItemTypes = new List<ItemType>();
        private List<Transform> m_ItemPickups = new List<Transform>();

        // Component references
        private DeathmatchAgent m_DeathmatchAgent;
        private Inventory m_Inventory;

        /// <summary>
        /// Cache the component references.
        /// </summary>
        public override void OnAwake()
        {
            m_Inventory = GetComponent<Inventory>();

            var allItemPickups = GameObject.FindObjectsOfType<ItemPickup>();
            for (int i = 0; i < allItemPickups.Length; ++i) {
                for (int j = 0; j < allItemPickups[i].ItemList.Count; ++j) {
                    // The PrimaryItemType will always contain ConsumableItemTypes that belong to that PrimaryItemType.
                    if (!(allItemPickups[i].ItemList[j].ItemType is PrimaryItemType)) {
                        continue;
                    }
                    m_ItemTypes.Add(allItemPickups[i].ItemList[j].ItemType);
                    m_ItemPickups.Add(allItemPickups[i].transform);
                    break;
                }
            }
        }

        public override void OnStart() {
            m_DeathmatchAgent = GetComponent<DeathmatchAgent>();
            m_Distance = m_DeathmatchAgent.phenoType.GetParameter("ammo distance threshold") * 20f;
        }


        /// <summary>
        /// Returns success if an item is near.
        /// </summary>
        /// <returns></returns>
        public override TaskStatus OnUpdate()
        {


            if (m_FoundItemType != null) {
                m_ItemType.Value = m_FoundItemType;
                return TaskStatus.Success;
            }

            // Determine if the agent is close to any pickups.
            if (m_DistanceCheck.Value) {
                var closestDistance = float.MaxValue;
                var closestIndex = -1;
                for (int i = 0; i < m_ItemTypes.Count; ++i) {
                    if (!m_ItemPickups[i].gameObject.activeInHierarchy) {
                        continue;
                    }
                    var itemDistance = (m_ItemPickups[i].position - transform.position).magnitude;
                    if (itemDistance < m_Distance.Value && itemDistance < closestDistance) {
                        closestDistance = itemDistance;
                        closestIndex = i;
                    }
                }

                if (closestIndex != -1) {
                    var itemType = m_ItemTypes[closestIndex];
                    // The item is only found if the character us low on ammo from that ammo type.
                    var amount = m_Inventory.GetItemCount(itemType, true) + m_Inventory.GetItemCount(itemType, false);
                    if (amount < (m_DeathmatchAgent.WeaponStatForItemType(itemType).Class == DeathmatchAgent.WeaponStat.WeaponClass.Power ? m_PowerWeaponPickupAmount.Value : m_PickupAmount.Value)) {
                        m_ItemType.Value = itemType;
                        return TaskStatus.Success;
                    }
                }
            }

            return TaskStatus.Failure;
        }

        /// <summary>
        /// The item is no longer found.
        /// </summary>
        public override void OnEnd()
        {
            m_FoundItemType = null;
        }

        /// <summary>
        /// The agent has entered a trigger. Check to seee if the trigger belongs to an ItemPickup. 
        /// </summary>
        /// <param name="other">The possible item.</param>
        public override void OnTriggerEnter(Collider other)
        {
            ItemPickupLocator itemPickupLocator = null;
            if ((itemPickupLocator = Utility.GetComponentForType<ItemPickupLocator>(other.gameObject) as ItemPickupLocator) != null) {
                var itemPickup = itemPickupLocator.ParentItemPickup;
                for (int i = 0; i < itemPickup.ItemList.Count; ++i) {
                    // The PrimaryItemType will always contain ConsumableItemTypes that belong to that PrimaryItemType.
                    if (!(itemPickup.ItemList[i].ItemType is PrimaryItemType)) {
                        continue;
                    }

                    // The item is only found if the character us low on ammo from that ammo type.
                    var amount = m_Inventory.GetItemCount(itemPickup.ItemList[i].ItemType, true) + m_Inventory.GetItemCount(itemPickup.ItemList[i].ItemType, false);
                    if (amount < (m_DeathmatchAgent.WeaponStatForItemType(itemPickup.ItemList[i].ItemType).Class == DeathmatchAgent.WeaponStat.WeaponClass.Power ? 
                                                                                    m_PowerWeaponPickupAmount.Value : m_PickupAmount.Value)) {
                        m_FoundItemType = itemPickup.ItemList[i].ItemType;
                    }
                }
            }
        }
    }
}