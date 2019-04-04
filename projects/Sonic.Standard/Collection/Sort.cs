namespace Std.Collection;

/// <summary>
///     鎻愪緵鍘熷湴鎺掑簭绠楁硶鐨勯潤鎬佸伐鍏风被锛屽寘鎷揩閫熸帓搴忓拰褰掑苟鎺掑簭銆?///
/// </summary>
public static class Sort
{
    /// <summary>
    ///     浣跨敤鎸囧畾姣旇緝鍣ㄥ鍒楄〃杩涜蹇€熸帓搴忋€?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     鍏冪礌绫诲瀷銆?/typeparam>
    ///     <param name="list">
    ///         瑕佹帓搴忕殑鍒楄〃銆?/param>
    ///         <param name="comparison">鍏冪礌姣旇緝鍑芥暟銆?/param>
    public static void quick_sort<T>(List<T> list, Comparison<T> comparison)
    {
        quick_sort_range(list, 0, list.Count - 1, comparison);
    }

    /// <summary>
    ///     浣跨敤榛樿姣旇緝鍣ㄥ鍒楄〃杩涜蹇€熸帓搴忋€?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     鍏冪礌绫诲瀷锛屽繀椤诲彲姣旇緝�?/typeparam>
    ///     <param name="list">瑕佹帓搴忕殑鍒楄〃銆?/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void quick_sort<T>(List<T> list)
        where T : IComparable<T>
    {
        quick_sort(list, (a, b) => a.CompareTo(b));
    }

    /// <summary>
    ///     浣跨敤鎸囧畾姣旇緝鍣ㄥ鍒楄〃杩涜褰掑苟鎺掑簭�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     鍏冪礌绫诲瀷銆?/typeparam>
    ///     <param name="list">
    ///         瑕佹帓搴忕殑鍒楄〃銆?/param>
    ///         <param name="comparison">鍏冪礌姣旇緝鍑芥暟銆?/param>
    public static void merge_sort<T>(List<T> list, Comparison<T> comparison)
    {
        merge_sort_range(list, 0, list.Count - 1, comparison);
    }

    /// <summary>
    ///     浣跨敤榛樿姣旇緝鍣ㄥ鍒楄〃杩涜褰掑苟鎺掑簭�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     鍏冪礌绫诲瀷锛屽繀椤诲彲姣旇緝�?/typeparam>
    ///     <param name="list">瑕佹帓搴忕殑鍒楄〃銆?/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void merge_sort<T>(List<T> list)
        where T : IComparable<T>
    {
        merge_sort(list, (a, b) => a.CompareTo(b));
    }

    private static void quick_sort_range<T>(List<T> list, int lo, int hi, Comparison<T> cmp)
    {
        if (lo >= hi) return;

        var pivot = partition(list, lo, hi, cmp);
        quick_sort_range(list, lo, pivot - 1, cmp);
        quick_sort_range(list, pivot + 1, hi, cmp);
    }

    private static int partition<T>(List<T> list, int lo, int hi, Comparison<T> cmp)
    {
        var pivot = list[hi];
        var i = lo - 1;

        for (var j = lo; j < hi; j++)
            if (cmp(list[j], pivot) < 0)
            {
                i++;
                swap(list, i, j);
            }

        swap(list, i + 1, hi);
        return i + 1;
    }

    private static void merge_sort_range<T>(List<T> list, int left, int right, Comparison<T> cmp)
    {
        if (left >= right) return;

        var mid = left + (right - left) / 2;
        merge_sort_range(list, left, mid, cmp);
        merge_sort_range(list, mid + 1, right, cmp);
        merge(list, left, mid, right, cmp);
    }

    private static void merge<T>(List<T> list, int left, int mid, int right, Comparison<T> cmp)
    {
        var leftSize = mid - left + 1;
        var rightSize = right - mid;

        var leftArray = new T[leftSize];
        var rightArray = new T[rightSize];

        for (var i = 0; i < leftSize; i++) leftArray[i] = list[left + i];

        for (var j = 0; j < rightSize; j++) rightArray[j] = list[mid + 1 + j];

        var li = 0;
        var ri = 0;
        var ki = left;

        while (li < leftSize && ri < rightSize)
        {
            if (cmp(leftArray[li], rightArray[ri]) <= 0)
            {
                list[ki] = leftArray[li];
                li++;
            }
            else
            {
                list[ki] = rightArray[ri];
                ri++;
            }

            ki++;
        }

        while (li < leftSize)
        {
            list[ki] = leftArray[li];
            li++;
            ki++;
        }

        while (ri < rightSize)
        {
            list[ki] = rightArray[ri];
            ri++;
            ki++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void swap<T>(List<T> list, int i, int j)
    {
        (list[i], list[j]) = (list[j], list[i]);
    }
}