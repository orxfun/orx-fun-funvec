using System.Collections;
using System.Runtime.CompilerServices;

namespace Orx.Fun.FunVec;

/// <summary>
/// A unified functional 1-dimensional collection.
/// 
/// It does not hold the data, rather provides a unified view over different sources of data providing indexed access to collection elements.
/// </summary>
/// <typeparam name="T">Type of the innermost elements of the collection.</typeparam>
public class FunVec1<T> : IEnumerable<T>
{
    // data
    readonly Func<int, T> GetValueByIndex;
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
    readonly Func<IEnumerable<T>> AsEnumerable;
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
    FunVec1(Func<int> length1, Func<int, T> getValueByIndex, Opt<T> underlyingConstantValue, Func<IEnumerable<T>> asEnumerable)
    {
        Length1 = length1;
        GetValueByIndex = getValueByIndex;
        UnderlyingScalar = underlyingConstantValue;
        AsEnumerable = asEnumerable;
    }
    /// <summary>
    /// 1-dimensional vector with optional length, which always yields the same constant value.
    /// 
    /// <code>
    /// var agentSmith = GetSmith();
    /// FunVec1&lt;Agent> vec = new(agentSmith);
    /// Assert(vec.Length1 == int.MaxValue); // since length1 is omitted
    /// Assert(vec[0] == agentSmith);
    /// Assert(vec[42] == agentSmith);
    /// Assert(vec.Get(100) == Some(agentSmith));
    /// 
    /// FunVec1&lt;Agent> vec = new(agentSmith, Some(50));
    /// Assert(vec.Length1 == 50);
    /// Assert(vec[0] == agentSmith);
    /// Assert(vec[42] == agentSmith);
    /// Assert(vec.Get(100).IsNone);
    /// </code>
    /// </summary>
    /// <param name="constantValue">Constant value that every position of the jagged array will return.</param>
    /// <param name="length1">Optional length of the jagged array; will default to None (int.MaxValue) when omitted.</param>
    public FunVec1(T constantValue, Opt<int> length1 = default)
        : this(length1.Match(l => () => l, () => int.MaxValue),
              _ => constantValue, Some(constantValue),
              () => Enumerable.Range(0, length1.UnwrapOr(int.MaxValue)).Select(_ => constantValue))
    { }
    /// <summary>
    /// 1-dimensional vector with optional length, values of which are determined by the <paramref name="getValueByIndex"/> function.
    /// 
    /// <code>
    /// static int Factorial(int number) { .. }
    /// 
    /// FunVec1&lt;int> factorials = new(Factorial);
    /// Assert(factorials.Length1 == int.MaxValue); // since length1 is omitted
    /// Assert(factorials[3] == 6);
    /// Assert(factorials[5] == 120);
    /// 
    /// FunVec1&lt;int> factorialsUpTo4 = new(Factorial, Some(4));
    /// Assert(factorialsUpTo4.Length1 == 4);
    /// Assert(factorialsUpTo4[3] == 6);
    /// // Assert(factorialsUpTo4[5] == 120); // out-of-bounds, throws!
    /// Assert(factorialsUpTo4.Get(5).IsNone);
    /// </code>
    /// </summary>
    /// <param name="getValueByIndex">Function (index -> value) that returns the value of the element at the given index-th position.</param>
    /// <param name="length1">Optional length of the jagged array; will default to None (int.MaxValue) when omitted.</param>
    public FunVec1(Func<int, T> getValueByIndex, Opt<int> length1 = default)
        : this(length1.Match(l => () => l, () => int.MaxValue),
              getValueByIndex, None<T>(),
              () => Enumerable.Range(0, length1.UnwrapOr(int.MaxValue)).Select(getValueByIndex))
    { }
    /// <summary>
    /// 1-dimensional vector, length and values of which are determined by the underlying <paramref name="array"/>.
    /// 
    /// <code>
    /// var array = new char[] { 'a', 'b', 'c' };
    /// FunVec1&lt;char> vec = new(array);
    /// Assert(vec.Length1 == 3);
    /// Assert(vec[2] == 'c');
    /// Assert(vec.Get(0) == Some('a'));
    /// Assert(vec.Get(3).IsNone);
    /// </code>
    /// </summary>
    /// <param name="array">Underlying array.</param>
    public FunVec1(T[] array)
        : this(() => array.Length, i => array[i], None<T>(), () => array) { }
    /// <summary>
    /// 1-dimensional vector, length and values of which are determined by the underlying <paramref name="list"/>.
    /// 
    /// <code>
    /// var list = new List&lt;char> { 'a', 'b', 'c' };
    /// FunVec1&lt;char> vec = new(list);
    /// Assert(vec.Length1 == 3);
    /// Assert(vec[2] == 'c');
    /// Assert(vec.Get(0) == Some('a'));
    /// Assert(vec.Get(3).IsNone);
    /// </code>
    /// </summary>
    /// <param name="list">Underlying list.</param>
    public FunVec1(List<T> list)
        : this(() => list.Count, i => list[i], None<T>(), () => list) { }


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
    /// <param name="i">Index of the element to be retrieved.</param>
    public Opt<T> Get(int i)
    {
        if (i >= 0 && i < Length1())
            return Some(GetValueByIndex(i));
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
    /// <param name="i">Index of the element to be retrieved.</param>
    public T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetValueByIndex(i);
    }


    // enumerable
    /// <summary>
    /// Returns the enumerator for values in the vector.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
        => AsEnumerable().GetEnumerator();
    /// <summary>
    /// Returns the enumerator for values in the vector.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();


    // implicit
    /// <summary>
    /// <inheritdoc cref="FunVec1{T}.FunVec1(T, Opt{int})"/>
    /// </summary>
    /// <param name="constantValue"></param>
    public static implicit operator FunVec1<T>(T constantValue) => new(constantValue);
    /// <summary>
    /// <inheritdoc cref="FunVec1{T}.FunVec1(Func{int, T}, Opt{int})"/>
    /// </summary>
    /// <param name="getValueByIndex"></param>
    public static implicit operator FunVec1<T>(Func<int, T> getValueByIndex) => new(getValueByIndex);
    /// <summary>
    /// <inheritdoc cref="FunVec1{T}.FunVec1(T[])"/>
    /// </summary>
    /// <param name="array"></param>
    public static implicit operator FunVec1<T>(T[] array) => new(array);
    /// <summary>
    /// <inheritdoc cref="FunVec1{T}.FunVec1(List{T})"/>
    /// </summary>
    /// <param name="list"></param>
    public static implicit operator FunVec1<T>(List<T> list) => new(list);
}
