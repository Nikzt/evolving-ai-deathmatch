using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections.Specialized;

public class PhenoType : IComparable
{
    
    protected GameObject m_Parent;
    protected string m_Name;
    protected float m_DamageDone;
    protected int m_Kills;
    protected int m_Deaths;
    protected float[] m_Parameters;
    protected string[] m_ParameterNames;
    protected int[] m_Chromosome;
    protected int[][] m_SplitChromosome;
    protected float m_Fitness;
    protected int m_Id;
	protected Dictionary<string,float> m_geneRegistry;
    

	public PhenoType() {
		// Initialize genes and registry
		m_geneRegistry = new Dictionary<string,float>();
		m_geneRegistry.Add("cover probability", UnityEngine.Random.value);
		m_geneRegistry.Add("ammo distance threshold", UnityEngine.Random.value);
        m_geneRegistry.Add("flee health threshold", UnityEngine.Random.value);
        m_geneRegistry.Add("flee distance threshold", UnityEngine.Random.value);
        m_geneRegistry.Add("find health threshold", UnityEngine.Random.value);
        m_geneRegistry.Add("chase wait time", UnityEngine.Random.value);
        m_Parameters = m_geneRegistry.Values.ToArray();
        m_ParameterNames = m_geneRegistry.Keys.ToArray();
        m_Chromosome = new int[6 * 8];
        m_SplitChromosome = new int[6][];
        m_DamageDone = 0f;
        SetChromosome();
        
	}

    // Exposed properties
    public GameObject Parent { get { return m_Parent; } set { m_Parent = value; } }
    public string Name { get { return m_Name; } set { m_Name = value; } }
    public int Kills { get { return m_Kills; } set { m_Kills = value; } }
    public int Deaths { get { return m_Deaths; } set { m_Deaths = value; } }
    public float[] Parameters {get {return m_Parameters;} set {m_Parameters = value;}}
    public string[] ParameterNames {get {return m_ParameterNames;} set {m_ParameterNames = value;}}
    public int[] Chromosome {get {return m_Chromosome;} set {m_Chromosome = value;}}
    public int[][] SplitChromosome {get {return m_SplitChromosome;} set {m_SplitChromosome = value;}}
    public float DamageDone {get {return m_DamageDone;} set {m_DamageDone = value;}}
    public float Fitness {get {return m_Fitness;} set {m_Fitness = value;}}
    public int Id {get {return m_Id;} set {m_Id = value;}}

    public int CompareTo(object obj) {
        if (obj == null) {
            return 1;
        }
        PhenoType otherPhenoType = obj as PhenoType;
        if (otherPhenoType != null) {
            return this.Fitness.CompareTo(otherPhenoType.Fitness);
        } else {
            throw new ArgumentException("Object is not a PhenoType");
        }
    }

	public void UpdateFitness() {
		m_Fitness = this.Kills - (0.2f * this.Deaths) + (0.01f * this.DamageDone);
	}

	public float GetParameter(string paramName) {
        float param = 0f;
        switch (paramName) {
            case "cover probability":
                param = Parameters[0];
                break;
            case "ammo distance threshold":
                param = Parameters[1];
                break;
            case "flee health threshold":
                param = Parameters[2];
                break;
            case "flee distance threshold":
                param = Parameters[3];
                break;
            case "find health threshold":
                param = Parameters[4];
                break;
            case "chase wait time":
                param = Parameters[5];
                break;
        }
        return param;
	}

    private void SetParameters() {
        for (int i = 0; i < Parameters.Length; i++) {
            Parameters[i] = BinaryToFloat(m_SplitChromosome[i]);
        }
    }

    private void SetChromosome() {
        for (int i = 0; i < Parameters.Length; i++) {
            m_SplitChromosome[i] = FloatToBinary(Parameters[i]);
        }
        m_Chromosome = JoinGenes(m_SplitChromosome, 8);
    }

	private int[] FloatToBinary(float num) {
		int intRange = (int) (num * 255);		
		int[] bin = new int[8];
		for (int i = 7; i >= 0; i--) {
			int pow = (int) Mathf.Pow(2,i);
			if (pow <= intRange) {
				bin[7 - i] = 1;
				intRange -= pow;
			} else {
				bin[7 - i] = 0;
			}
		}
		return bin;
	}

    private float BinaryToFloat(int[] bin) {
        float num = 0f;
        for (int i = 0; i < bin.Length; i++) {
            if (bin[i] == 1) {
                num += Mathf.Pow(2,(7 - i));
            }
        }
        return num / 255f;
    }

    public static int[] JoinGenes(int[][] genes, int geneLength) {
        int numGenes = genes.Length;
        int length = numGenes * geneLength;
        int[] chromosome = new int[length];
        for (int i = 0; i < numGenes; i++) {
            Array.Copy(genes[i], 0, chromosome, i * geneLength, geneLength);
        }
        return chromosome;
    }

    private int[][] SplitGenes(int[] genes, int numGenes) {
        int geneLength = genes.Length / numGenes;
        int[][] chromosome = new int[numGenes][];
        for (int i = 0; i < numGenes; i++) {
            chromosome[i] = new int[geneLength];
            Array.Copy(genes, i * geneLength, chromosome[i], 0, geneLength);
        }
        return chromosome;
    }


    public void Crossover(PhenoType p1, PhenoType p2, int crossoverIndex) {
        int[] c1 = p1.Chromosome;
        int[] c2 = p2.Chromosome;
		for (int i = 0; i < c1.Length; i++) {
			// Crossover
			if (i < crossoverIndex) {
				m_Chromosome[i] = c1[i];
			} else {
				m_Chromosome[i] = c2[i];
			}

			// chance of mutation
			if (UnityEngine.Random.value <= 0.10f) {
                if (m_Chromosome[i] == 0) {
                    m_Chromosome[i] = 1;
                } else {
                    m_Chromosome[i] = 0;
                }
			}
		}
        m_SplitChromosome = SplitGenes(m_Chromosome, 6);
        this.PrintChromosome();
        SetParameters();
    }

    public void PostCrossoverUpdate() {
        m_SplitChromosome = SplitGenes(m_Chromosome, 6);
        SetParameters();

    }


    public void PrintChromosome() {
        string buf = "";
        foreach (int g in m_Chromosome) {
            buf += g.ToString();
        }
        Debug.Log(buf);

    }


}

