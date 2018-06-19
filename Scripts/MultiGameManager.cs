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
		} else {
			evoManager.ReadPopulation();
			evoManager.ReadPopulationQueue();
		}
		PlayGame();
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

	void UpdateFitness() {
		PhenoType[] players = evolver.GetPlayers();
		Debug.Log(players.Length);
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
		evoManager.WritePopulation();
		yield return new WaitForSeconds(0.5f);
		evoManager.WritePopulationQueue();
		yield return new WaitForSeconds(0.5f);
		evoManager.EndGame();
		yield return new WaitForSeconds(0.5f);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
	}
	
}
