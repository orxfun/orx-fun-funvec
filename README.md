# Orx.Fun.FunVec

Unified functional collections for C# allowing access by index.

Complete auto-generated documentation can be found here:
**[sandcastle-documentation](https://orxfun.github.io/orx-fun-funvec/index.html)**.

## Why?

* A unified collection type allowing access by index is required.
* However, built-in types such as `IList` does not provide the desired flexibility.

Consider a 2-dimensional collection of integers for instance. C# provides the following types that hold own the data:

* `int[][]`
* `List<List<int>>`
* `int[,]` (unpopular)
* `List<int[]>` (has its uses)
* `List<int>[]` (likewise)

and views on data such as:

* `Span<int>`
* `ReadOnlySpan<int>`
* `Memory<int>`


All of these types allow the desired behavior of accessing the (i,j)-th element by `collection[i][j]` or `collection[i, j]`.

But there are more.

Sometimes, we want a collection to always return the same value, say 1, without storing millions of 1's in the memory. For instance, consider the traditional shortest path problem which works on some kind of a graph with edge weights. The weights might be stored as a square matrix in (almost) fully connected cases or as a jagged array for sparse cases. So `double[][]` might be a good choice as the weights input data of such an algorithm. However, sometimes we want to find the shortest path in number of edges; in other words, all edge weights are 1. It would be not only be annoying but waste of memory to create the entire weights data where every element is 1.

In other memory-tight cases, we may want to compute the value of the (i,j)-th value on demand, rather than storing the entire data in memory at once. Assume that we are working with a complete Euclidean distance matrix of N locations in an algorithm, say traveling salesperson problem. It is appealing to pre-compute the entire NxN matrix and quickly fetch the required data throughout the algorithm. But how much memory do we need if N is one million? Sometimes it is just not possible. So maybe for a too large N, one just holds the coordinates of N locations and compute the (i,j)-th Euclidean distance whenever queried; while exact same algorithm works on a pre-computed NxN matrix for manageable N's.

## Approach

Proposed solution is simple and straightforward. Continuing on the 2-dimensional example, `vec` which is of type `FunVec2<T>` can be created from all of the following:

* `T constantValue`
    * `vec[i,j]` is `constantValue` for all (i,j) -> as in all 1's example.
    * bounds can optionally be added.
* `Func<int, int, T> getValue`
    * `vec[i,j]` is `getValue(i, j)` for all (i,j) -> as in Euclidean example.
    * bounds can optionally be added.
* `T[][] array`
    * `vec[i,j]` is `array[i][j]` for all (i,j).
    * bounds of arrays are used.
* `List<List<T>> list`
	* `vec[i,j]` is `list[i][j]` for all (i,j).
    * bounds of lists are used. 

Note that `FunVec2<T>` is not the owner of the data; it is only an immutable unified view over data which can be considered as a 2-dimensional data having access with index.

The functional vector exposes only two methods:

* `public T this[int i, int j]`
    * => directly returns T; might lead to out-of-bounds exception
* `public Opt<T> Get(int i, int j)`
    * => safely returns the optional value; None in out-of-bounds cases

Now a method having `FunVec2<T>` as its argument can use the unified view over different variants of 2-dimensional data.
