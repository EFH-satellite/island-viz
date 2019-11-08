﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OsgiViz.SideThreadConstructors;
using OsgiViz.Core;
using OsgiViz.Island;
using OsgiViz.Unity.MainThreadConstructors;
using OsgiViz.Unity.Island;

/// <summary>
/// This class handles the basic software visualization. 
/// Additional visualization stuff if handled by the IslandVizBehavior through its 
/// VisualizationComponents.
/// </summary>
public class IslandVizVisualization : MonoBehaviour
{
    public static IslandVizVisualization Instance; // The instance of this class.

    [Header("Settings")]
    public int RandomSeed;
    public Graph_Layout Graph_Layout;

    [Header("Prefabs")]
    public GameObject Water_Plane_Prefab;

    [Header("Environment GameObjects")]
    public GameObject Table;

    [Header("Additional Components Container")]
    public GameObject VisualizationComponentsGameObject;

    [HideInInspector]
    public VisualizationTransformContainer TransformContainer;

    [HideInInspector]
    public Transform VisualizationRoot;

    public List<CartographicIsland> IslandStructures;
    public List<IslandGO> IslandGameObjects;

    public Text ZoomLevelValue;

    // Mandatory coomonents for visualization.
    private IslandGOConstructor islandGOConstructor;
    //private ServiceGOConstructor serviceGOConstructor;
    private DockGOConstructor dockGOConstructor;
    private HierarchyConstructor hierarchyConstructor;
    private IslandStructureConstructor isConstructor;
    private Graph_Layout_Constructor bdConstructor;
    private Neo4jOsgiConstructor neo4jConstructor;

    private AdditionalIslandVizComponent[] visualizationComponents; // Array of all additional input componets.

    private System.Random RNG;
    private System.Diagnostics.Stopwatch stopwatch;

    private List<IslandGO> currentIslands;

    private ZoomLevel currentZoomLevel;
    private bool zoomLevelDirty;
    private bool islandsDirty;


    // ################
    // Initiation
    // ################

    #region Initiation

    /// <summary>
    /// Called by Unity on application stat up before the Start() method.
    /// </summary>
    void Awake()
    {
        Instance = this;

        // Get all optimal additional visualization components
        visualizationComponents = VisualizationComponentsGameObject.GetComponents<AdditionalIslandVizComponent>();

        // Since we saved all additional visualization components in "visualizationComponents", we can add the remaining
        // mandatory visualization components.
        islandGOConstructor = VisualizationComponentsGameObject.AddComponent<IslandGOConstructor>();
        //serviceGOConstructor = VisualizationComponentsGameObject.AddComponent<ServiceGOConstructor>();
        dockGOConstructor = VisualizationComponentsGameObject.AddComponent<DockGOConstructor>();
        hierarchyConstructor = VisualizationComponentsGameObject.AddComponent<HierarchyConstructor>();

        isConstructor = new IslandStructureConstructor(1, 2, 8);
        bdConstructor = new Graph_Layout_Constructor();

        // Create root transforms and add some stuff
        TransformContainer = new VisualizationTransformContainer();
        VisualizationRoot = new GameObject("Visualization").transform;
        TransformContainer.IslandContainer = new GameObject("VisualizationContainer").transform;
        TransformContainer.IslandContainer.SetParent(VisualizationRoot);
        TransformContainer.DependencyContainer = new GameObject("DependencyContainer").transform;
        TransformContainer.DependencyContainer.SetParent(VisualizationRoot);
        
        // Create water visual
        GameObject water = (GameObject)Instantiate(Water_Plane_Prefab, TransformContainer.IslandContainer);
        water.name = Water_Plane_Prefab.name; // Just making shure since there are still a alot GameObject.Find... TODO: remove in future
        water.transform.localPosition = Vector3.zero;
        water.transform.localScale = new Vector3(1000, 1, 1000);

        RNG = new System.Random(RandomSeed);
        stopwatch = new System.Diagnostics.Stopwatch();

        currentIslands = new List<IslandGO>();
    }

