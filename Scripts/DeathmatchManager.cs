using UnityEngine;
#if !(UNITY_5_1 || UNITY_5_2)
using UnityEngine.SceneManagement;
#endif
using Opsive.ThirdPersonController;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using Opsive.DeathmatchAIKit.AI;

namespace Opsive.DeathmatchAIKit
{
    /// <summary>
    /// The DeathmatchManager sets up the deathmatch game and acts as a coordinator between the various components.
    /// </summary>
    public class DeathmatchManager : MonoBehaviour
    {
        // Static variables
        private static DeathmatchManager s_Instance;
        private static DeathmatchManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!m_Initialized) {
                    s_Instance = new GameObject("Deathmatch Manager").AddComponent<DeathmatchManager>();
                }
#endif
                return s_Instance;
            }
        }

        // Constant values
        private const int c_MaxPlayerCount = 8;
        private const int c_MaxTeamPlayerCount = 4;
        private const int c_MaxTeamCount = 4;

        private MultiGameManager gameManager;

        public enum AIDifficulty { Easy, Medium, Hard }

        [Tooltip("Should the Deathmatch Manager force initialization upon start? The necessary objects should already be instantiated - useful for debugging")]
        [SerializeField] protected bool m_ForceInitialization;
        [Tooltip("Specifies the number of players a FFA deathmatch game")]
        [SerializeField] protected int m_PlayerCount = 8;
        [Tooltip("Specifies the number of players on a team in the deathmatch game")]
        [SerializeField] protected int m_PlayersPerTeam = 4;
        [Tooltip("Specifies the number of teams in the deathmatch game")]
        [SerializeField] protected int m_TeamCount = 2;
        [Tooltip("Should a team deathmath game be started?")]
        [SerializeField] protected bool m_TeamGame;
        [Tooltip("Should the observer mode be started? Player will not spawn but merely observe")]
        [SerializeField] protected bool m_ObserverMode;
        [Tooltip("Specifies how hard the AI agents are to beat")]
        [SerializeField] protected AIDifficulty m_Difficulty = AIDifficulty.Medium;
        [Tooltip("A reference to the player controlled character")]
        [SerializeField] protected GameObject m_PlayerPrefab;
        [Tooltip("A reference to the AI controlled character")]
        [SerializeField] protected GameObject m_AgentPrefab;
        [Tooltip("A reference to the camera")]
        [SerializeField] protected GameObject m_CameraPrefab;
        [Tooltip("A reference to the observer")]
        [SerializeField] protected GameObject m_ObserverPrefab;
        [Tooltip("The names to use within in the deathmatch game")]
        [SerializeField] protected string[] m_AgentNames;
        [Tooltip("The team names to use within in the deathmatch game")]
        [SerializeField] protected string[] m_TeamNames;
        [Tooltip("The name of the object to parent the players to")]
        [SerializeField] protected string m_ParentName = "Players";
        [Tooltip("The primary player colors to use when in a FFA game")]
        [SerializeField] protected Color[] m_PrimaryFFAColors;
        [Tooltip("The secondary player colors to use when in a FFA game")]
        [SerializeField] protected Color[] m_SecondaryFFAColors;
        [Tooltip("The primary player colors to use for the when in a team game")]
        [SerializeField] protected Color[] m_PrimaryTeamColors;
        [Tooltip("The secondary player colors to use for the when in a team game")]
        [SerializeField] protected Color[] m_SecondaryTeamColors;
        [Tooltip("A reference to the materials that should use the primary color")]
        [SerializeField] protected Material[] m_PrimaryColorMaterials;
        [Tooltip("A reference to the materials that should use the secondary color")]
        [SerializeField] protected Material[] m_SecondaryColorMaterials;
        [Tooltip("The LayerMask name to use for a FFA and team game")]
        [SerializeField] protected string[] m_Layers;
        [Tooltip("The behavior tree to use when in a FFA game")]
        [SerializeField] protected ExternalBehavior m_SoloTree;
        [Tooltip("The behavior tree to use when in a team game")]
        [SerializeField] protected ExternalBehavior m_TeamTree;
        // Internal variables
#if UNITY_EDITOR
        private static bool m_Initialized;
