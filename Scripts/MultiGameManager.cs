using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiGameManager : MonoBehaviour {
	public int n;
	public int numberOfGames;
	private int currentGameNumber;

	private EvolutionInfoManager evoManager;
	private EvolutionController evolver;

	// Use this for initialization
	void Start () {
		evolver = GameObject.Find("Evolution Controller").GetComponent<EvolutionController>();
		evoManager = new EvolutionInfoManager();
		evoManager.ReadInfo();
		if (evoManager.Initial()) {
			evoManager.Population = evolver.InitializeRandomPopulation(evoManager.N);
			evoManager.InitializePopulationQueue();
			// Select 8 phenotypes and start a game with them
		} else if (evoManager.SelectBest()) {
			evoManager.ReadPopulation("population.csv");
			List<PhenoType> best = Selection();
			evoManager.Population = best;
			evoManager.WritePopulation("prev-population.csv");
			evoManager.ChildPopulation = GenerateOffspring();
			evoManager.Population = evoManager.ChildPopulation;
			evoManager.InitializePopulationQueue();
			

		} else if (evoManager.EvaluateOffspring()) {
			evoManager.ReadPopulation("population.csv");
			evoManager.WritePopulation("prev-population.csv");
			evoManager.ChildPopulation = GenerateOffspring();
			evoManager.Population = evoManager.ChildPopulation;
			evoManager.InitializePopulationQueue();
		} else {
			evoManager.ReadPopulation("population.csv");
			evoManager.ReadPopulationQueue();
		}
		PlayGame();
	}

	List<PhenoType> Selection() {
		List<PhenoType> best = evoManager.Population;
		evoManager.ReadPopulation("prev-population.csv");
		foreach (PhenoType player in evoManager.Population) {
			best.Add(player);
		}
		best.Sort();
		best.Reverse();
		for (int i = best.Count / 2; i < best.Count; i++ ) {
			best.RemoveAt(i);
		}

		for (int i = 0; i < best.Count; i++) {
			best[i].Id = i;
		}

		return best;	

	}

	void PlayGame () {
		PhenoType[] players = new PhenoType[8];
		for (int i = 0; i < 8; i++) {
			int ind = Random.Range(0, evoManager.PopulationQueue.Count);
			players[i] = evoManager.PopulationQueue[ind];
			evoManager.PopulationQueue.RemoveAt(ind);
		}
		evolver.SetPlayers(players);
	}

	List<PhenoType> GenerateOffspring () {
		List<PhenoType> offspring = new List<PhenoType>();
		List<PhenoType> populationBuffer = evoManager.Population;
		int id = 0;

		for (int i = 0; i < evoManager.N / 2; i++) {

			int ind1 = Random.Range(0, populationBuffer.Count);
			PhenoType p1 = populationBuffer[ind1];
			populationBuffer.RemoveAt(ind1);

			int ind2 = Random.Range(0, populationBuffer.Count);
			PhenoType p2 = populationBuffer[ind2];
			populationBuffer.RemoveAt(ind2);

			PhenoType[] children = evolver.HUXCrossover(p1, p2);
			children[0].Id = id;
			id++;
			children[1].Id = id;
			id++;
			offspring.Add(children[0]);
			offspring.Add(children[1]);
		}

		return offspring;

	}

	void UpdateFitness() {
		PhenoType[] players = evolver.GetPlayers();
		foreach (PhenoType player in players) {
			evoManager.UpdateFitness(player);
		}
	}

	private bool youCalled = false;
	public void EndGame() {
		if (youCalled) {
			return;
		} else {
			youCalled = true;
		}
		// called at end of the match
		// Update fitness for each member of the population that was just in the game
		StartCoroutine("DelayedRestart");
		// Update game number and iteration number

	}

	private IEnumerator DelayedRestart() {
		evolver.GetScores();
		yield return new WaitForSeconds(0.5f);
		UpdateFitness();
		yield return new WaitForSeconds(0.5f);
		evoManager.WritePopulation("population.csv");
		yield return new WaitForSeconds(0.5f);
		evoManager.WritePopulationQueue();
		yield return new WaitForSeconds(0.5f);
		evoManager.EndGame();
		yield return new WaitForSeconds(0.5f);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
	}
	
}