    /// <summary>
    /// This Coroutine creates the OSGI visualization from a JSON file located at projectModelFile.
    /// </summary>
    public IEnumerator ConstructVisualization()
    {
        yield return null;

        // Start the timer to measure total construction time.
        stopwatch.Start();
                
        //Construct islands from bundles in the osgi Object.
        yield return isConstructor.Construct(IslandVizData.Instance.OsgiProject);

        // Construct the spatial distribution of the islands.
        if (Graph_Layout == Graph_Layout.ForceDirected)
        {
            yield return bdConstructor.ConstructFDLayout(IslandVizData.Instance.OsgiProject, 0.25f, 70000, RNG);
        }
        else if (Graph_Layout == Graph_Layout.Random)
        {
            Vector3 minBounds = new Vector3(-10.5f, 1.31f, -10.5f);
            Vector3 maxBounds = new Vector3(10.5f, 1.31f, 10.5f);
            yield return bdConstructor.ConstructRndLayout(IslandVizData.Instance.OsgiProject.getDependencyGraph(), minBounds, maxBounds, 0.075f, 10000, RNG);
        }

        GlobalVar.islandNumber = IslandVizData.Instance.OsgiProject.getBundles().Count;
        IslandStructures = isConstructor.getIslandStructureList();

        // Construct and store the island GameObjects.
        yield return islandGOConstructor.Construct(IslandVizVisualization.Instance.IslandStructures, TransformContainer.IslandContainer.gameObject);
        IslandGameObjects = islandGOConstructor.getIslandGOs();

        // Construct the connections between the islands from services in the osgi Object.
        //yield return serviceGOConstructor.Construct(IslandVizData.Instance.osgiProject.getServices(), islandGOConstructor.getIslandGOs());

        // Construct the dock GameObjects.
        yield return dockGOConstructor.Construct(islandGOConstructor.getIslandGOs(), TransformContainer.IslandContainer.gameObject);

        // Construct the island hierarchy. TODO enable in the future?
        //yield return hierarchyConstructor.Construct(islandGOConstructor.getIslandGOs());
                
        yield return AutoZoom();

        StartCoroutine(ZoomLevelRoutine());

        // Set table height
        //GlobalVar.hologramTableHeight = IslandVizInteraction.Instance.GetPlayerEyeHeight() - 0.75f;
        UpdateTableHight(GlobalVar.hologramTableHeight);

        stopwatch.Stop();
        Debug.Log("IslandVizVisualization Construction finished after " + stopwatch.Elapsed.TotalSeconds.ToString("0.00") + " seconds!");
    }       

    /// <summary>
    /// Initialize all additional input components. Called by IslandVizBehavior.
    /// </summary>
    /// <returns></returns>
    public IEnumerator InitVisualizationComponents()
    {
        foreach (var item in visualizationComponents)
        {
            item.Init();
            yield return null;
        }
    }

    #endregion



    public void AddCurrentIsland(IslandGO islandGO)
    {
        if (!currentIslands.Contains(islandGO))
        {
            currentIslands.Add(islandGO);

            if (islandGO.currentZoomLevel != currentZoomLevel)
            {
                islandsDirty = true;
            }
            //Debug.Log(currentIslands.Count);
        }
    }

    public void RemoveCurrentIsland(IslandGO islandGO)
    {
        if (currentIslands.Contains(islandGO))
        {
            currentIslands.Remove(islandGO);

            //Debug.Log(currentIslands.Count);
        }
    }



    // ################
    // Zoom Level
    // ################

    public void ZoomChanged ()
    {
        zoomLevelDirty = true;
    }



