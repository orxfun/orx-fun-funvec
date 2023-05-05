using System.Collections;
using System.Runtime.CompilerServices;

namespace Orx.Fun.FunVec;

/// <summary>
/// A unified functional 4-dimensional collection.
/// 
/// It does not hold the data, rather provides a unified view over different sources of data providing indexed access to collection elements.
/// </summary>
/// <typeparam name="T">Type of the innermost elements of the collection.</typeparam>
public class FunVec4<T> : IEnumerable<FunVec3<T>>
{
    // data
    readonly Func<int, int, int, int, T> GetValueByIndex;
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
    readonly Func<IEnumerable<FunVec3<T>>> AsEnumerable;
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
    /// Length of the jagged collection in the third dimension.
    /// 
    /// <code>
    /// var array = new int[2][][]
    /// {
    ///     new int[3][]
    ///     {
    ///         new int[2] { 0, 1 },
    ///         new int[3] { 2, 3, 4 },
    ///         Array.Empty&lt;int>(),
    ///     },
    ///     new int[1][]
    ///     {
    ///         new int[4] { 0, 1, 2, 3 },
    ///     }
    /// };
    /// UniJaggedD3&lt;int> jagged = new(array);
    /// Assert(jagged.Length3(0, 2) == 0);
    /// </code>
    /// </summary>
    public readonly Func<int, int, int> Length3;
    /// <summary>
    /// Length of the jagged collection in the third dimension.
    /// 
    /// <code>
    /// var array = new int[2][][]
    /// {
    ///     new int[3][]
    ///     {
    ///         new int[2] { 0, 1 },
    ///         new int[3] { 2, 3, 4 },
    ///         Array.Empty&lt;int>(),
    ///     },
    ///     new int[1][]
    ///     {
    ///         new int[4] { 0, 1, 2, 3 },
    ///     }
    /// };
    /// UniJaggedD3&lt;int> jagged = new(array);
    /// Assert(jagged.Length3(0, 2) == 0);
    /// </code>
    /// </summary>
    public readonly Func<int, int, int, int> Length4;
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
    FunVec4(Func<int> length1, Func<int, int> length2, Func<int, int, int> length3, Func<int, int, int, int> length4,
        Func<int, int, int, int, T> getValueByIndex,
        Opt<T> underlyingConstantValue,
        Func<IEnumerable<FunVec3<T>>> asEnumerable)
    {
        Length1 = length1;
        Length2 = length2;
        Length3 = length3;
        Length4 = length4;
        GetValueByIndex = getValueByIndex;
        UnderlyingScalar = underlyingConstantValue;
        AsEnumerable = asEnumerable;
    }
    /// <summary>
    /// 4-dimensional jagged collection with optional lengths, which always yields the same constant value.
    /// 
    /// <para>See <see cref="FunVec2{T}.FunVec2(T, Opt{int}, Opt{Func{int, int}})"/> for two-dimensional examples.</para>
    /// </summary>
    /// <param name="constantValue">Constant value that every position of the jagged array will return.</param>
    /// <param name="length1">Optional length of the jagged array; will default to None (int.MaxValue) when omitted.</param>
    /// <param name="getLength2">Optional function (i -> length) to get length of the i-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any index.</param>
    /// <param name="getLength3">Optional function (i,j -> length) to get length of the (i,j)-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any indices.</param>
    /// <param name="getLength4">Optional function (i,j,k -> length) to get length of the (i,j,k)-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any indices.</param>
    public FunVec4(T constantValue, Opt<int> length1 = default, Opt<Func<int, int>> getLength2 = default,
        Opt<Func<int, int, int>> getLength3 = default, Opt<Func<int, int, int, int>> getLength4 = default)
        : this(length1.Match(l => () => l, () => int.MaxValue),
              getLength2.UnwrapOr(FunVec2<T>.GetInfiniteLen2),
              getLength3.UnwrapOr(FunVec3<T>.GetInfiniteLen3),
              getLength4.UnwrapOr(GetInfiniteLen4),
              (i, j, k, l) => constantValue, Some(constantValue),
              () => Enumerable.Range(0, length1.UnwrapOr(int.MaxValue))
                    .Select(i => new FunVec3<T>(constantValue, Some(getLength2.UnwrapOr(FunVec2<T>.GetInfiniteLen2)(i)))))
    { }
    /// <summary>
    /// 4-dimensional jagged collection with optional lengths, values of which are determined by the <paramref name="getValue"/> function.
    /// 
    /// <para>See <see cref="FunVec2{T}.FunVec2(Func{int, int, T}, Opt{int}, Opt{Func{int, int}})"/> for two-dimensional examples.</para>
    /// </summary>
    /// <param name="getValue">Function (indices -> value) that returns the value of the element at the given position.</param>
    /// <param name="length1">Optional length of the jagged array; will default to None (int.MaxValue) when omitted.</param>
    /// <param name="getLength2">Optional function (i -> length) to get length of the i-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any index.</param>
    /// <param name="getLength3">Optional function (i,j -> length) to get length of the (i,j)-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any indices.</param>
    /// <param name="getLength4">Optional function (i,j,k -> length) to get length of the (i,j,k)-th collection; when omitted will default to None which will yield to a length of int.MaxValue for any indices.</param>
    public FunVec4(Func<int, int, int, int, T> getValue, Opt<int> length1 = default, Opt<Func<int, int>> getLength2 = default,
        Opt<Func<int, int, int>> getLength3 = default, Opt<Func<int, int, int, int>> getLength4 = default)
        : this(length1.Match(l => () => l, () => int.MaxValue),
              getLength2.UnwrapOr(FunVec2<T>.GetInfiniteLen2),
              getLength3.UnwrapOr(FunVec3<T>.GetInfiniteLen3),
              getLength4.UnwrapOr(GetInfiniteLen4),
              getValue, None<T>(),
              () => Enumerable.Range(0, length1.UnwrapOr(int.MaxValue))
                    .Select(i => new FunVec3<T>((j, k, l) => getValue(i, j, k, l), Some(getLength2.UnwrapOr(FunVec2<T>.GetInfiniteLen2)(i)))))
    { }
    /// <summary>
    /// 4-dimensional jagged collection lengths and values of which are determined by the underlying <paramref name="array"/>.
    /// 
    /// <para>See <see cref="FunVec2{T}.FunVec2(T[][])"/> for two-dimensional examples.</para>
    /// </summary>
    /// <param name="array">Underlying array of the unified jagged collection.</param>
    public FunVec4(T[][][][] array)

