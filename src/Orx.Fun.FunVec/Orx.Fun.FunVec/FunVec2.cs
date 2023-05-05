using System.Collections;
using System.Runtime.CompilerServices;

namespace Orx.Fun.FunVec;

/// <summary>
/// A unified functional 2-dimensional collection.
/// 
/// It does not hold the data, rather provides a unified view over different sources of data providing indexed access to collection elements.
/// 
/// 
/// <para>
/// Takes a functional approach; i.e., the following closures are defined and stored on construction:
/// <list type="bullet">
/// <item>closure to get the jagged lengths in all dimensions;</item>
/// <item>closure to get element at the given indices (also the bounds-checked version GetOrNone, in addition to direct access by indices);</item>
/// <item>closure to get the collection as IEnumerable.</item>
/// </list>
/// The allocation is limited by these closures.
/// The data is not re-allocated, or memoized.
/// </para>
/// <para>
/// For instance, if a unified collection is defined to always return a contant value,
/// and have a length of 1000 collections in the first dimension,
/// and 1000 elements in the second dimension for each of the collection;
/// there will NOT be an underlying array storing the same value 1_000_000 times.
/// </para>
/// 
/// <para>To clarify that the unified collection only holds a reference, consider the following example.</para>
/// <code>
/// int[][] underlyingArray = new int[] { new int[] { 0, 10 }, new int[] { 1, 11, 111, 1111 }, new int[] { 2, 22, 222 } };
/// FunVec2&lt;int> jagged = new(underlyingArray);
/// Assert(underlyingArray[1][0] == 1 and jagged[1, 0] == 1);
/// underlyingArray[1][0] = 42;
/// Assert(underlyingArray[1][0] == 42 and jagged[1, 0] == 42);
/// </code>
/// 
/// <para>To further illustrate, consider the following with underlying list of lists.</para>
/// <code>
/// List&lt;List&lt;char>> underlyingList = new() { new() { 'a', 'b', 'c', 'd' }, new() { 'u', 'v' } };
/// FunVec2&lt;char> jagged = new(underlyingList);
/// 
/// Assert(underlyingList.Count == 2 and jagged.Length1 == 2);
/// underlyingList.Add(new() { 'x', 'y', 'z' });
/// Assert(underlyingList.Count == 3 and jagged.Length1 == 3);
/// 
/// Assert(underlyingList[0].Count == 4 and jagged.Length2(0) == 4);
/// underlyingList[0].Add('e');
/// Assert(underlyingList[0].Count == 5 and jagged.Length2(0) == 5);
/// </code>
/// 
/// <para>The unified jagged collection exposes a unified interface for the following methods:</para>
/// <list type="bullet">
/// <item>Length1: length in the first dimension (i.e., number of 1D collections).</item>
/// <item>Length2(i): length of the i-th collection in the second dimension (i.e., number of elements in the i-th 1D collection).</item>
/// <item>this[i]: i-th D1 collection.</item>
/// <item>this[i,j]: element at the (i,j)-th position (i.e., j-th element of the i-th 1D collection).</item>
/// <item>GetOrNone(i, j): returns Some of the element at the (i,j)-th position if indices are valid; None otherwise.</item>
/// <item>AsEnumerable(): returns the collection as IEnumerable; particularly useful for linq calls over the collection.</item>
/// </list>
/// 
/// </summary>
/// <typeparam name="T">Type of the innermost elements of the collection.</typeparam>
public class FunVec2<T> : IEnumerable<FunVec1<T>>
{
    // data
    readonly Func<int, int, T> GetValueByIndex;
    /// <summary>
    /// Converts the unified collection to IEnumerable yielding its underlying values.
    /// Particularly useful for linq calls over the collection.
    /// 
    /// <code>
    /// Func&lt;int, bool> underlyingFun = i => i % 2 == 0;
    /// UniJaggedD1&lt;bool> jagged = new(underlyingFun, length1: Some(4));
    /// 
    /// bool anyEvens = jagged.AsEnumerable().Aggregate(false, (x, y) => x || y);
    /// Assert(anyEvens == true);
    /// 
    /// var enumerable = jagged.AsEnumerable();
    /// 
    /// int counter = 0;
    /// for (var isEven in enumerable)
    /// {
    ///     Assert(isEven == counter % 2 == 0);
    ///     counter++;
    /// }
    /// </code>
    /// </summary>
    readonly Func<IEnumerable<FunVec1<T>>> AsEnumerable;
    // data - pub
    /// <summary>
    /// Length of the vector.
    /// 
    /// <code>
    /// var underlyingList = new List&lt;int> { 10, 11, 12 };
    /// FunVec1&lt;int> vec = new(underlyingList);
    /// Assert(vec.Length1 == 3);
    /// 
    /// Func&lt;int, bool> underlyingFun = i => i % 2 == 0;
    /// vec = new(underlyingFun, length1: Some(4));
    /// Assert(vec.Length1, 4);
    /// 
    /// Func&lt;int, bool> underlyingFun = i => i % 2 == 0;
    /// vec = new(underlyingFun); // omitted optional argument 'length1' defaults to None -> no limit
    /// Assert(vec.Length1 == int.MaxValue);
    /// 
    /// </code>
    /// </summary>
    public readonly Func<int> Length1;
    /// <summary>
    /// Length of the jagged array in the first dimension; i.e., number of 1D collections.
    /// 
    /// <code>
    /// var underlyingList = new List&lt;List&lt;int>> { new() { 1 }, new() { 2, 3, 4 } };
    /// FunVec2&lt;int> jagged = new(underlyingList);
    /// Assert(jagged.Length1 == 2);
    /// Assert(jagged.Length2(0) == 1);
    /// Assert(jagged.Length2(1) == 3);
    /// 
    /// Func&lt;int, int, bool> underlyingFun = (i, j) => (i + j) % 2 == 0;
    /// FunVec2&lt;bool> upperTriangular = new(underlyingFun, length1: Some(3), getLength2: Some&lt;Func&lt;int, int>>(i => i + 1));
    /// Assert(jagged.Length1 == 3);
    /// Assert(jagged.Length2(0) == 1);
    /// Assert(jagged.Length2(1) == 2);
    /// Assert(jagged.Length2(2) == 3);
    /// 
    /// FunVec2&lt;Agent> bool = new(underlyingFun, length1: Some(2)); // omitted optional argument 'length2' defaults to None -> no limit
    /// Assert(jagged.Length2(0) == jagged.Length2(1) == int.MaxValue);
    /// 
    /// </code>
    /// </summary>
    public readonly Func<int, int> Length2;
    /// <summary>
    /// The unified collection might be constructed with a constant scalar value; hence, returning the scalar for all indices.
    /// If this is the case, <see cref="HasUnderlyingScalar"/> is true; and the field <see cref="UnderlyingScalar"/> equals to Some of the underlying scalar value.
    /// <para>Otherwise, HasUnderlyingScalar is false and UnderlyingScalar.IsNone.</para>
    /// 
    /// <code>
    /// // vec[i] = 10, for all i.
    /// UniJaggedD1&lt;int> vec = new(10);
    /// Assert(vec[3] == 10 and vec[42] == 10);
    /// Assert(vec.Get(12) == Some(10));
    /// 
    /// // underlying constant can be obtained by the optional UnderlyingScalar field.
    /// Assert(vec.HasUnderlyingScalar);
    /// Assert(vec.UnderlyingScalar.IsSome);
    /// Assert(vec.UnderlyingScalar == Some(10));
    /// Assert(vec.UnderlyingScalar.Unwrap() == 10);
    /// 
    /// </code>
    /// </summary>
    public readonly Opt<T> UnderlyingScalar;
    /// <summary>
    /// <inheritdoc cref="UnderlyingScalar"/>
    /// </summary>
    public bool HasUnderlyingScalar => UnderlyingScalar.IsSome;


