using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Opsive.ThirdPersonController;
using Opsive.DeathmatchAIKit;
using System;
using System.IO;
using System.Linq;

public class EvolutionController : MonoBehaviour {

	private Scoreboard scoreboard;
	public int numPlayers = 8;
    private Dictionary<string,PhenoType> playerRegistry;
    protected PhenoType[] m_FFAPlayers;


	// Set the chromosomes for each player and express them as parameters
	public void SetPlayers(PhenoType[] players) {
		m_FFAPlayers = new PhenoType[numPlayers];
		for (int i = 0; i < numPlayers; i++) {
			m_FFAPlayers[i] = new PhenoType();
		} 

        // Initialize dictionary to keep track of player genes
        playerRegistry = new Dictionary<string,PhenoType>();
        GetScores();
        for (int i = 0; i < numPlayers; i++) {
            playerRegistry.Add(m_FFAPlayers[i].Name, m_FFAPlayers[i]);
        }
		for (int i = 0; i < 8; i++) {
			m_FFAPlayers[i].Chromosome = players[i].Chromosome;
			m_FFAPlayers[i].Id = players[i].Id;
			m_FFAPlayers[i].PostCrossoverUpdate();
		}
	}

	public PhenoType[] GetPlayers() {
		return m_FFAPlayers;
	}

	// Update the scores of each player by looking at the scoreboard
	public void GetScores () {
		if (scoreboard == null) {
			scoreboard = Scoreboard.Instance;
		}
        var teamStats = scoreboard.SortedStats;
        for (int i = 0; i < teamStats.Count; ++i) {
            var playerCount = i;

            // Update the individual player scores. 
            var playerStats = teamStats[i].PlayerStats;
        	    m_FFAPlayers[playerCount].Name = playerStats[0].Player.name;
        	    m_FFAPlayers[playerCount].Kills = playerStats[0].Kills;
        	    m_FFAPlayers[playerCount].Deaths = playerStats[0].Deaths;
        	    m_FFAPlayers[playerCount].UpdateFitness();
        }
	}

	// get phenotype of player with specified name
    public PhenoType GetPhenoType(string name) {
        return playerRegistry[name];
    }

	public List<PhenoType> InitializeRandomPopulation(int populationSize) {
		List<PhenoType> population = new List<PhenoType>();
		population.Capacity = populationSize;
		for (int i = 0; i < populationSize; i++) {
			PhenoType newPheno = new PhenoType();
			newPheno.Id = i;
			population.Add(newPheno);
		}
		return population;
	}

	// Write info on winners to files.
	// phenotypes.csv is necessary for next game to generate offspring from current game
	public void ReportWinner() {
		// Add current players to population
		GetScores();
		List<PhenoType> population = m_FFAPlayers.ToList();
		foreach (PhenoType player in population) {
			player.UpdateFitness();
		}
		population.Sort();
		PhenoType winner = population[7];

		// Add players from previous game to population
		if (File.Exists("phenotypes.csv")) {
			string[] phenos = System.IO.File.ReadAllLines("phenotypes.csv");
			for (int i = 0; i < phenos.Length; i++) {
				population.Add(StringToPhenoType(phenos[i]));
			}
			population.Sort();


		}
		// Sorted population in descending order
		population.Reverse();

		// Write stats on winners to file
		string[] lines = new string[8+numPlayers];
		lines[0] = "------- End Game Report -------";
		lines[1] = "Winner:";
		lines[2] = winner.Name;
		lines[3] = winner.Kills.ToString();
		lines[4] = winner.Deaths.ToString();
		lines[5] = "Winner's Parameters";
		for (int i = 6; i < 12; i++) {
			lines[i] = winner.ParameterNames[i-6] + ": " + winner.Parameters[i-6].ToString();	
		}
		using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter("endgame-report.csv", true))
        {
            foreach (string line in lines)
            {
                file.WriteLine(line);
            }
        }

