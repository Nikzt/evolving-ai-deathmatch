using UnityEngine;
using Opsive.ThirdPersonController;
using Opsive.DeathmatchAIKit.AI;

namespace BehaviorDesigner.Runtime.Tasks
{
    [TaskDescription("The random probability task will return success when the random probability is above the succeed probability. It will otherwise return failure.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=33")]
    public class RandomProbability : Conditional
    {
        [Tooltip("The chance that the task will return success")]
        public SharedFloat successProbability = 0.5f;
        [Tooltip("Seed the random number generator to make things easier to debug")]
        public SharedInt seed;
        [Tooltip("Do we want to use the seed?")]
        public SharedBool useSeed;
        [Tooltip("What are we rolling the dice on?")]
        public string purpose;

        private DeathmatchAgent m_DeathmatchAgent;

        public override void OnAwake()
        {
            // If specified, use the seed provided.
            if (useSeed.Value) {
#if UNITY_5_3
                Random.seed = seed.Value;
#else
                Random.InitState(seed.Value);
#endif
            }
        }

        public override void OnStart() {
            m_DeathmatchAgent = GetComponent<DeathmatchAgent>();
            if (purpose == "cover") {
                successProbability = m_DeathmatchAgent.phenoType.GetParameter("cover probability");
            }
        }

        public override TaskStatus OnUpdate()
        {
            // Return success if random value is less than the success probability. Otherwise return failure.
            float randomValue = Random.value;
            if (randomValue < successProbability.Value) {
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            // Reset the public properties back to their original values
            successProbability = 0.5f;
            seed = 0;
            useSeed = false;
        }
    }
}