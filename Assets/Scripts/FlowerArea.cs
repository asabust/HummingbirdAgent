using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 
/// </summary>
public class FlowerArea : MonoBehaviour
{
    public const float AreaDiametor = 20f;

    private List<GameObject> flowerPlants;
    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    public List<Flower> Flowers { get; private set; }

    public void ResetFlower()
    {
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotaion = Random.Range(-5f, 5f);
            float yRotaion = Random.Range(-180f, 180f);
            float zRotaion = Random.Range(-5f, 5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotaion, yRotaion, zRotaion);
        }

        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    public Flower GerFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void FindChileFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag("flowerPlant"))
            {
                flowerPlants.Add(child.gameObject);
                FindChileFlowers(child);
            }
            else
            {
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    Flowers.Add(flower);
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    FindChileFlowers(child);
                }
            }
        }
    }

    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    private void Start()
    {
        FindChileFlowers(transform);
    }
}