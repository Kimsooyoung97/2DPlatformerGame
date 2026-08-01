using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Ground that is not ground.
    ///
    /// The disguise is painted straight into the real terrain tilemaps: the pit is filled
    /// with the neighbours' own tiles, the rim corner tiles are flattened so no seam is
    /// drawn down the sides, and the pit floor's grass row is buried so no stray green
    /// line shows through the dirt. Collapsing simply restores every one of those cells,
    /// which puts the original spike pit back exactly as it was.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class VanishingPlatformTrap : MonoBehaviour, ITrapResettable
    {
        [SerializeField] private Tilemap[] cellMaps;
        [SerializeField] private Vector3Int[] cells;
        [SerializeField] private TileBase[] intactTiles;
        [SerializeField] private TileBase[] collapsedTiles;

        [SerializeField] private Tilemap spikeMap;
        [SerializeField] private Vector3Int[] spikeCells;
        [SerializeField] private TileBase[] spikeTiles;

        [Tooltip("Beat between stepping on it and the floor giving way.")]
        [SerializeField] private float collapseDelay = 0.12f;

        private bool fired;

        public void Configure(
            Tilemap[] maps, Vector3Int[] targetCells,
            TileBase[] intact, TileBase[] collapsed,
            Tilemap spikes, Vector3Int[] hiddenSpikeCells, TileBase[] hiddenSpikeTiles)
        {
            cellMaps = maps;
            cells = targetCells;
            intactTiles = intact;
            collapsedTiles = collapsed;
            spikeMap = spikes;
            spikeCells = hiddenSpikeCells;
            spikeTiles = hiddenSpikeTiles;

            ResetTrap();
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Awake()
        {
            ResetTrap();
        }

        public void ResetTrap()
        {
            StopAllCoroutines();
            fired = false;

            ApplyTiles(true);
            PaintSpikes(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired || other == null)
                return;
            if (other.GetComponentInParent<PlayerHealth>() == null)
                return;

            fired = true;
            StartCoroutine(Collapse());
        }

        private IEnumerator Collapse()
        {
            if (collapseDelay > 0f)
                yield return new WaitForSeconds(collapseDelay);

            ApplyTiles(false);
            PaintSpikes(true);
        }

        private void ApplyTiles(bool intact)
        {
            if (cells == null || cellMaps == null)
                return;

            TileBase[] source = intact ? intactTiles : collapsedTiles;

            for (int i = 0; i < cells.Length; i++)
            {
                if (i >= cellMaps.Length || cellMaps[i] == null)
                    continue;

                TileBase tile = source != null && i < source.Length ? source[i] : null;
                cellMaps[i].SetTile(cells[i], tile);
            }
        }

        private void PaintSpikes(bool present)
        {
            if (spikeMap == null || spikeCells == null)
                return;

            for (int i = 0; i < spikeCells.Length; i++)
            {
                TileBase tile = present && spikeTiles != null && i < spikeTiles.Length
                    ? spikeTiles[i]
                    : null;
                spikeMap.SetTile(spikeCells[i], tile);
            }
        }
    }
}
