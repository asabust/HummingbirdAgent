using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full")]
    public Color fullFlowerColor = new Color(1f, 0f, .3f);

    [Tooltip("The color when the flower is full")]
    public Color emptyFlowerColor = new Color(.5f, 0f, 1f);

    [HideInInspector] public Collider nectarCollider;

    private Collider flowerCollider;
    private Material flowerMaterial;

    public Vector3 FlowerUpVector
    {
        get { return nectarCollider.transform.up; }
    }

    public Vector3 FlowerCenterPosition
    {
        get { return nectarCollider.transform.position; }
    }

    public float NectarAmont { get; private set; }

    public bool HasNectar
    {
        get { return NectarAmont > 0f; }
    }

    /// <summary>
    /// remove nectar from the flower
    /// </summary>
    /// <param name="amont">the amount to remove</param>
    /// <returns>the actual amount successfully removed</returns>
    public float Feed(float amount)
    {
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmont);
        NectarAmont -= amount;

        if (NectarAmont <= 0)
        {
            NectarAmont = 0;

            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        return nectarTaken;
    }

    public void ResetFlower()
    {
        NectarAmont = 1f;
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}