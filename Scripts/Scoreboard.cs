using UnityEngine;
using Opsive.ThirdPersonController;
using System.Collections.Generic;

namespace Opsive.DeathmatchAIKit
{
    /// <summary>
    /// The Scoreboard keeps track of the player stats (such as kill/death) for a deathmatch game.
    /// </summary>
    public class Scoreboard : MonoBehaviour
    {
        // Static variables
        private static Scoreboard s_Instance;
        public static Scoreboard Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!m_Initialized) {
                    s_Instance = new GameObject("Scoreboard").AddComponent<Scoreboard>();
                }
#endif
                return s_Instance;
            }
        }

        [Tooltip("The number of FFA kills required to win")]
        [SerializeField] protected int m_FFAKillsToWin = 10;
        [Tooltip("The number of team kills required to win")]
        [SerializeField] protected int m_TeamKillsToWin = 25;

        /// <summary>
        /// Stores the stats for each team.
        /// </summary>
        public class TeamStats : System.IComparable<TeamStats>
        {
            // Internal variables
            private string m_Name;
            private int m_TeamIndex;
            private int m_Kills;
            private List<PlayerStats> m_PlayerStats = new List<PlayerStats>();
            private Dictionary<GameObject, PlayerStats> m_PlayerStatMap = new Dictionary<GameObject, PlayerStats>();

            // Exposed properties
            public string Name { get { return m_Name; } }
            public int Kills { get { return m_Kills; } }
            public int TeamIndex { get { return m_TeamIndex; } }
            public List<PlayerStats> PlayerStats { get { return m_PlayerStats; } }

            /// <summary>
            /// Initializes the TeamStats object.
            /// </summary>
            /// <param name="name">The name of the team.</param>
            /// <param name="teamIndex">The index of the team.</param>
            public void Initialize(string name, int teamIndex)
            {
                m_Name = name;
                m_TeamIndex = teamIndex;
                m_Kills = 0;
            }

            /// <summary>
            /// Adds the player to the team.
            /// </summary>
            /// <param name="player">The player to add.</param>
            public void AddPlayer(GameObject player)
            {
                var playerStat = ObjectPool.Get<PlayerStats>();
                playerStat.Initailize(player);
                m_PlayerStats.Add(playerStat);
                m_PlayerStatMap.Add(player, playerStat);
            }

            /// <summary>
            /// Compares two Stats in descending order.
            /// </summary>
            /// <param name="other">The Stats to compare to.</param>
            /// <returns>The relative order of the objects being compared.</returns>
            public int CompareTo(TeamStats other)
            {
                return other.Kills.CompareTo(m_Kills);
            }

            /// <summary>
            /// A player on the team got a kill. Update the stats.
            /// </summary>
            /// <param name="player">The player that got a kill.</param>
            public void AddKill(GameObject player)
            {
                m_Kills += 1;

                var playerStat = m_PlayerStatMap[player];
                playerStat.Kills += 1;
            }

            /// <summary>
            /// A player on the team died. Update the stats.
            /// </summary>
            /// <param name="player">The player that died.</param>
            public void AddDeath(GameObject player)
            {
                var playerStat = m_PlayerStatMap[player];
                playerStat.Deaths += 1;
            }

            /// <summary>
            /// Sort the players in descending order according to their kill count.
            /// </summary>
            public void SortPlayers()
            {
                m_PlayerStats.Sort();
            }

            /// <summary>
            /// Reinitialize the game variables to their starting values.
            /// </summary>
            public void Reinitialize()
            {
                for (int i = 0; i < m_PlayerStats.Count; ++i) {
                    ObjectPool.Return(m_PlayerStats[i]);
                }
                m_PlayerStats.Clear();
                m_PlayerStatMap.Clear();
            }
        }

        /// <summary>
        /// Stores the stats for each player.
        /// </summary>
        public class PlayerStats : System.IComparable<PlayerStats>
        {
            // Internal variables
            private int m_Kills;
            private int m_Deaths;

            // Component references
            private GameObject m_Player;

            // Exposed properties
            public GameObject Player { get { return m_Player; } }
            public int Kills { get { return m_Kills; } set { m_Kills = value; } }
            public int Deaths { get { return m_Deaths; } set { m_Deaths = value; } }

            /// <summary>
            /// Initialize the PlayerStats for use.
            /// </summary>
            /// <param name="player">The player to track the stats of.</param>
            public void Initailize(GameObject player)
            {
                m_Player = player;
                m_Kills = m_Deaths = 0;
            }

            /// <summary>
            /// Compares two Stats in descending order.
            /// </summary>
            /// <param name="other">The Stats to compare to.</param>
            /// <returns>The relative order of the objects being compared.</returns>
            public int CompareTo(PlayerStats other)
            {
                // If the kill count is the same then compare the name. This will ensure the ordering is consistent.
                if (m_Kills != other.Kills) {
                    return other.Kills.CompareTo(m_Kills);
                }
                return m_Player.name.CompareTo(other.Player.name);
            }
        }

        // Internal variables
#if UNITY_EDITOR
        private static bool m_Initialized;