        : this(() => array.Length,
              i => array[i].Length,
              (i, j) => array[i][j].Length,
              (i, j, k) => array[i][j][k].Length,
              (i, j, k, l) => array[i][j][k][l], None<T>(),
              () => Enumerable.Range(0, array.Length)
                    .Select(i => new FunVec3<T>((j, k, l) => array[i][j][k][l], Some(array[i].Length))))
    { }
    /// <summary>
    /// 4-dimensional jagged collection lengths and values of which are determined by the underlying <paramref name="list"/>.
    /// 
    /// <para>See <see cref="FunVec2{T}.FunVec2(List{List{T}})"/> for two-dimensional examples.</para>
    /// </summary>
    /// <param name="list">Underlying list of the unified jagged collection.</param>
    public FunVec4(List<List<List<List<T>>>> list)

        : this(() => list.Count,
              i => list[i].Count,
              (i, j) => list[i][j].Count,
              (i, j, k) => list[i][j][k].Count,
              (i, j, k, l) => list[i][j][k][l], None<T>(),
              () => Enumerable.Range(0, list.Count)
                    .Select(i => new FunVec3<T>((j, k, l) => list[i][j][k][l], Some(list[i].Count))))
    { }


    // helper
    internal static int GetInfiniteLen4(int _i, int _j, int _k)
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
    /// <param name="k">Third index of the element to be retrieved.</param>
    /// <param name="l">Fourth index of the element to be retrieved.</param>
    public Opt<T> Get(int i, int j, int k, int l)
    {
        if (i >= 0 && i < Length1() && j >= 0 && j < Length2(i) && k >= 0 && k < Length3(i, j))
            return Some(GetValueByIndex(i, j, k, l));
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
    /// <param name="k">Third index of the element to be retrieved.</param>
    /// <param name="l">Fourth index of the element to be retrieved.</param>
    public T this[int i, int j, int k, int l]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetValueByIndex(i, j, k, l);
    }


    // enumerable
    /// <summary>
    /// Returns the enumerator for sub-vectors in the vector.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<FunVec3<T>> GetEnumerator()
        => AsEnumerable().GetEnumerator();
    /// <summary>
    /// Returns the enumerator for sub-vectors in the vector.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();


    // implicit
    /// <summary>
    /// <inheritdoc cref="FunVec4{T}.FunVec4(T, Opt{int}, Opt{Func{int, int}}, Opt{Func{int, int, int}}, Opt{Func{int, int, int, int}})"/>
    /// </summary>
    /// <param name="constantValue"></param>
    public static implicit operator FunVec4<T>(T constantValue) => new(constantValue);
    /// <summary>
    /// <inheritdoc cref="FunVec4{T}.FunVec4(Func{int, int, int, int, T}, Opt{int}, Opt{Func{int, int}}, Opt{Func{int, int, int}}, Opt{Func{int, int, int, int}})"/>
    /// </summary>
    /// <param name="getValueByIndex"></param>
    public static implicit operator FunVec4<T>(Func<int, int, int, int, T> getValueByIndex) => new(getValueByIndex);
    /// <summary>
    /// <inheritdoc cref="FunVec4{T}.FunVec4(T[][][][])"/>
    /// </summary>
    /// <param name="array"></param>
    public static implicit operator FunVec4<T>(T[][][][] array) => new(array);
    /// <summary>
    /// <inheritdoc cref="FunVec4{T}.FunVec4(List{List{List{List{T}}}})"/>
    /// </summary>
    /// <param name="list"></param>
    public static implicit operator FunVec4<T>(List<List<List<List<T>>>> list) => new(list);
}
