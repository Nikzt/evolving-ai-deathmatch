using UnityEngine;
#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4)
using UnityEngine.AI;
#endif
using Opsive.ThirdPersonController;
using BehaviorDesigner.Runtime.Tasks.ThirdPersonController;
using System.Collections.Generic;
using Opsive.DeathmatchAIKit.UI;

namespace Opsive.DeathmatchAIKit.AI
{
    /// <summary>
    /// Extends AIAgent by allowing the agent to detect any targets and keeps the list of possible weapons.
    /// </summary>
    public class DeathmatchAgent : AIAgent
    {
        /// <summary>
        /// Specifies how a Third Person Controller ItemType can be used by the agent.
        /// </summary>
        [System.Serializable]
        public class WeaponStat
        {
            // The type of weapon
            public enum WeaponClass { Power, Primary, Secondary, Melee, Grenade }

            [Tooltip("The Third Person Controller ItemType")]
            [SerializeField] ItemType m_ItemType;
            [Tooltip("Specifies the type of weapon")]
            [SerializeField] WeaponClass m_Class = WeaponClass.Primary;
            [Tooltip("A curve describing how likely an item can be used at any distance. The higher the value the more likely that item will be used")]
            [SerializeField] AnimationCurve m_UseLikelihood = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
            [Tooltip("The minimum distance that the item can be used")]
            [SerializeField] float m_MinUse = 5;
            [Tooltip("The maximum distance that the item can be used")]
            [SerializeField] float m_MaxUse = 15;
            [Tooltip("Can this item damage multiple targets in one hit?")]
            [SerializeField] bool m_GroupDamage;

            // Exposed properties
            public ItemType ItemType { get { return m_ItemType; } set { m_ItemType = value; } }
            public WeaponClass Class { get { return m_Class; } set { m_Class = value; } }
            public float MinUse { get { return m_MinUse; } }
            public float MaxUse { get { return m_MaxUse; } }
            public bool GroupDamage { get { return m_GroupDamage; } }

            // Internal variables
            private float m_PrevTargetDistance = -1;
            private float m_PrevUseLikelihood;
            
            /// <summary>
            /// Returns the use likelihood based off of the target distance.
            /// </summary>
            /// <param name="targetDistance">The distance that the weapon is being evaluated at.</param>
            /// <returns>The use likelhood based off of the target distance.</returns>
            public float GetUseLikelihood(float targetDistance)
            {
                // Cache the distance to prevent the agent from having to reevaluate the curve every tick.
                if (m_PrevTargetDistance == targetDistance) {
                    return m_PrevUseLikelihood;
                }
                m_PrevTargetDistance = targetDistance;

                // The weapon cannot be used if the distance is out of range.
                if (targetDistance > m_MaxUse || targetDistance < m_MinUse) {
                    m_PrevUseLikelihood = 0;
                } else {
                    // Normalize the target distance based off of the min and max use distance.
                    var normalizedTargetDistance = (targetDistance - m_MinUse) / (m_MaxUse - m_MinUse);
                    m_PrevUseLikelihood = m_UseLikelihood.Evaluate(normalizedTargetDistance);
                }

                return m_PrevUseLikelihood;
            }
        }

        [Tooltip("Should the DeathmatchAgent add the agent to a team?")]
        [SerializeField] protected bool m_AddToTeam;
        [Tooltip("If AddToTeam is enabled, specifies the team that the agent should be added to")]
        [SerializeField] protected int m_TeamIndex;
        [Tooltip("Specifies the layers that the targets are in")]
        [SerializeField] protected LayerMask m_TargetLayerMask = (1 << LayerManager.Enemy) | (1 << LayerManager.Player);
        [Tooltip("Specifies any layers that the sight check should ignore")]
        [SerializeField] protected LayerMask m_IgnoreLayerMask = (1 << LayerManager.IgnoreRaycast) | (1 << LayerManager.VisualEffect);
        [Tooltip("Optionally specify a transform to determine where to check the line of sight from")]
        [SerializeField] protected Transform m_LookTransform;
        [Tooltip("If the sight check locates a humanoid, specify the bone that the agent should look at")]
        [SerializeField] protected HumanBodyBones m_TargetBone = HumanBodyBones.Head;
        [Tooltip("The amount of weight to apply to the distance when determining which target to attack score. The higher the value the more influence the distance has")]
        [SerializeField] protected float m_DistanceScoreWeight = 2;
        [Tooltip("The amount of weight to apply to the angle when determining which target to attack score. The higher the value the more influence the angle has")]
        [SerializeField] protected float m_AngleScoreWeight = 1;
        [Tooltip("All possible weapons that the agent can carry")]
        [SerializeField] protected WeaponStat[] m_AvailableWeapons;

#if UNITY_EDITOR
        // Used by the editor to keep the available weapon selection.
        [SerializeField] protected int m_SelectedAvailableWeapon = -1;
        public int SelectedAvailableWeapon { get { return m_SelectedAvailableWeapon; } set { m_SelectedAvailableWeapon = value; } }
#endif