    // ctor
    FunVec2(Func<int> length1, Func<int, int> length2,
        Func<int, int, T> getValueByIndex,
        Opt<T> underlyingConstantValue,
        Func<IEnumerable<FunVec1<T>>> asEnumerable)
    {
        Length1 = length1;
        Length2 = length2;
        GetValueByIndex = getValueByIndex;
        UnderlyingScalar = underlyingConstantValue;
        AsEnumerable = asEnumerable;
    }
    /// <summary>
    /// 2-dimensional jagged collection with optional lengths, which always yields the same constant value.
    /// 
    /// <code>
    /// var agentSmith = GetSmith();
    /// FunVec2&lt;Agent> jagged = new(agentSmith);
    /// Assert(jagged.Length1 == int.MaxValue);   // since length1 is omitted
    /// Assert(jagged.Length2(42) == int.MaxValue); // since length2 is omitted
    /// Assert(jagged[0][0] == agentSmith);
    /// Assert(jagged[42][42] == agentSmith);
    /// Assert(jagged.GetOrNone(100, 42) == Some(agentSmith));
    /// 
    /// FunVec2&lt;Agent> jagged = new(agentSmith, Some(50));
    /// Assert(jagged.Length1 == 50);
    /// Assert(jagged.Length2(42) == int.MaxValue); // since length2 is omitted
    /// Assert(jagged[0][0] == agentSmith);
    /// Assert(jagged[42][142] == agentSmith);
    /// Assert(jagged.GetOrNone(100, 2).IsNone);
    /// 
    /// FunVec2&lt;Agent> jagged = new(agentSmith, Some(50), Some&lt;Func&lt;int, int>>(_ => 2));
    /// Assert(jagged.Length1 == 50);
    /// Assert(jagged.Length2(42) == 2);
    /// Assert(jagged[0][0] == agentSmith);
    /// Assert(jagged[42][1] == agentSmith);
    /// Assert(jagged.GetOrNone(0, 2).IsNone);
    /// Assert(jagged.GetOrNone(50, 0).IsNone);
    /// 
    /// FunVec2&lt;Agent> jagged = new(agentSmith, Some(50), Some&lt;Func&lt;int, int>>(i => i));
    /// Assert(jagged.Length1 == 50);
    /// Assert(jagged.Length2(0) == 0);
    /// Assert(jagged.Length2(42) == 42);
    /// Assert(jagged[42][1] == agentSmith);
    /// Assert(jagged.GetOrNone(1, 0) == Some(agentSmith));
    /// Assert(jagged.GetOrNone(0, 0).IsNone);
    /// Assert(jagged.GetOrNone(50, 0).IsNone);
    /// </code>
    /// </summary>
    /// <param name="constantValue">Constant value that every position of the jagged array will return.</param>
    /// <param name="length1">Optional length of the jagged array; will default to None (int.MaxValue) when omitted.</param>
    /// <param name="getLength2">Optional function (i -> length) to get length of the i-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any index.</param>
    public FunVec2(T constantValue, Opt<int> length1 = default, Opt<Func<int, int>> getLength2 = default)
        : this(length1.Match(l => () => l, () => int.MaxValue),
              getLength2.UnwrapOr(GetInfiniteLen2),
              (i, j) => constantValue, Some(constantValue),
              () => Enumerable.Range(0, length1.UnwrapOr(int.MaxValue))
                    .Select(i => new FunVec1<T>(constantValue, Some(getLength2.UnwrapOr(GetInfiniteLen2)(i)))))
    { }
    /// <summary>
    /// 2-dimensional jagged collection with optional lengths, values of which are determined by the <paramref name="getValue"/> function.
    /// 
    /// <code>
    /// static int GetDistance(int from, int to) { return Math.Abs(to - from); }
    /// 
    /// FunVec2&lt;int> distances = new(GetDistance);
    /// Assert(distances.Length1 == int.MaxValue);    // since length1 is omitted
    /// Assert(distances.Length2(100) == int.MaxValue); // since length2 is omitted
    /// Assert(distances[1, 2] == 1);
    /// Assert(distances[10, 5] == 5);
    /// 
    /// FunVec2&lt;int> distancesUpTo4 = new(GetDistance, Some(4));
    /// Assert(distancesUpTo4.Length1 == 4);
    /// Assert(distancesUpTo4[3, 1] == 2);
    /// // Assert(distancesUpTo4[5, 1] == 4); // out-of-bounds, throws!
    /// Assert(distancesUpTo4.GetOrNone(5, 1).IsNone);
    /// 
    /// FunVec2&lt;int> distancesUpTo4 = new(GetDistance, Some(4), Some&lt;Func&lt;int, int>>(i => 2*i));
    /// Assert(distancesUpTo4.Length1 == 4);
    /// Assert(distancesUpTo4.Length2(2) == 4);
    /// Assert(distancesUpTo4[3, 5] == 2);
    /// // Assert(distancesUpTo4[3, 6] == 3); // out-of-bounds, throws!
    /// // Assert(distancesUpTo4[4, 0] == 4); // out-of-bounds, throws!
    /// Assert(distancesUpTo4.GetOrNone(3, 6).IsNone);  // since Length2(3) is 6, index 6 is out of bounds.
    /// Assert(distancesUpTo4.GetOrNone(4, 0).IsNone);  // since Length1 is 4, index 4 is out of bounds.
    /// </code>
    /// </summary>
    /// <param name="getValue">Function (index -> value) that returns the value of the element at the given index-th position.</param>
    /// <param name="length1">Optional length of the jagged array; will default to None (int.MaxValue) when omitted.</param>
    /// <param name="getLength2">Optional function (i -> length) to get length of the i-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any index.</param>
    public FunVec2(Func<int, int, T> getValue, Opt<int> length1 = default, Opt<Func<int, int>> getLength2 = default)
        : this(length1.Match(l => () => l, () => int.MaxValue),
              getLength2.UnwrapOr(GetInfiniteLen2),
              getValue, None<T>(),
              () => Enumerable.Range(0, length1.UnwrapOr(int.MaxValue))
                    .Select(i => new FunVec1<T>(j => getValue(i, j), Some(getLength2.UnwrapOr(GetInfiniteLen2)(i)))))
    { }
    /// <summary>
    /// 2-dimensional jagged collection lengths and values of which are determined by the underlying <paramref name="array"/>.
    /// 
    /// <code>
    /// var array = new char[][] { new char[] { 'a', 'b', 'c' }, new char[] { 'd' } };
    /// FunVec2&lt;char> jagged = new(array);
    /// Assert(jagged.Length1 == 2);
    /// Assert(jagged.Length2(0) == 3);
    /// Assert(jagged.Length2(1) == 1);
    /// Assert(jagged[0, 2] == 'c');
    /// Assert(jagged.GetOrNone(1, 0) == Some('d'));
    /// Assert(jagged.GetOrNone(0, 3).IsNone);
    /// Assert(jagged.GetOrNone(2, 0).IsNone);
    /// </code>
    /// </summary>
    /// <param name="array">Underlying array of the unified jagged collection.</param>
    public FunVec2(T[][] array)
        : this(() => array.Length,
                i => array[i].Length,
                (i, j) => array[i][j], None<T>(),
                () => Enumerable.Range(0, array.Length).Select(i => new FunVec1<T>(array[i])))
    { }
    /// <summary>
    /// 2-dimensional jagged collection lengths and values of which are determined by the underlying <paramref name="list"/>.
    /// 
    /// <code>
    /// var list = new List&lt;List&lt;char>>() { new() { 'a', 'b', 'c' }, new() { 'd' } };
    /// FunVec2&lt;char> jagged = new(list);
    /// Assert(jagged.Length1 == 2);
    /// Assert(jagged.Length2(0) == 3);
    /// Assert(jagged.Length2(1) == 1);
    /// Assert(jagged[0, 2] == 'c');
    /// Assert(jagged.GetOrNone(1, 0) == Some('d'));
    /// Assert(jagged.GetOrNone(0, 3).IsNone);
    /// Assert(jagged.GetOrNone(2, 0).IsNone);
    /// </code>
    /// </summary>
    /// <param name="list">Underlying list of the unified jagged collection.</param>
    public FunVec2(List<List<T>> list)
        : this(() => list.Count, i => list[i].Count,
                  (i, j) => list[i][j], None<T>(),
                  () => Enumerable.Range(0, list.Count).Select(i => new FunVec1<T>(list[i])))
    { }
    // helper
    internal static int GetInfiniteLen2(int _i)
        => int.MaxValue;