#endif
        private string m_PlayerName = "Aydan";
        private bool m_Paused;

        // Component references
        private Transform m_PlayerParent;
        private CoverPoint[] m_CoverPoints;
        private TeamManager m_TeamManager;
        private GameObject m_LocalPlayer;

        // Exposed properties
        public static bool IsInstantiated { get { return s_Instance != null; } }
        public static int PlayerCount { set { Instance.PlayerCountInternal = value; } get { return Instance.PlayerCountInternal; } }
        private int PlayerCountInternal { set { m_PlayerCount = Mathf.Clamp(value, 2, c_MaxPlayerCount); } get { return m_PlayerCount; } }
        private static int MaxPlayerCount { get { return c_MaxPlayerCount; } }
        public static int PlayersPerTeam { set { Instance.PlayersPerTeamInternal = value; } get { return Instance.PlayersPerTeamInternal; } }
        private int PlayersPerTeamInternal { set { m_PlayersPerTeam = Mathf.Clamp(value, 2, c_MaxTeamPlayerCount); } get { return m_PlayersPerTeam; } }
        private static int MaxTeamPlayerCount { get { return c_MaxTeamPlayerCount; } }
        public static int TeamCount { set { Instance.TeamCountInternal = value; } get { return Instance.TeamCountInternal; } }
        private int TeamCountInternal { set { m_TeamCount = Mathf.Clamp(value, 2, c_MaxTeamCount); } get { return m_TeamCount; } }
        private static int MaxTeamCount { get { return c_MaxTeamCount; } }
        public static string PlayerName { set { Instance.PlayerNameInternal = value; } }
        private string PlayerNameInternal { set { m_PlayerName = value; } }
        public static string[] TeamNames { get { return Instance.TeamNamesInternal; } }
        private string[] TeamNamesInternal { get { return m_TeamNames; } }
        public static bool TeamGame { get { return Instance.TeamGameInternal; } set { Instance.TeamGameInternal = value; } }
        private bool TeamGameInternal { get { return m_TeamGame; } set { m_TeamGame = value; } }
        public static bool ObserverMode { get { return Instance.ObserverModeInternal; } set { Instance.ObserverModeInternal = value; } }
        private bool ObserverModeInternal { get { return m_ObserverMode; } set { m_ObserverMode = value; } }
        public static AIDifficulty Difficulty { get { return Instance.DifficultyInternal; } set { Instance.DifficultyInternal = value; } }
        private AIDifficulty DifficultyInternal { get { return m_Difficulty; } set { m_Difficulty = value; } }
        public static Color[] PrimaryFFAColors { get { return Instance.PrimaryFFAColorsInternal; } }
        private Color[] PrimaryFFAColorsInternal { get { return m_PrimaryFFAColors; } }
        public static Color[] PrimaryTeamColors { get { return Instance.PrimaryTeamColorsInternal; } }
        private Color[] PrimaryTeamColorsInternal { get { return m_PrimaryTeamColors; } }
        public static bool Paused { set { Instance.PausedInternal = value;  } }
        private bool PausedInternal
        {
            set
            {
                // Don't pause if the game is over.
                if (m_LocalPlayer == null) {
                    return;
                }
                m_Paused = value;
                Time.timeScale = m_Paused ? 0 : 1;
                EventHandler.ExecuteEvent("OnPauseGame", m_Paused);
                EventHandler.ExecuteEvent<bool>("OnShowUI", !m_Paused);
                BehaviorManager.instance.enabled = !m_Paused;
            }
        }
        public static CoverPoint[] CoverPoints { get { return Instance.CoverPointsInternal; } }
        private CoverPoint[] CoverPointsInternal { get { return m_CoverPoints; } }
        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Awake()
        {
            s_Instance = this;
#if UNITY_EDITOR
            m_Initialized = true;
#endif
            DontDestroyOnLoad(gameObject);
            enabled = m_ForceInitialization;

#if !(UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            SceneManager.sceneLoaded += SceneLoaded;
#endif

            EventHandler.RegisterEvent<bool>("OnGameOver", GameOver);
        }

        /// <summary>
        /// Performs sanity checks and calls the SceneLoaded callback if the manager should be force initialized.
        /// </summary>
        private void Start()
        {
            // Ensure the arrays are the correct size.
            if (m_TeamGame) {
                if (m_PrimaryTeamColors.Length < m_TeamCount || m_SecondaryTeamColors.Length < m_TeamCount) {
                    Debug.LogError("Error: The team count is greater than the number of colors available.");
                }
                if (m_Layers.Length < m_TeamCount) {
                    Debug.LogError("Error: The team count is greater than the number of layers available.");
                }
            } else {
                if (m_PrimaryFFAColors.Length < m_PlayerCount || m_SecondaryFFAColors.Length < m_PlayerCount) {
                    Debug.LogError("Error: The player count is greater than the number of colors available.");
                }
            }

            if (m_ForceInitialization) {
                SceneLoaded(1);
            }
        }

        /// <summary>
        /// Pause the game if the escape key is pressed.
        /// </summary>
        private void Update()
        {
            // Pause the game when escape is pressed.
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Paused = true;
            }
        }