        // Internal variables
        private Dictionary<ItemType, WeaponStat> m_AvailableWeaponMap;
        private Dictionary<Transform, Transform> m_TransformBoneMap = new Dictionary<Transform, Transform>();
        private Collider[] m_HitColliders;
        private CoverPoint m_CoverPoint;

        // Genetic Algorithm variables
        private EvolutionController evolver;
        public PhenoType phenoType;
        private string playerName;

        // Exposed properties
        public bool AddToTeam { set { m_AddToTeam = value; } }
        public WeaponStat[] AvailableWeapons { get { return m_AvailableWeapons; } set { m_AvailableWeapons = value; } }
        public Transform LookTransform { get { return m_LookTransform; } set { m_LookTransform = value; } }
        public LayerMask TargetLayerMask { get { return m_TargetLayerMask; } set { m_TargetLayerMask = value; } }
        public CoverPoint CoverPoint { get { return m_CoverPoint; } set { m_CoverPoint = value;} }
        private Vector3 LookPosition { get { return (m_CoverPoint == null || m_Controller.Aiming)  ? m_LookTransform.position : m_CoverPoint.AttackPosition; } }

        // Component references
        private RigidbodyCharacterController m_Controller;
        private CapsuleCollider m_CapsuleCollider;

        /// <summary>
        /// Cache component references and initializes default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (m_LookTransform == null) {
                m_LookTransform = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
            }
            m_Controller = GetComponent<RigidbodyCharacterController>();
            m_CapsuleCollider = GetComponent<CapsuleCollider>();
#if !(UNITY_5_1 || UNITY_5_2)
            m_HitColliders = new Collider[10];
#endif
            m_AvailableWeaponMap = new Dictionary<ItemType, WeaponStat>();
            for (int i = 0; i < m_AvailableWeapons.Length; ++i) {
                m_AvailableWeaponMap.Add(m_AvailableWeapons[i].ItemType, m_AvailableWeapons[i]);
            }
        }

        /// <summary>
        /// Adds the agent to the team if requested.
        /// </summary>
        private void Start()
        {
            if (m_AddToTeam) {
                TeamManager.AddTeamMember(gameObject, m_TeamIndex);
            }
            // Genetic Algorithm initialize
            evolver = GameObject.Find("Evolution Controller").GetComponent<EvolutionController>();
            playerName = this.gameObject.name;
            phenoType = evolver.GetPhenoType(playerName);
        }

        /// <summary>
        /// Returns any targets in sight. If multiple targets are in sight the target most directly in front of the agent will be returned.
        /// </summary>
        /// <param name="fieldOfView">The maximum field of view that the agent can see other targets (in degrees).</param>
        /// <param name="maxDistance">The maximum distance that the agent can see other targets.</param>
        /// <param name="ignoreTargets">The targets that can be ignored.</param>
        /// <returns>A reference to the target in sight. Can be null if no targets are in sight.</returns>
        public GameObject TargetInSight(float fieldOfView, float maxDistance, HashSet<GameObject> ignoreTargets)
        {
            GameObject foundTarget = null;
            // Find all of the nearby objects with the specified layer mask.
#if UNITY_5_1 || UNITY_5_2
            m_HitColliders = Physics.OverlapSphere(LookPosition, maxDistance, m_TargetLayerMask);
            if (m_HitColliders != null) {
                var hitCount = m_HitColliders.Length;
#else
            var hitCount = Physics.OverlapSphereNonAlloc(LookPosition, maxDistance, m_HitColliders, m_TargetLayerMask);
            if (hitCount > 0) {
#endif
                // The angle and distance are normalized to get a 0 - 2 value. The target with the highest value will be returned.
                var highestScore = Mathf.NegativeInfinity;
                var maxDistanceSqr = maxDistance * maxDistance;
                var fieldOfViewHalf = fieldOfView * 0.5f;
                for (int i = 0; i < hitCount; ++i) {
                    // The agent cannot see themself.
                    if (m_HitColliders[i].transform == m_Transform) {
                        continue;
                    }

                    // Don't return any targets that are within the ignore list.
                    if (ignoreTargets.Contains(m_HitColliders[i].gameObject)) {
                        continue;
                    }

                    float angle;
                    // The position should only be checked when cover is not active. The cover attack point will always be valid so no check is necessary.
                    if (TargetInSight(m_HitColliders[i].transform, m_CoverPoint == null, fieldOfView, maxDistanceSqr, out angle)) {
                        // The object is in sight, determine the score to determine which is the best target.
                        var score = ((1 - ((m_HitColliders[i].transform.position - LookPosition).magnitude / maxDistance)) * m_DistanceScoreWeight) + 
                                    ((1 - (angle / fieldOfViewHalf)) * m_AngleScoreWeight);
                        if (score > highestScore) {
                            highestScore = score;
                            foundTarget = m_HitColliders[i].gameObject;
                        }
                    }
                }
            }
            return foundTarget;
        }

        /// <summary>
        /// Is the specified target in sight?
        /// </summary>
        /// <param name="target">The target to determine if it is in sight.</param>
        /// <returns>True if the target is in sight.</returns>
        public bool TargetInSight(Transform target, float fieldOfView)
        {
            float angle;
            return TargetInSight(target, false, fieldOfView, float.MaxValue, out angle);
        }

        /// <summary>
        /// Is the specified target in sight?
        /// </summary>
        /// <param name="target">The target to determine if it is in sight.</param>
        /// <param name="checkPosition">Should the pathfinding position be checked?</param>
        /// <param name="fieldOfView">The maximum field of view that the agent can see other targets (in degrees).</param>
        /// <param name="maxDistanceSqr">The maximum distance squared that the agent can see other targets.</param>
        /// <param name="angle">If a target is within sight, specifies the angle difference between the target and the current agent.</param>
        /// <returns>True if the target is in sight.</returns>
        private bool TargetInSight(Transform target, bool checkPosition, float fieldOfView, float maxDistanceSqr, out float angle)
        {
            // The target has to exist.
            if (target == null) {
                angle = 0;
                return false;
            }
            var direction = target.position - LookPosition;
            // The target has to be within distance.
            if (direction.sqrMagnitude > maxDistanceSqr) {
                angle = 0;
                return false;
            }
            direction.y = 0;
            angle = Vector3.Angle(direction, m_CoverPoint == null ? m_Transform.forward : m_CoverPoint.transform.forward);
            // The target has to be in the field of view.
            if (angle > fieldOfView * 0.5f) {
                return false;
            }
            // The target has to be in line of sight.
            return LineOfSight(target, checkPosition) != null;
        }

        /// <summary>
        /// Is the specified target within line of sight?
        /// </summary>
        /// <param name="target">The target to determine if it is within sight.</param>
        /// <param name="checkPosition">Should the pathfinding position be checked?</param>
        /// <returns>True if the target is within line of sight.</returns>
        public Transform LineOfSight(Transform target, bool checkPosition)
        {
            return LineOfSight(m_CoverPoint == null ? m_Transform.position : m_CoverPoint.AttackPosition, target, checkPosition);
        }

        /// <summary>
        /// Is the position within within line of sight of hte target?
        /// </summary>
        /// <param name="position">The position to check against.</param>
        /// <param name="target">The target to determine if it is within sight.</param>
        /// <param name="checkPosition">Should the pathfinding position be checked?</param>
        /// <returns>True if the target is within line of sight.</returns>
        public Transform LineOfSight(Vector3 position, Transform target, bool checkPosition)
        {
            if (position == target.transform.position) {
                return target;
            }
            // Temporarily set the character to a layer not being checked. This will prevent the line of sight from hitting the character.
            var prevLayer = m_GameObject.layer;
            m_GameObject.layer = LayerManager.IgnoreRaycast;
            var hitTarget = false;
            RaycastHit hit;
            var bonePosition = GetTargetBoneTransform(target).position;
            NavMeshHit m_NavMeshHit;
            // Set the local z offset to 0 to prevent the line of sight check from intersecting with a wall immediately in front of the character.
            var offset = Vector3.zero;
            if (m_CoverPoint == null) {
                offset = m_Transform.InverseTransformPoint(m_LookTransform.position);
                offset.z = 0;
                offset = (m_Transform.TransformPoint(offset) - m_Transform.position);
            }
            // Sample a pathfinding position to ensure the position will be valid. Do this instead of immediately firing the raycast because the position
            // may be in an invalid pathfinding location.
            if (checkPosition) {
                if (NavMesh.SamplePosition(position, out m_NavMeshHit, m_CapsuleCollider.height * 2, NavMesh.AllAreas)) {
                    position = m_NavMeshHit.position;
                    if (Physics.Linecast(position + offset, bonePosition, out hit, ~m_IgnoreLayerMask)) {
                        hitTarget = hit.transform.IsChildOf(target);
                    } else {
                        hitTarget = true;
                    }
                    Debug.DrawLine((position + offset), bonePosition, hitTarget ? Color.green : Color.red);
                }
            } else {
                if (Physics.Linecast(position + offset, bonePosition, out hit, ~m_IgnoreLayerMask)) {
                    hitTarget = hit.transform.IsChildOf(target);
                } else {
                    hitTarget = true;
                }
                Debug.DrawLine((position + offset), bonePosition, hitTarget ? Color.green : Color.red);
            }
            m_GameObject.layer = prevLayer;
            return hitTarget ? target : null;
        }

        /// <summary>
        /// Returns the bone transform for the specified Transform.
        /// </summary>
        /// <param name="target">The Transform to return the bone transform of.</param>
        /// <returns>The bone transform for the specified Transform. If the Transform does not have an Animator the Transform will be returned.</returns>
        public Transform GetTargetBoneTransform(Transform target)
        {
            Transform bone;
            if (!m_TransformBoneMap.TryGetValue(target, out bone)) {
                var targetAnimator = target.GetComponentInParent<Animator>();
                if (targetAnimator != null) {
                    bone = targetAnimator.GetBoneTransform(m_TargetBone);
                    // The bone may be null for generic characters.
                    if (bone == null) {
                        bone = target;
                    }
                    m_TransformBoneMap.Add(target, bone);
                } else {
                    // If no Animator exists then the current target is considered the target bone.
                    m_TransformBoneMap.Add(target, target);
                    return target;
                }
            }
            return bone;
        }

        /// <summary>
        /// Returns the WeaponStat for the specified ItemType.
        /// </summary>
        /// <param name="itemType">The ItemType to get the WeaponStat of.</param>
        /// <returns>The WeaponStat for the specified ItemType.</returns>
        public WeaponStat WeaponStatForItemType(ItemType itemType)
        {
            return m_AvailableWeaponMap[itemType];
        }

        /// <summary>
        /// Returns the WeaponStat for the specified WeaponClass.
        /// </summary>
        /// <param name="weaponClass">The WeaponClass to get the WeaponStat of.</param>
        /// <returns>The WeaponStat for the specified WeaponClass.</returns>
        public WeaponStat WeaponStatForClass(WeaponStat.WeaponClass weaponClass)
        {
            for (int i = 0; i < m_AvailableWeaponMap.Count; ++i) {
                if (m_AvailableWeapons[i].Class == weaponClass) {
                    return m_AvailableWeapons[i];
                }
            }
            return null;
        }
    }
}