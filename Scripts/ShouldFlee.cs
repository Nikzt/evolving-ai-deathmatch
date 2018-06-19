using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using Opsive.ThirdPersonController;

namespace Opsive.DeathmatchAIKit.AI.Conditions
{
    [TaskCategory("Deathmatch AI Kit")]
    [TaskDescription("Should the agent flee?")]
    [TaskIcon("Assets/Deathmatch AI Kit/Editor/Images/Icons/DeathmatchAIKitIcon.png")]
    public class ShouldFlee : Conditional
    {
        [Tooltip("The agent should flee if the health is less than the specified value")]
        public SharedFloat m_HealthAmount;
        [Tooltip("The agent should flee if the distance is greater than the specified value")]
        public SharedFloat m_MinDistance;

        // Component references
        private Health m_Health;
        private GameObject m_Attacker;
        private DeathmatchAgent m_DeathmatchAgent;

        /// <summary>
        /// Cache the component references
        /// </summary>
        public override void OnAwake()
        {
            m_Health = GetComponent<Health>();

            EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject>(gameObject, "OnHealthDamageDetails", OnDamage);
        }

        public override void OnStart() {
            m_DeathmatchAgent = GetComponent<DeathmatchAgent>();
            m_HealthAmount = m_DeathmatchAgent.phenoType.GetParameter("flee health threshold") * 100f;
            m_MinDistance = m_DeathmatchAgent.phenoType.GetParameter("flee distance threshold") * 20f;
        }

        /// <summary>
        /// Returns Success if the agent's health is low and the agent is far away from the attacker.
        /// </summary>
        /// <returns>Success if the agent's health is low and the agent is far away from the attacker.</returns>
        public override TaskStatus OnUpdate()
        {
            // If the attacker is null then the attacking object is no longer alive.
            if (m_Attacker == null) {
                return TaskStatus.Success;
            }
            if (m_Health.CurrentHealth + m_Health.CurrentShield > m_HealthAmount.Value) {
                return TaskStatus.Failure;
            }
            if ((transform.position - m_Attacker.transform.position).magnitude < m_MinDistance.Value) {
                return TaskStatus.Failure;
            }
            return TaskStatus.Success;
        }

        /// <summary>
        /// The task has ended.
        /// </summary>
        public override void OnEnd()
        {
            m_Attacker = null;
        }

        /// <summary>
        /// The agent has taken damage.
        /// </summary>
        /// <param name="amount">The amount of damage received.</param>
        /// <param name="position">The position of the damage.</param>
        /// <param name="force">The force of the damage.</param>
        /// <param name="attacker">The GameObject that attacked the agent.</param>
        private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker)
        {
            m_Attacker = attacker;
        }

        /// <summary>
        /// Reset the SharedVariable values.
        /// </summary>
        public override void OnReset()
        {
            m_HealthAmount = 0;
            m_MinDistance = 0;
        }
    }
}