#if !(UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
        /// <summary>
        /// Callback when a new scene is loaded starting with Unity 5.4. 
        /// </summary>
        /// <param name="scene">The scene that was loaded.</param>
        /// <param name="mode">Specifies how the scene was loaded.</param>
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneLoaded(scene.buildIndex);
        }
#else
        /// <summary>
        /// Callback when a new level is loaded prior to Unity 5.4.
        /// </summary>
        /// <param name="level">The index of the loaded level.</param>
        private void OnLevelWasLoaded(int level)
        {
            SceneLoaded(level);
        }
#endif

        /// <summary>
        /// A new scene was loaded. Spawn the players if the level isn't the main menu.
        /// </summary>
        /// <param name="sceneIndex">The index of the loaded scene.</param>
        public void SceneLoaded(int sceneIndex)
        {
            // No action for the main menu scene.
            if (sceneIndex == 0) {
                enabled = false;
                return;
            }

            // Find the parent object of all of the players.
            var parentGameObject = GameObject.Find(m_ParentName);
            if (parentGameObject != null) {
                m_PlayerParent = parentGameObject.transform;
            }

            // Randomly choose a spawn location. If another character is within the radius of that spawn location then determine a new spawn location.
            var spawnPoints = DeathmatchSpawnSelection.GetAllSpawnLocations(m_TeamGame, 0);
            var availableLocations = ThirdPersonController.ObjectPool.Get<List<Transform>>();
            availableLocations.Clear();
            for (int i = 0; i < spawnPoints.Length; ++i) {
                availableLocations.Add(spawnPoints[i]);
            }

            // Reset the player list.
            Scoreboard.Reinitialize();

            // AI Agents need to know where to take cover.
            m_CoverPoints = GameObject.FindObjectsOfType<CoverPoint>();

            // Add the players to the scoreboard and return early if the component is being forced initialized. The necessary objects should already be instantiated.
            if (m_ForceInitialization) {
                m_LocalPlayer = GameObject.FindObjectOfType<ThirdPersonController.Input.UnityInput>().gameObject;
                Scoreboard.AddPlayer(m_LocalPlayer, 0);
                var agents = GameObject.FindObjectsOfType<BehaviorTree>();
                for (int i = 0; i < agents.Length; ++i) {
                    Scoreboard.AddPlayer(agents[i].gameObject, i + 1);
                }
                return;
            }

            // Add the local player/camera. Wait to attach the character until after all of the scripts have been created. If in observer mode then only spawn the
            // observer prefab.
            GameObject camera = null;
            if (m_ObserverMode) {
                m_LocalPlayer = GameObject.Instantiate(m_ObserverPrefab) as GameObject;
            } else {
                m_LocalPlayer = InstantiatePlayer(availableLocations, m_PlayerPrefab, m_PlayerName, 0);
                camera = GameObject.Instantiate(m_CameraPrefab) as GameObject;
            }

            gameManager = GameObject.Find("MultiGame Manager").GetComponent<MultiGameManager>();

            // Spawn the AI agents.
            var startIndex = m_ObserverMode ? 0 : 1; // Start at index 1 in non-observer mode since the player will be on the first team.
            if (m_TeamGame) {
                // Create the TeamManager.
                m_TeamManager = gameObject.AddComponent<TeamManager>();

                // The local player should target players on the other team.
                var enemyLayer = 0;
                for (int i = 1; i < m_TeamCount; ++i) {
                    enemyLayer |= 1 << LayerMask.NameToLayer(m_Layers[i]);
                }
                if (!m_ObserverMode) {
                    // Setup the local player on the first team.
                    SetupPlayer(m_LocalPlayer, 0, 0, LayerMask.NameToLayer(m_Layers[0]), enemyLayer, m_PrimaryTeamColors[0], m_SecondaryTeamColors[0]);
                    TeamManager.AddTeamMember(m_LocalPlayer, 0);

                    // The crosshairs should target the enemy layer.
                    var crosshairsMonitor = GameObject.FindObjectOfType<UI.DeathmatchCrosshairsMonitor>();
                    crosshairsMonitor.CrosshairsTargetLayer = enemyLayer;
                }

                // Setup the team AI agents.
                for (int i = 0; i < m_TeamCount; ++i) {
                    // A new set of spawn points need to be returned for any subsequent teams.
                    if (i > 0) {
                        spawnPoints = DeathmatchSpawnSelection.GetAllSpawnLocations(m_TeamGame, i);
                        availableLocations.Clear();
                        for (int j = 0; j < spawnPoints.Length; ++j) {
                            availableLocations.Add(spawnPoints[j]);
                        }
                    }

                    // The AI agent should target players on the other team.
                    var teamLayer = LayerMask.NameToLayer(m_Layers[i]);
                    enemyLayer = 0;
                    for (int j = 0; j < m_TeamCount; ++j) {
                        // Do not allow friendly fire.
                        if (i == j) {
                            continue;
                        }
                        enemyLayer |= 1 << LayerMask.NameToLayer(m_Layers[j]);
                    }

                    // Setup the AI agents.
                    for (int j = startIndex; j < m_PlayersPerTeam; ++j) {
                        var agentNameIndex = m_ObserverMode ? ((i * m_PlayersPerTeam) + j ) : ((i * m_PlayersPerTeam) + j - 1);
                        var aiAgent = InstantiatePlayer(availableLocations, m_AgentPrefab, m_AgentNames[agentNameIndex], i);
                        SetupPlayer(aiAgent, (i * m_PlayersPerTeam) + j, i, teamLayer, enemyLayer, m_PrimaryTeamColors[i], m_SecondaryTeamColors[i]);

                        TeamManager.AddTeamMember(aiAgent, i);
                    }

                    startIndex = 0;
                }
            } else {
                var playerLayer = LayerMask.NameToLayer(m_Layers[0]);
                if (!m_ObserverMode) {
                    // Setup the local player.
                    SetupPlayer(m_LocalPlayer, 0, 0, playerLayer, 1 << playerLayer, m_PrimaryFFAColors[0], m_SecondaryTeamColors[0]);

                    // The crosshairs should target the enemy layer.
                    var crosshairsMonitor = GameObject.FindObjectOfType<UI.DeathmatchCrosshairsMonitor>();
                    crosshairsMonitor.CrosshairsTargetLayer = 1 << playerLayer;
                }
                // Setup the AI agents.
                for (int i = startIndex; i < m_PlayerCount; ++i) {
                    var aiAgent = InstantiatePlayer(availableLocations, m_AgentPrefab, m_AgentNames[i - (m_ObserverMode ? 0 : 1)], i);
                    SetupPlayer(aiAgent, i, i, playerLayer, 1 << playerLayer, m_PrimaryFFAColors[i], m_SecondaryFFAColors[i]);
                }
            }

            ThirdPersonController.ObjectPool.Return(availableLocations);
            EventHandler.ExecuteEvent("OnStartGame");
            // The camera will be null in observer mode.
            if (camera != null) {
                var cameraController = camera.GetComponent<CameraController>();
                cameraController.Character = m_LocalPlayer;
                cameraController.DeathAnchor = Utility.GetComponentForType<Animator>(m_LocalPlayer).GetBoneTransform(HumanBodyBones.Head);
                cameraController.FadeTransform = Utility.GetComponentForType<Animator>(m_LocalPlayer).GetBoneTransform(HumanBodyBones.Chest);
            }
            enabled = true;
        }

        /// <summary>
        /// Instantiates a new deathmatch player.
        /// </summary>
        /// <param name="availableLocations">A list of locations that the agent can spawn at.</param>
        /// <param name="prefab">The prefab that can spawn.</param>
        /// <param name="name">The name of the player.</param>
        /// <param name"teamIndex">The index of the team that the player is on.</param>
        /// <returns>The instantiated object.</returns>
        private GameObject InstantiatePlayer(List<Transform> availableLocations, GameObject prefab, string name, int teamIndex)
        {
            // Determine a unique spawn point. Team games will always start with spawn locations for the specific team so the spawn index can be 0 for consistant player spawning.
            var spawnIndex = m_TeamGame ? 0 : Random.Range(0, availableLocations.Count);
            var spawnPoint = availableLocations[spawnIndex];
            availableLocations.RemoveAt(spawnIndex);

            // Instantiate the player and notify the scoreboard.
            var player = GameObject.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation) as GameObject;
            player.transform.parent = m_PlayerParent;
            player.name = name;
            Scoreboard.AddPlayer(player, teamIndex);
            return player;
        }

        /// <summary>
        /// Setup the player for a deathmatch game.
        /// </summary>
        /// <param name="player">The player to set the layers on.</param>
        /// <param name="playerIndex">The index of the player.</param>
        /// <param name="teamIndex">The index of the team.</param>
        /// <param name="friendlyLayer">The friendly layer.</param>
        /// <param name="enemyLayer">The enemy layer.</param>
        /// <param name="primaryColor">The primary color of the player.</param>
        /// <param name="secondaryColor">The secondary color of the player.</param>
        private void SetupPlayer(GameObject player, int playerIndex, int teamIndex, int friendlyLayer, int enemyLayer, Color primaryColor, Color secondaryColor)
        {
            // The 0th index corresponds to the local player while not in observer mode.
            if (m_ObserverMode || playerIndex != 0) {
                var behaviorTree = player.GetComponent<BehaviorTree>();
                behaviorTree.ExternalBehavior = (m_TeamGame ? m_TeamTree : m_SoloTree);
                var waypoints = behaviorTree.GetVariable("Waypoints") as SharedGameObjectList;
                var spawnLocations = SpawnSelection.GetAllSpawnLocations();
                for (int j = 0; j < spawnLocations.Length; ++j) {
                    waypoints.Value.Add(spawnLocations[j].gameObject);
                }
                var deathmatchAgent = player.GetComponent<DeathmatchAgent>();
                deathmatchAgent.TargetLayerMask = enemyLayer;

                if (teamIndex >= 0) {
                    // The following adjustments are applied to the enemy agents:
                    // Easy: Reduced health and less accuracy.
                    // Medium: Less accuracy.
                    // Hard: No change.
                    DeathmatchShootableWeapon[] shootableWeapons = null;
                    switch (m_Difficulty) {
                        case AIDifficulty.Easy:
                            var health = deathmatchAgent.GetComponent<Health>();
                            health.MaxHealth = health.CurrentHealth * 0.5f;
                            health.SetHealthAmount(health.MaxHealth);
                            shootableWeapons = deathmatchAgent.GetComponentsInChildren<DeathmatchShootableWeapon>();
                            for (int i = 0; i < shootableWeapons.Length; ++i) {
                                shootableWeapons[i].Spread = 0.02f;
                            }
                            break;
                        case AIDifficulty.Medium:
                            shootableWeapons = deathmatchAgent.GetComponentsInChildren<DeathmatchShootableWeapon>();
                            for (int i = 0; i < shootableWeapons.Length; ++i) {
                                shootableWeapons[i].Spread = 0.1f;
                            }
                            break;
                    }
                }
            }

            // Set the material color.
            var renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < renderers.Length; ++i) {
                var materials = renderers[i].materials;
                for (int j = 0; j < materials.Length; ++j) {
                    // Do not compare the material directly because the player may be using an instance material.
                    if (materials[j].name.Contains("Primary")) {
                        materials[j].color = primaryColor;
                    } else if (materials[j].name.Contains("Secondary")) {
                        materials[j].color = secondaryColor;
                    }
                } 
            }

            // Set the layer of the player GameObject.
            player.layer = friendlyLayer;
        }

        /// <summary>
        /// The game has ended.
        /// </summary>
        /// <param name="winner">Did the local player win?</param>
        private void GameOver(bool winner)
        {
            if (!m_ObserverMode) {
                EventHandler.ExecuteEvent(m_LocalPlayer, "OnAllowGameplayInput", false);
            }
            m_LocalPlayer = null;
            var behaviorTrees = Object.FindObjectsOfType<BehaviorTree>();
            for (int i = behaviorTrees.Length - 1; i > -1; --i) {
                behaviorTrees[i].DisableBehavior();
            }
            // Stop the characters from respawning after a delay to allow the spawn events to be created.
            Scheduler.Schedule(0.1f, StopRespawns);
        }

        /// <summary>
        /// Stop all of the characters from respawning.
        /// </summary>
        private void StopRespawns()
        {
            var respawners = Object.FindObjectsOfType<CharacterRespawner>();
            for (int i = 0; i < respawners.Length; ++i) {
                respawners[i].CancelSpawn();
            }
        }

        /// <summary>
        /// The game has ended. Load the main menu.
        /// </summary>
        public static void EndGame()
        {
            Instance.EndGameInternal();
        }

        /// <summary>
        /// Internal method called when the game has ended. Load the main menu.
        /// </summary>
        private void EndGameInternal()
        {
            gameManager.EndGame();
            GameOver(false);
            Time.timeScale = 1;
            if (m_TeamManager != null) {
                Destroy(m_TeamManager);
                m_TeamManager = null;
            }
            // RESTART

            // TODO: Get player stats and update parameters
        }
    }
}