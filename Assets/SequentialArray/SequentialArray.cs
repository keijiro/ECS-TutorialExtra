using UnityEngine;
using System.Collections.Generic;
using System.Linq;

sealed class SequentialArray : MonoBehaviour
{
    [field:SerializeField] public GameObject Prefab = null;
    [field:SerializeField] public Color[] Palette = null;
    [field:SerializeField] public float LowAlpha = 0.5f;
    [field:SerializeField] public int RowCount = 10;
    [field:SerializeField] public float CellHeight = 0.2f;
    [field:SerializeField] public Vector2 Interval = new Vector2(1, 0.02f);
    [field:SerializeField] public int Seed = 123;
    [field:SerializeField] public float Delay = 0.5f;
    [field:SerializeField] public float GapProbability = 0.3f;

    List<GameObject>[] _cellTable;

    async void Start()
    {
        Random.InitState(Seed);

        _cellTable = Enumerable.Range(0, Palette.Length)
                     .Select(x => new List<GameObject>()).ToArray();

        for (var col = 0; col < Palette.Length; col++)
        {
            var rows = Random.Range(RowCount / 2, RowCount - col - 1);

            for (var row = 0; row < rows; row += col + 1)
            {
                var h = col + 1;
                var x = col * (1 + Interval.x);
                var y = CellHeight * (row + 0.5f * h) + 0.5f * Interval.y;
                var s = CellHeight * h - Interval.y;

                var cell = Instantiate(Prefab, new Vector3(x, -y, 0), Quaternion.identity);
                cell.transform.localScale = new Vector3(1, s, 1);

                cell.GetComponent<MeshRenderer>().material.color = Palette[col] * LowAlpha;

                _cellTable[col].Add(cell);
            }
        }

        var deltaAlpha = (1 - LowAlpha) / (Delay * 0.2f);

        for (var idx = 0; idx < Palette.Length; idx++)
        {
            var cells = _cellTable[idx];
            foreach (var cell in cells)
            {
                var m = cell.GetComponent<MeshRenderer>().material;

                var alpha = LowAlpha;
                while (alpha < 1)
                {
                    alpha = Mathf.Min(1, alpha + deltaAlpha * Time.deltaTime);
                    m.color = Palette[idx] * alpha;
                    await Awaitable.NextFrameAsync();
                }

                await Awaitable.WaitForSecondsAsync(Delay);

                while (alpha > LowAlpha)
                {
                    alpha = Mathf.Max(LowAlpha, alpha - deltaAlpha * Time.deltaTime);
                    m.color = Palette[idx] * alpha;
                    await Awaitable.NextFrameAsync();
                }
            }
        }
    }
}
