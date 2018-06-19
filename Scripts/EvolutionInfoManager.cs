using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionInfoManager {
	protected int m_CurrentGameNumber;
	protected int m_CurrentIteration;
	protected int m_HammingDistanceDelta;
	protected int m_NumberOfParameters;
	protected int m_N;
	protected List<PhenoType> m_Population;
	protected List<PhenoType> m_ChildPopulation;
	protected List<PhenoType> m_PopulationQueue;

	protected int m_GeneLength;

    public List<PhenoType> Population { get { return m_Population; } set { m_Population = value; } }
    public List<PhenoType> PopulationQueue { get { return m_PopulationQueue; } set { m_PopulationQueue = value; } }
	public int N { get { return m_N; } set {m_N = value;}}

	public EvolutionInfoManager() {

	}

	public void EndGame() {
		m_CurrentGameNumber += 1;
		WriteInfo();
		if (m_CurrentGameNumber >= 8) {
		    #if UNITY_EDITOR
         	UnityEditor.EditorApplication.isPlaying = false;
			#else 
			Application.Quit()
			#endif
		}
	}

	public void ReadInfo() {
		string[] info = System.IO.File.ReadAllLines("evolution.csv");
		int.TryParse(info[0], out m_CurrentGameNumber);
		int.TryParse(info[1], out m_CurrentIteration);
		int.TryParse(info[2], out m_HammingDistanceDelta);
		int.TryParse(info[3], out m_NumberOfParameters);
		int.TryParse(info[4], out m_N);
		int.TryParse(info[5], out m_GeneLength);
	}

	public void WriteInfo() {
		string[] info = new string[6];
		info[0] = m_CurrentGameNumber.ToString();
		info[1] = m_CurrentIteration.ToString();
		info[2] = m_HammingDistanceDelta.ToString();
		info[3] = m_NumberOfParameters.ToString();
		info[4] = m_N.ToString();
		info[5] = m_GeneLength.ToString();
		System.IO.File.WriteAllLines("evolution.csv", info);
	}

	public void ReadPopulation() {
		string[] pop = System.IO.File.ReadAllLines("population.csv");
		m_Population = new List<PhenoType>();
		foreach (String pheno in pop) {
			m_Population.Add(StringToPhenoType(pheno));
		}

	}

	public void ReadPopulationQueue() {
		string[] pop = System.IO.File.ReadAllLines("population-queue.csv");
		m_PopulationQueue = new List<PhenoType>();
		foreach (String pheno in pop) {
			m_PopulationQueue.Add(StringToPhenoType(pheno));
		}
	}

	public void WritePopulation() {
		string[] pop = new string[m_N];
		for (int k = 0; k < m_N; k++) {
			string phenobuf = "";
			for (int i = 0; i < 6; i++) {
				for (int j = i * 8; j < (i * 8) + 8; j++) {
					phenobuf += m_Population[k].Chromosome[j].ToString();  
				}
				phenobuf += ", ";
			}
			phenobuf += m_Population[k].Fitness.ToString();
			phenobuf += ", ";
			phenobuf += m_Population[k].Id.ToString();
			pop[k] = phenobuf;
		}
		using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter("population.csv"))
        {
            foreach (string line in pop)
            {
                file.WriteLine(line);
            }
        }
	}

	public void WritePopulationQueue() {
		int count = m_PopulationQueue.Count;
		string[] pop = new string[count];
		for (int k = 0; k < count; k++) {
			string phenobuf = "";
			for (int i = 0; i < 6; i++) {
				for (int j = i * 8; j < (i * 8) + 8; j++) {
					phenobuf += m_PopulationQueue[k].Chromosome[j].ToString();  
				}
				phenobuf += ", ";
			}
			phenobuf += m_PopulationQueue[k].Fitness.ToString();
			phenobuf += ", ";
			phenobuf += m_PopulationQueue[k].Id.ToString();
			pop[k] = phenobuf;
		}
		using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter("population-queue.csv"))
        {
            foreach (string line in pop)
            {
                file.WriteLine(line);
            }
        }
	}

	// Interprets a string in the form "11100101, 10011011, ..." to initialize a phenotype
	// with that chromosome
	private PhenoType StringToPhenoType(string pheno) {
		PhenoType p = new PhenoType();
		string[] separators = new string[] {", "};
		string[] result = pheno.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
		int[][] chrom = new int[m_NumberOfParameters][];
		// Set chromosome
		for (int i = 0; i < m_NumberOfParameters; i++) {
			chrom[i] = Array.ConvertAll(result[i].ToCharArray(), c => (int)Char.GetNumericValue(c));
		}

		// Update parameters to express chromosome
		p.Chromosome = PhenoType.JoinGenes(chrom, m_GeneLength);
		p.PostCrossoverUpdate();

		// Set Fitness
		float fit = 0f;
		float.TryParse(result[m_NumberOfParameters], out fit);
		p.Fitness = fit;

		// Set Id
		int id = 0;
		int.TryParse(result[m_NumberOfParameters + 1], out id);
		p.Id = id;
		
		return p;
	}

	public bool Initial() {
		return (m_CurrentGameNumber == 1 && m_CurrentIteration == 1);
	}

	public void UpdateFitness(PhenoType player) {
		for (int i = 0; i < m_N; i++) {
			if (m_Population[i].Id == player.Id) {
				m_Population[i].Fitness = player.Fitness;
			}
		}
	}

	public void InitializePopulationQueue() {
		m_PopulationQueue = new List<PhenoType>();
		foreach (PhenoType player in m_Population) {
			m_PopulationQueue.Add(player);
		}
	}




}