    // access
    /// <summary>
    /// Safely gets the element at the i-th position; returns None if the index is invalid.
    /// 
    /// <code>
    /// var underlyingArray = new int[] { 10, 11, 12 };
    /// FunVec1&lt;int> vec = new(underlyingArray);
    /// 
    /// Assert(jagvecged.Get(1) == Some(11));
    /// Assert(vec.Get(-1).IsNone);
    /// Assert(vec.Get(2).IsNone);
    /// </code>
    /// 
    /// For other methods on the resulting optional, see <see cref="Opt{T}"/>.
    /// </summary>
    /// <param name="i">First index of the element to be retrieved.</param>
    /// <param name="j">Second index of the element to be retrieved.</param>
    public Opt<T> Get(int i, int j)
    {
        if (i >= 0 && i < Length1() && j >= 0 && j < Length2(i))
            return Some(GetValueByIndex(i, j));
        else
            return None<T>();
    }
    /// <summary>
    /// Directly returns the element at the i-th position.
    /// Use <see cref="Get"/> for the bound-checked optional version.
    /// 
    /// <code>
    /// var underlyingArray = new int[] { 10, 11, 12 };
    /// FunVec1&lt;int> vec = new(underlyingArray);
    /// 
    /// Assert(vec[1] == 11);
    /// 
    /// // var x = vec[-1]; => out-of-bounds, throws!
    /// // var x = vec[3]; => out-of-bounds, throws!
    /// </code>
    /// </summary>
    /// <param name="i">First index of the element to be retrieved.</param>
    /// <param name="j">Second index of the element to be retrieved.</param>
    public T this[int i, int j]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetValueByIndex(i, j);
    }


    // enumerable
    /// <summary>
    /// Returns the enumerator for sub-vectors in the vector.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<FunVec1<T>> GetEnumerator()
        => AsEnumerable().GetEnumerator();
    /// <summary>
    /// Returns the enumerator for sub-vectors in the vector.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();


    // implicit
    /// <summary>
    /// <inheritdoc cref="FunVec2{T}.FunVec2(T, Opt{int}, Opt{Func{int, int}})"/>
    /// </summary>
    /// <param name="constantValue"></param>
    public static implicit operator FunVec2<T>(T constantValue) => new(constantValue);
    /// <summary>
    /// <inheritdoc cref="FunVec2{T}.FunVec2(Func{int, int, T}, Opt{int}, Opt{Func{int, int}})"/>
    /// </summary>
    /// <param name="getValueByIndex"></param>
    public static implicit operator FunVec2<T>(Func<int, int, T> getValueByIndex) => new(getValueByIndex);
    /// <summary>
    /// <inheritdoc cref="FunVec2{T}.FunVec2(T[][])"/>
    /// </summary>
    /// <param name="array"></param>
    public static implicit operator FunVec2<T>(T[][] array) => new(array);
    /// <summary>
    /// <inheritdoc cref="FunVec2{T}.FunVec2(List{List{T}})"/>
    /// </summary>
    /// <param name="list"></param>
    public static implicit operator FunVec2<T>(List<List<T>> list) => new(list);
}
