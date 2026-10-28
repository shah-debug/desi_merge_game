using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Plans the Shuffle power-up: new resting positions for every item, in board-local space (x = 0 centre, y = 0 floor).
    /// Items are packed into horizontal shelves, largest first, in random order within each shelf. Each shelf is as tall
    /// as its largest item and items sit on its bottom, so no two circles can overlap, every circle is inside the walls
    /// with at least <c>gap</c> to spare, and items sit on the floor or slightly above the row below (they just settle).
    /// The plan fails, and nothing should move, if the stack would reach above <c>maxTop</c>.
    /// </summary>
    public static class ShufflePlanner
    {
        /// <param name="radii">Radius of each item; the result is index-aligned with it.</param>
        /// <param name="positions">Filled with one centre per item when the plan succeeds.</param>
        public static bool TryPlan(IReadOnlyList<float> radii, float halfWidth, float maxTop, float gap, System.Random random, List<Vector2> positions)
        {
            if (radii == null || positions == null || random == null)
            {
                throw new ArgumentNullException(radii == null ? nameof(radii) : positions == null ? nameof(positions) : nameof(random));
            }

            positions.Clear();
            var width = halfWidth * 2f;
            var order = new List<int>(radii.Count);
            for (var i = 0; i < radii.Count; i++)
            {
                if (radii[i] <= 0f || radii[i] * 2f + gap * 2f > width)
                {
                    return false;
                }

                order.Add(i);
            }

            // Random order first, then a stable sort by size: equal sizes keep their random order.
            Shuffle(order, random);
            order = StableSortByRadiusDescending(order, radii);

            var planned = new Vector2[radii.Count];
            var floor = 0f;
            var start = 0;
            while (start < order.Count)
            {
                // Fill one shelf.
                var used = gap;
                var end = start;
                var tallest = 0f;
                while (end < order.Count && used + radii[order[end]] * 2f + gap <= width)
                {
                    used += radii[order[end]] * 2f + gap;
                    tallest = Mathf.Max(tallest, radii[order[end]] * 2f);
                    end++;
                }

                if (floor + tallest > maxTop)
                {
                    return false;
                }

                var shelf = order.GetRange(start, end - start);
                Shuffle(shelf, random);
                // Spread the spare width evenly so the shelf isn't jammed against one wall.
                var spacing = gap + (width - used) / (shelf.Count + 1);
                var x = -halfWidth + spacing;
                foreach (var index in shelf)
                {
                    var radius = radii[index];
                    planned[index] = new Vector2(x + radius, floor + radius);
                    x += radius * 2f + spacing;
                }

                floor += tallest + gap;
                start = end;
            }

            positions.AddRange(planned);
            return true;
        }

        /// <summary>True if any two circles overlap by more than <paramref name="tolerance"/>. Used as a safety check and by tests.</summary>
        public static bool HasOverlap(IReadOnlyList<Vector2> centres, IReadOnlyList<float> radii, float tolerance = 0.0001f)
        {
            for (var i = 0; i < centres.Count; i++)
            {
                for (var j = i + 1; j < centres.Count; j++)
                {
                    if (Vector2.Distance(centres[i], centres[j]) < radii[i] + radii[j] - tolerance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>True if every circle lies within the walls, above the floor and with its top at or below <paramref name="maxTop"/>.</summary>
        public static bool AllInside(IReadOnlyList<Vector2> centres, IReadOnlyList<float> radii, float halfWidth, float maxTop)
        {
            for (var i = 0; i < centres.Count; i++)
            {
                var c = centres[i];
                var r = radii[i];
                if (c.x - r < -halfWidth || c.x + r > halfWidth || c.y - r < 0f || c.y + r > maxTop + 0.0001f)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle(List<int> list, System.Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<int> StableSortByRadiusDescending(List<int> order, IReadOnlyList<float> radii)
        {
            var sorted = new List<int>(order.Count);
            foreach (var index in order)
            {
                var insertAt = sorted.Count;
                while (insertAt > 0 && radii[sorted[insertAt - 1]] < radii[index])
                {
                    insertAt--;
                }

                sorted.Insert(insertAt, index);
            }

            return sorted;
        }
    }
}
