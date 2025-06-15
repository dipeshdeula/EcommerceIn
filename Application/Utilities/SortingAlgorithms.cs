using System.Linq.Expressions;

namespace Application.Utilities;

public static class SortingAlgorithms
{
    public static async Task<IEnumerable<T>> QuickSort<T, Tkey>(this Task<IEnumerable<T>> source, Expression<Func<T, Tkey>> keySelector, IComparer<Tkey>? comparer = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        List<T> list = new List<T>(await source);
        comparer ??= Comparer<Tkey>.Default;
        QuickSortInternal(list, 0, list.Count - 1, keySelector, comparer);
        return list;
    }
    public static async Task<IEnumerable<T>> QuickSortDesc<T, Tkey>(this Task<IEnumerable<T>> source, Expression<Func<T, Tkey>> keySelector, IComparer<Tkey>? comparer = null)
      where Tkey : IComparable<Tkey>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        List<T> list = new List<T>(await source);
        comparer ??= Comparer<Tkey>.Create((x, y) => y.CompareTo(x));
        QuickSortInternal(list, 0, list.Count - 1, keySelector, comparer);
        return list;
    }

    public static IEnumerable<T> QuickSort<T, Tkey>(this IEnumerable<T> source, Expression<Func<T, Tkey>> keySelector, IComparer<Tkey>? comparer = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        List<T> list = new List<T>(source);
        comparer ??= Comparer<Tkey>.Default;
        QuickSortInternal(list, 0, list.Count - 1, keySelector, comparer);
        return list;
    }
    public static IEnumerable<T> QuickSortDesc<T, Tkey>(this IEnumerable<T> source, Expression<Func<T, Tkey>> keySelector, IComparer<Tkey>? comparer = null)
      where Tkey : IComparable<Tkey>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        List<T> list = new List<T>(source);
        comparer ??= Comparer<Tkey>.Create((x, y) => y.CompareTo(x));
        QuickSortInternal(list, 0, list.Count - 1, keySelector, comparer);
        return list;
    }
    public static void QuickSortInternal<T, Tkey>(List<T> list, int start, int end, Expression<Func<T, Tkey>> keySelector, IComparer<Tkey> comparer)
    {
        if (start >= end) return;

        int pivotIndex = Partition(list, start, end, keySelector, comparer);
        QuickSortInternal(list, start, pivotIndex - 1, keySelector, comparer);
        QuickSortInternal(list, pivotIndex + 1, end, keySelector, comparer);
    }

    private static int Partition<T, Tkey>(List<T> list, int start, int end, Expression<Func<T, Tkey>> keySelector, IComparer<Tkey> comparer)
    {
        T pivot = list[end]; // Choose last element as pivot
        int i = start - 1;
        var compiledKeySelector = keySelector.Compile();
        for (int j = start; j < end; j++)
        {
            if (comparer.Compare(compiledKeySelector(list[j]), compiledKeySelector(pivot)) <= 0)
            {
                i++;
                Swap(list, i, j);
            }
        }

        Swap<T>(list, i + 1, end);
        return i + 1;
    }

    private static void Swap<T>(List<T> list, int a, int b)
    {
        T temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }
}