		// Write n (8) best phenotypes to file, which will produce next game's population
		string[] pheno = new string[8];
		for (int k = 0; k < 8; k++) {
			string phenobuf = "";
			for (int i = 0; i < 6; i++) {
				for (int j = i * 8; j < (i * 8) + 8; j++) {
					phenobuf += population[k].Chromosome[j].ToString();  
				}
				phenobuf += ", ";
			}
			m_FFAPlayers[k].UpdateFitness();
			phenobuf += m_FFAPlayers[k].Fitness.ToString();
			pheno[k] = phenobuf;
		}
		using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter("phenotypes.csv"))
        {
            foreach (string line in pheno)
            {
                file.WriteLine(line);
            }
        }

		// Keep track of winning phenotypes across multiple games
		using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter("phenotypes-history.csv", true))
        {
            foreach (string line in pheno)
            {
                file.WriteLine(line);
            }
        }

	}

	public void InitializePhenotypes() {
		// acquire phenotypes from file
		string[] pheno = System.IO.File.ReadAllLines("phenotypes.csv");
		PhenoType p1 = StringToPhenoType(pheno[0]);
		PhenoType p2 = StringToPhenoType(pheno[1]);

		bool flip = true;
		int crossoverIndex = UnityEngine.Random.Range(0,6 * 8);
		foreach (PhenoType p in m_FFAPlayers) {
			if (flip) {
				p.Crossover(p1, p2, crossoverIndex);
			} else {
				p.Crossover(p2, p1, crossoverIndex);
				crossoverIndex = UnityEngine.Random.Range(0,6 * 8);
			}
			flip = !flip;
		}

	}

	public void CHCInitializePhenotypes() {
		// Get population from previous game from file
		string[] pheno = System.IO.File.ReadAllLines("phenotypes.csv");
		List<PhenoType> population = new List<PhenoType>();
		for (int i = 0; i < pheno.Length; i++) {
			population.Add(StringToPhenoType(pheno[i]));
		}

		// Select from population without replacement
		for (int i = 0; i < population.Count / 2; i++) {
			int ind1 = UnityEngine.Random.Range(0, population.Count);
			int ind2 = UnityEngine.Random.Range(0, population.Count);
			while (ind1 == ind2) {
				ind1 = UnityEngine.Random.Range(0, population.Count);
				ind2 = UnityEngine.Random.Range(0, population.Count);
			}
			PhenoType[] children = HUXCrossover(population[ind1], population[ind2]);
			m_FFAPlayers[i].Chromosome = children[0].Chromosome;
			m_FFAPlayers[i].PostCrossoverUpdate();
			m_FFAPlayers[i + 4].Chromosome = children[1].Chromosome;
			m_FFAPlayers[i + 4].PostCrossoverUpdate();
		}

	}

    private PhenoType[] HUXCrossover(PhenoType p1, PhenoType p2) {
		PhenoType[] children = new PhenoType[2];
		children[0] = new PhenoType();
		children[1] = new PhenoType();
		children[0].Chromosome = p1.Chromosome;
		children[1].Chromosome = p2.Chromosome;
        for (int i = 0; i < p1.Chromosome.Length; i++) {
			// exchange half of non-matching bits
            if (p1.Chromosome[i] != p2.Chromosome[i] && UnityEngine.Random.value < 0.5f) {
					children[0].Chromosome[i] = p2.Chromosome[i];
					children[1].Chromosome[i] = p1.Chromosome[i];
            }

        }
		return children;
		
    }

	// returns the number of non-matching bits between 2 phenotypes
	private int HammingDistance(PhenoType p1, PhenoType p2) {
		int count = 0;
		for (int i = 0; i < p1.Chromosome.Length; i++) {
			if (p1.Chromosome[i] != p2.Chromosome[i]) {
				count += 1;
			}
		}
		return count;
	}

	// Interprets a string in the form "11100101, 10011011, ..." to initialize a phenotype
	// with that chromosome
	private PhenoType StringToPhenoType(string pheno) {
		PhenoType p = new PhenoType();
		string[] separators = new string[] {", "};
		string[] result = pheno.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
		int[][] chrom = new int[6][];
		for (int i = 0; i < 6; i++) {
			chrom[i] = Array.ConvertAll(result[i].ToCharArray(), c => (int)Char.GetNumericValue(c));
		}
		p.Chromosome = PhenoType.JoinGenes(chrom, 8);
		
		return p;
	}

	public void RegisterDamage(string attacker, float amount) {
		for (int i = 0; i < m_FFAPlayers.Length; i++) {
			if (m_FFAPlayers[i].Name == attacker) {
				m_FFAPlayers[i].DamageDone += amount;
			}
		}
	}


}