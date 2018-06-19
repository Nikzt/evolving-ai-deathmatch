using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using Opsive.ThirdPersonController;

namespace Opsive.DeathmatchAIKit.AI.Conditions
{
    [TaskCategory("Deathmatch AI Kit")]
    [TaskDescription("Is the agent's health low?")]
    [TaskIcon("Assets/Deathmatch AI Kit/Editor/Images/Icons/DeathmatchAIKitIcon.png")]
    public class IsHealthLow : Conditional
    {
        [@Tooltip("The value to compare to")]
        public SharedFloat m_Amount;
        [@Tooltip("The current target")]
        public SharedGameObject m_Target;

        // Component references
        private Health m_Health;
        private DeathmatchAgent m_DeathmatchAgent;

        /// <summary>
        /// Cache the component references
        /// </summary>
        public override void OnAwake()
        {
            m_Health = GetComponent<Health>();
        }

        public override void OnStart() {
            m_DeathmatchAgent = GetComponent<DeathmatchAgent>();
            m_Amount = m_DeathmatchAgent.phenoType.GetParameter("find health threshold") * 100f;

        }

        /// <summary>
        /// Returns Success if the agent's health is low and the agent does not have a target.
        /// </summary>
        /// <returns>Success if the agent's health is low and the agent does not have a target.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Target.Value != null) {
                return TaskStatus.Failure;
            }
            return m_Health.CurrentHealth + m_Health.CurrentShield < m_Amount.Value ? TaskStatus.Success : TaskStatus.Failure;
        }

        /// <summary>
        /// Reset the SharedVariable values.
        /// </summary>
        public override void OnReset()
        {
            m_Target = null;
            m_Amount = 0;
        }
    }
}