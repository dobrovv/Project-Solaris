using System;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class LaneComp : MonoBehaviour
{
    private Graph.EdgeT edge;

    private StarComp keepA;
    private StarComp keepB;
    public List<EnergyComp> unitsOnRoad = new List<EnergyComp>();

    private LineRenderer lineRenderer;

    private Material solidRoadMat;
    private Material dashedRoadMat;

    private Color NeutralLaneColor = new Color(26f / 255f, 49f / 255, 81f / 255);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateLine();
        SetSolidLine();

    }

    public void InitLane(Graph.EdgeT edge)
    {
        keepA = edge.A.keep;
        keepB = edge.B.keep;
        this.edge = edge;

    }

    void CreateLine()
    {
        lineRenderer = this.gameObject.AddComponent<LineRenderer>();
        // set z index to be under the stars
        gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, 101);
        lineRenderer.textureMode = LineTextureMode.Tile; // Optional: Set texture mode to Tile for repeating textures

        solidRoadMat = Resources.Load("Shaders/Shader Graphs_SolidLine", typeof(Material)) as Material;
        dashedRoadMat = Resources.Load("Shaders/Shader Graphs_DottedLine", typeof(Material)) as Material;

        if (dashedRoadMat == null || solidRoadMat == null)
            Debug.LogError("Material not found in Resources folder!");

        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;

        lineRenderer.material = solidRoadMat;
        lineRenderer.SetPosition(0, StarA.Position);
        lineRenderer.SetPosition(1, StarB.Position);
    }

    public void SetLineAlpha(float alphaTarget)
    {
        // Get the current colors
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;

        // Modify only the alpha (A) component
        startColor.a = alphaTarget;
        endColor.a = alphaTarget;

        // Apply back to the LineRenderer
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
    }

    public void SetDottedLine()
    {
        lineRenderer.material = dashedRoadMat;
        lineRenderer.material.SetFloat("_Height", 0.01f);
        lineRenderer.material.SetFloat("_Speed", 1.3f);
        SetLineAlpha(1f);

    }

    public void SetSolidLine()
    {
        lineRenderer.material = solidRoadMat;
        lineRenderer.material.SetFloat("_Height", 0.01f);
        SetLineAlpha(0.5f);

        if (keepA.Faction == keepB.Faction && keepA.Faction != Factions.Neutral) {
            lineRenderer.material.SetFloat("_Height", 0.015f);
            lineRenderer.material.SetColor("_Color", keepA.Faction.color);
        }
        else
        {
            lineRenderer.material.SetFloat("_Height", 0.01f);
            lineRenderer.material.SetColor("_Color", NeutralLaneColor);
        }
    }

    public StarComp StarA { get { return keepA; } }
    public StarComp StarB { get { return keepB; } }

    public StarComp getKeepA() { return keepA; }
    public StarComp getKeepB() { return keepB; }

    public StarComp getOtherKeep(StarComp keepOnRoad)
    {
        if (keepOnRoad != keepA && keepOnRoad != keepB)
            throw new ArgumentException("Keep is not on the road");

        if (keepA == keepOnRoad)
            return keepB;
        else
            return keepA;
    }

    public void addUnitToUnitsOnRoad(EnergyComp unit)
    {
        unitsOnRoad.Add(unit);
    }

    public void removeUnitFromUnitsOnRoad(EnergyComp unit)
    {
        unitsOnRoad.Remove(unit);
    }

    public List<EnergyComp> getUnitsOnRoad()
    {
        List<EnergyComp> copiedList = new List<EnergyComp>(unitsOnRoad);
        return copiedList;
    }

    // Update is called once per frame
    void Update()
    {
      
    }
}