#endif
        private List<GameObject> m_Players = new List<GameObject>();
        private Dictionary<GameObject, TeamStats> m_PlayerTeamStatMap = new Dictionary<GameObject, TeamStats>();
        private Dictionary<int, TeamStats> m_TeamIndexStatMap = new Dictionary<int, TeamStats>();
        private List<TeamStats> m_TeamStats = new List<TeamStats>();
        private bool m_GameOver;

        // Exposed properties
        public TeamStats NonLocalPlayerLeader { get { return m_NonLocalPlayerLeader; } }
        public List<TeamStats> SortedStats
        {
            get
            {
                m_TeamStats.Sort();
                // The individual player stats should be sorted as well.
                for (int i = 0; i < m_TeamStats.Count; ++i) { m_TeamStats[i].SortPlayers(); }
                return m_TeamStats;
            }
        }

        // Component references
        private TeamStats m_Leader;
        private TeamStats m_NonLocalPlayerLeader;

        /// <summary>
        /// Assign the static variables.
        /// </summary>
        private void OnEnable()
        {
            s_Instance = this;
#if UNITY_EDITOR
            m_Initialized = true;
#endif
        }

        /// <summary>
        /// Reinitialize the game variables to their starting values.
        /// </summary>
        public static void Reinitialize()
        {
            Instance.ReinitializeInternal();
        }

        /// <summary>
        /// Internal method to reinitialize the game variables to their starting values.
        /// </summary>
        private void ReinitializeInternal()
        {
            m_Players.Clear();
            for (int i = 0; i < m_TeamStats.Count; ++i) {
                m_TeamStats[i].Reinitialize();
                ObjectPool.Return(m_TeamStats[i]);
            }
            m_TeamStats.Clear();
            m_PlayerTeamStatMap.Clear();
            m_TeamIndexStatMap.Clear();
            m_GameOver = false;
        }

        /// <summary>
        /// Add a player to the scoreboard.
        /// </summary>
        /// <param name="player">The player to add.</param>
        /// <param name"teamIndex">The index of the team that the player is on.</param>
        public static void AddPlayer(GameObject player, int teamIndex)
        {
            Instance.AddPlayerInternal(player, teamIndex);
        }

        /// <summary>
        /// Internal method to add a player to the scoreboard.
        /// </summary>
        /// <param name="player">The player to add.</param>
        /// <param name"teamIndex">The index of the team that the player is on.</param>
        private void AddPlayerInternal(GameObject player, int teamIndex)
        {
            TeamStats teamStats;
            if (!m_TeamIndexStatMap.TryGetValue(teamIndex, out teamStats)) {
                teamStats = ObjectPool.Get<TeamStats>();
                // A team game will have the name predetermined. If playing a FFA game then the team name should be the player name (there is only one player per team).
                teamStats.Initialize(DeathmatchManager.TeamGame ? DeathmatchManager.TeamNames[teamIndex] : player.name, teamIndex);
                m_TeamIndexStatMap.Add(teamIndex, teamStats);
                m_TeamStats.Add(teamStats);
                m_Leader = teamStats;
                m_NonLocalPlayerLeader = teamStats;
            }
            teamStats.AddPlayer(player);
            m_Players.Add(player);
            m_PlayerTeamStatMap.Add(player, teamStats);

            EventHandler.ExecuteEvent("OnScoreChange");
        }

        /// <summary>
        /// A player has died. Update the stats.
        /// </summary>
        /// <param name="attacker">The player that did the attacking.</param>
        /// <param name="victim">The player that died.</param>
        public static void ReportDeath(GameObject attacker, GameObject victim)
        {
            Instance.ReportDeathInternal(attacker, victim);
        }

        /// <summary>
        /// A player has died. Internal method to update the stats.
        /// </summary>
        /// <param name="attacker">The player that did the attacking.</param>
        /// <param name="victim">The player that died.</param>
        private void ReportDeathInternal(GameObject attacker, GameObject victim)
        {
            // Don't continue to add to the score if the game is over.
            if (m_GameOver) {
                return;
            }
//            Health health = attacker.GetComponent<Health>();
//            health.CurrentHealth += 50f;
//            if (health.CurrentHealth > 100f) {
//                health.CurrentHealth = 100f;
//            }

            TeamStats teamStats;
            // The attacker gets a kill as long as it wasn't a suicide.
            if (attacker != victim && attacker != null) {
                if (m_PlayerTeamStatMap.TryGetValue(attacker, out teamStats)) {
                    teamStats.AddKill(attacker);

                    // Check for a new leader.
                    if (m_Leader != teamStats) {
                        if (teamStats.Kills > m_Leader.Kills) {
                            m_Leader = teamStats;
                        }
                    }
                    // The local player will always be on team 0.
                    if (teamStats.TeamIndex != 0 && m_NonLocalPlayerLeader != teamStats) {
                        if (teamStats.Kills > m_NonLocalPlayerLeader.Kills) {
                            m_NonLocalPlayerLeader = teamStats;
                        }
                    }
                }
            }

            // The victim gets a death.
            if (m_PlayerTeamStatMap.TryGetValue(victim, out teamStats)) {
                teamStats.AddDeath(victim);
            }

            EventHandler.ExecuteEvent("OnScoreChange");

//            // The leader won if they reach the winning kill count.
//            if (m_Leader.Kills >= (DeathmatchManager.TeamGame ? m_TeamKillsToWin : m_FFAKillsToWin)) {
//                EventHandler.ExecuteEvent<bool>("OnGameOver", m_Leader == m_TeamStats[0]);
//                m_GameOver = true;
//            }
        }

        /// <summary>
        /// Returns the stats for the specified player's team.
        /// </summary>
        /// <param name="player">The player whose states be retrieved.</param>
        /// <returns>The states for the specified player.</returns>
        public TeamStats StatsForPlayer(GameObject player)
        {
            return m_PlayerTeamStatMap[player];
        }

        public static void EndGame() {
            Instance.EndGameInternal();
        }

        private void EndGameInternal() {
            EventHandler.ExecuteEvent<bool>("OnGameOver", m_Leader == m_TeamStats[0]);
            m_GameOver = true;

        }

    }
}