﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using TriangleNet.Voronoi;
using TriangleNet.Topology;
using OsgiViz.Island;

namespace OsgiViz.Unity.Island
{

    public class IslandGO : MonoBehaviour
    {
        public ZoomLevel CurrentZoomLevel;

        public bool Selected;
        public bool Visible;

        private CartographicIsland island;
        private List<Region> regions;
        private GameObject coast;
        private GameObject importDock;
        private GameObject exportDock;


        void Awake()
        {
            regions = new List<Region>();
            importDock = null;
            exportDock = null;
            coast = null;

            IslandVizInteraction.Instance.OnIslandSelect += OnSelection;
        }



        // ################
        // Selection Events
        // ################

        private void OnSelection (IslandGO island, IslandVizInteraction.SelectionType selectionType, bool selected)
        {
            if (island != this && selectionType == IslandVizInteraction.SelectionType.Select && selected) // Another island was selected while this island was selected.
            {
                if (Selected)
                {
                    Selected = false;
                    IslandVizInteraction.Instance.OnIslandSelect(this, IslandVizInteraction.SelectionType.Select, false);
                }
            }
            else if (island == this && selectionType == IslandVizInteraction.SelectionType.Select) // This island was selected/deselected.
            {
                Selected = selected;
            }
        }


        // ################
        // Physics
        // ################
        #region Physics

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "TableContent")
            {
                MakeIslandVisible();
                IslandVizVisualization.Instance.OnIslandVisible(this);
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "TableContent")
            {
                MakeIslandInvisible();
                IslandVizVisualization.Instance.OnIslandInvisible(this);
            }
        }

        #endregion



        // ################
        // Visible & Invisible
        // ################

        private void MakeIslandVisible ()
        {
            // Enable all children, i.e. make island visible.
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }
            Visible = true;
        }

        private void MakeIslandInvisible()
        {
            // Disable all children, i.e. make island invisible.
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            Visible = false;
        }







        // ################
        // Zoom Level
        // ################

        #region Zoom Level

        /// <summary>
        /// Apply the rules of all ZoomLevels to an island. 
        /// Call this to change the Zoomlevel of an island.
        /// </summary>
        /// <param name="newZoomLevel">The ZoomLevel that you want to apply to the island.</param>
        /// <returns></returns>
        public IEnumerator ApplyZoomLevel(ZoomLevel newZoomLevel)
        {
            if (CurrentZoomLevel == newZoomLevel)
            {
                // Do nothing.
            }
            else if (newZoomLevel == ZoomLevel.Near)
            {
                yield return ApplyNearZoomLevel();
            }
            else if (newZoomLevel == ZoomLevel.Medium)
            {
                yield return ApplyMediumZoomLevel();
            }
            else if (newZoomLevel == ZoomLevel.Far)
            {
                yield return ApplyFarZoomLevel();
            }
            CurrentZoomLevel = newZoomLevel;
        }


        public IEnumerator ApplyNearZoomLevel()
        {
            // Disable region colliders & enable buildings.
            foreach (var region in regions)
            {
                if (region.GetComponent<MeshCollider>().enabled)
                    region.GetComponent<MeshCollider>().enabled = true; // TODO?

                foreach (var building in region.getBuildings())
                {
                    if (!building.gameObject.activeSelf)
                        building.gameObject.SetActive(true);
                }
                yield return null;
            }

            // Disable island collider.
            if (GetComponent<CapsuleCollider>().enabled)
                GetComponent<CapsuleCollider>().enabled = false;
        }


        public IEnumerator ApplyMediumZoomLevel()
        {
            // NEAR -> MEDIUM 
            if (CurrentZoomLevel == ZoomLevel.Near)
            {
                foreach (var region in regions)
                {
                    foreach (var building in region.getBuildings())
                    {
                        if (building.gameObject.activeSelf)
                            building.gameObject.SetActive(false);
                    }
                    yield return null;
                }
                if (GetComponent<CapsuleCollider>().enabled)
                    GetComponent<CapsuleCollider>().enabled = true;                
            }
            // FAR -> MEDIUM
            else
            {
                // Enable Docks.
                if (!importDock.activeSelf)
                {
                    importDock.SetActive(true);
                    exportDock.SetActive(true);
                }                

                // Enable region colliders.
                foreach (var region in regions)
                {
                    if (!region.GetComponent<MeshCollider>().enabled)
                        region.GetComponent<MeshCollider>().enabled = true;
                    GetComponent<CapsuleCollider>().radius /= 2f;
                }

                // Disable island collider.
                if (GetComponent<CapsuleCollider>().enabled)
                    GetComponent<CapsuleCollider>().enabled = true;
            }
        }


        public IEnumerator ApplyFarZoomLevel ()
        {
            // Hide Docks.
            if (importDock.activeSelf)
            {
                importDock.SetActive(false);
                exportDock.SetActive(false);
            }   

            // Disable region colliders & hide buildings.
            foreach (var region in regions)
            {
                if (region.GetComponent<MeshCollider>().enabled)
                    region.GetComponent<MeshCollider>().enabled = false;

                if (CurrentZoomLevel == ZoomLevel.Medium)
                    GetComponent<CapsuleCollider>().radius *= 2f;

                foreach (var building in region.getBuildings())
                {
                    if (building.gameObject.activeSelf)
                        building.gameObject.SetActive(false);
                }
                yield return null;
            }

            // Enable island collider.
            GetComponent<CapsuleCollider>().enabled = true;
        }

        #endregion







        //Returns true if island does not contain a single CU. Returns false otherwise.
        public bool IsIslandEmpty()
        {
            foreach (Region reg in regions)
                if (reg.getBuildings().Count > 0)
                    return false;

            return true;
        }

        public GameObject getCoast()
        {
            return coast;
        }
        public GameObject getImportDock()
        {
            return importDock;
        }
        public GameObject getExportDock()
        {
            return exportDock;
        }
        public List<Region> getRegions()
        {
            return regions;
        }
        public CartographicIsland getIslandStructure()
        {
            return island;
        }

        public void addRegion(Region reg)
        {
            regions.Add(reg);
        }

        public void setCoast(GameObject c)
        {
            coast = c;
        }
        public void setImportDock(GameObject i)
        {
            importDock = i;
        }
        public void setExportDock(GameObject e)
        {
            exportDock = e;
        }
        public void setIslandStructure(CartographicIsland i)
        {
            island = i;
        }

    }
}