    /// <summary>
    /// 
    /// Level 1: 100%-60%
    /// Level 2: 60%-30% 
    /// Level 3: 30%-1%
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator ZoomLevelRoutine ()
    {
        float zoomLevelPercent = 0;

        zoomLevelDirty = true;
        islandsDirty = true;

        while (true)
        {
            if (zoomLevelDirty)
            {
                zoomLevelDirty = false;                

                // Calculate current zoom level in percent
                zoomLevelPercent = Mathf.Sqrt(Mathf.Sqrt((GlobalVar.CurrentZoom - GlobalVar.MinZoom) / (GlobalVar.MaxZoom - GlobalVar.MinZoom))) * 100;

                // Check for ZoomLevel changes
                if (zoomLevelPercent <= GlobalVar.NearZoomLevelPercent && currentZoomLevel != ZoomLevel.Far)
                {
                    // Apply current zoom level to UI
                    currentZoomLevel = ZoomLevel.Far;
                    islandsDirty = true;
                }
                else if (zoomLevelPercent <= GlobalVar.MediumZoomLevelPercent && currentZoomLevel != ZoomLevel.Medium)
                {
                    // Apply current zoom level to UI
                    currentZoomLevel = ZoomLevel.Medium;
                    islandsDirty = true;
                }
                else if (zoomLevelPercent > GlobalVar.MediumZoomLevelPercent && currentZoomLevel != ZoomLevel.Near)
                {                    
                    currentZoomLevel = ZoomLevel.Near;
                    islandsDirty = true;
                }
                // Apply current zoom level to UI
                ZoomLevelValue.text = zoomLevelPercent.ToString("0") + "%";

                // Debug
                //Debug.Log(GlobalVar.MinZoomLevel + " - " + GlobalVar.CurrentZoomLevel + " - " + GlobalVar.MaxZoomLevel + " -> " + zoomPercent + "%");
            }

            if (islandsDirty)
            {
                islandsDirty = false;

                // Give feedback
                ZoomLevelValue.text = "...";
                yield return ApplyZoomLevelToCurrentIslands();
                // Apply current zoom level to UI
                ZoomLevelValue.text = zoomLevelPercent.ToString("0") + "%";
            }
            

            
            
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator ApplyZoomLevelToCurrentIslands ()
    {

        if (currentZoomLevel == ZoomLevel.Near)
        {
            // Enable all buildings.
            for (int i = 0; i < currentIslands.Count; i++)
            {
                if (currentIslands.Count <= i || currentIslands[i].currentZoomLevel == currentZoomLevel)
                {
                    continue;
                }
                currentIslands[i].currentZoomLevel = currentZoomLevel;
                currentIslands[i].getImportDock().SetActive(true);
                currentIslands[i].getExportDock().SetActive(true);
                foreach (var region in currentIslands[i].getRegions())
                {
                    foreach (var building in region.getBuildings())
                    {
                        if (!building.gameObject.activeSelf)
                            building.gameObject.SetActive(true);
                    }
                    yield return null;
                }
            }
        }
        else if (currentZoomLevel == ZoomLevel.Medium)
        {
            // Disable all buildings.
            for (int i = 0; i < currentIslands.Count; i++)
            {
                if (currentIslands.Count <= i || currentIslands[i].currentZoomLevel == currentZoomLevel)
                {
                    continue;
                }
                currentIslands[i].currentZoomLevel = currentZoomLevel;
                currentIslands[i].getImportDock().SetActive(true);
                currentIslands[i].getExportDock().SetActive(true);
                foreach (var region in currentIslands[i].getRegions())
                {
                    foreach (var building in region.getBuildings())
                    {
                        if (building.gameObject.activeSelf)
                            building.gameObject.SetActive(false);
                    }
                    yield return null;
                }
            }
        }
        else if (currentZoomLevel == ZoomLevel.Far)
        {
            for (int i = 0; i < currentIslands.Count; i++)
            {
                if (currentIslands.Count <= i || currentIslands[i].currentZoomLevel == currentZoomLevel)
                {
                    continue;
                }
                currentIslands[i].currentZoomLevel = currentZoomLevel;
                currentIslands[i].getImportDock().SetActive(false);
                currentIslands[i].getExportDock().SetActive(false);                
            }
        }
    }


    // ################
    // Helper Functions
    // ################

    #region HelperFunctions

    /// <summary>
    /// Scales the VisualizationContainer, so all islands are visible on start. The CurrentZoomLevel is saved in the GlobalVar.
    /// </summary>
    /// <returns></returns>
    IEnumerator AutoZoom()
    {
        Transform furthestIslandTransform = null; // Transfrom of the island which is furthest away from the center.
        float furthestDistance = 0; // Furthest distance of a island to the center.
        float distance_temp = 0;

        float maxDistance = 0.7f; // TODO move to Settings

        // Search island which is furthest away from the center.
        foreach (var islandGO in islandGOConstructor.getIslandGOs())
        {
            distance_temp = Vector3.Distance(islandGO.transform.position, Vector3.zero);
            if (furthestIslandTransform == null || distance_temp > furthestDistance)
            {
                furthestDistance = distance_temp;
                furthestIslandTransform = islandGO.transform;
            }
        }

        yield return null;

        VisualizationRoot.localScale *= maxDistance / furthestDistance;
        //TransformContainer.IslandContainer.localScale *= maxDistance / furthestDistance; // Scales the islands to make all of them fit on the table.
        GlobalVar.CurrentZoom = VisualizationRoot.localScale.x;
        GlobalVar.MinZoom = VisualizationRoot.localScale.x;

        // Debug
        //TransformContainer.DependencyContainer.localScale *= GlobalVar.CurrentZoomLevel;
    }

    /// <summary>
    /// Moves the table to a new hight.
    /// </summary>
    /// <param name="height">The new height (in meters) of the table.</param>
    public void UpdateTableHight (float height)
    {
        Table.transform.position = new Vector3(Table.transform.position.x, height, Table.transform.position.z);
        VisualizationRoot.transform.position = new Vector3(VisualizationRoot.transform.position.x, height, VisualizationRoot.transform.position.z);
    }

    #endregion
}

public enum Graph_Layout
{
    ForceDirected,
    Random
}

public enum ZoomLevel
{
    Near,
    Medium,
    Far
}

public class VisualizationTransformContainer
{
    public Transform IslandContainer;
    public Transform DependencyContainer;
}
