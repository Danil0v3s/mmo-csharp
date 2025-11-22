namespace Core.Timer;

public class BinaryHeap<T>
{
    private int _max;
    private int _len;
    private T[] _data;

    /// <summary>
    /// Returns the internal array of values.
    /// </summary>
    public T[] Data
    {
        get => _data;
        set => _data = value;
    }

    /// <summary>
    /// Returns the length of the vector.
    /// </summary>
    public int Length => _len;

    /// <summary>
    /// Returns the capacity of the vector.
    /// </summary>
    public int Capacity => _max;

    public BinaryHeap(int capacity)
    {
        _max = capacity;
        _len = 0;
        _data = new T[capacity];
    }

    /// <summary>
    /// Returns the value at the target index.
    /// Assumes the index exists.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T this[int index]
    {
        get => _data[index];
        set => _data[index] = value;
    }

    /// <summary>
    /// Returns the first value of the vector.
    /// Assumes the array is not empty.
    /// </summary>
    public T First => _data[0];

    /// <summary>
    /// Returns the last value of the vector.
    /// Assumes the array is not empty.
    /// </summary>
    public T Last => _data[_len - 1];

    /// <summary>
    /// Finds an entry in an array.
    /// ex: ARR_FIND(0, size, i, list[i] == target);
    /// </summary>
    /// <param name="start">Starting index (ex: 0)</param>
    /// <param name="end">End index (ex: size of the array)</param>
    /// <param name="var">Index variable</param>
    /// <param name="cmp">Expression that returns true when the target entry is found</param>
    public void Find(int start, int end, out int var, Predicate<T> cmp)
    {
        for (var = start; var < end; ++var)
        {
            if (cmp(_data[var]))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Initializes a vector.
    /// </summary>
    public void Init()
    {
        _max = 0;
        _len = 0;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _data = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    /// <summary>
    /// Moves an entry of the array.
    /// Use ARR_MOVERIGHT/ARR_MOVELEFT if __from and __to are direct numbers.
    /// ex: ARR_MOVE(i, 0, list, int);// move index i to index 0
    /// </summary>
    /// <param name="from">Initial index of the entry</param>
    /// <param name="to">Target index of the entry</param>
    public void Move(int from, int to)
    {
        if (from != to)
        {
            T[] backup = new T[_data.Length];
            Array.Copy(_data, from, backup, 0, 1);

            if (from < to)
            {
                Array.Copy(_data, from + 1, _data, from, to - from);
            }
            else if (from > to)
            {
                Array.Copy(_data, to, _data, to + 1, from - to);
            }

            Array.Copy(backup, 0, _data, to, 1);
        }
    }

    /// <summary>
    /// Moves an entry of the array to the right.
    /// ex: ARR_MOVERIGHT(1, 4, list, int);// move index 1 to index 4
    /// </summary>
    /// <param name="from">Initial index of the entry</param>
    /// <param name="to">Target index of the entry</param>
    public void MoveRight(int from, int to)
    {
        T[] backup = new T[_data.Length];
        Array.Copy(_data, from, backup, 0, 1);
        Array.Copy(_data, from + 1, _data, from, to - from);
        Array.Copy(backup, 0, _data, to, 1);
    }

    /// <summary>
    /// Moves an entry of the array to the left.
    /// ex: ARR_MOVELEFT(3, 0, list, int);// move index 3 to index 0
    /// </summary>
    /// <param name="from">Initial index of the entry</param>
    /// <param name="to">Target index of the entry</param>
    public void MoveLeft(int from, int to)
    {
        T[] backup = new T[_data.Length];
        Array.Copy(_data, from, backup, 0, 1);
        Array.Copy(_data, to, _data, to + 1, from - to);
        Array.Copy(backup, 0, _data, to, 1);
    }

    /// <summary>
    /// Resizes the vector.
    /// Excess values are discarded, new positions are zeroed.
    /// </summary>
    /// <param name="n">Size</param>
    public void Resize(int n)
    {
        if (n > _max)
        {
            // increase size
            if (_max == 0)
            {
                // Allocate new
                _data = new T[n];
            }
            else
            {
                // Reallocate
                Array.Resize(ref _data, n);
            }

            // Update capacity
            _max = n;
        }
        else if (n == 0 && _max != 0)
        {
            // clear vector
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _data = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            _max = 0;
            _len = 0;
        }
        else if (n < _max)
        {
            // reduce size
            Array.Resize(ref _data, n);

            // Update capacity
            _max = n;
            if (_len > n)
            {
                // Update length
                _len = n;
            }
        }
    }

    /// <summary>
    /// Clears the vector, freeing allocated data.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_data, 0, _len);
        _max = 0;
        _len = 0;
    }

    /// <summary>
    /// Resets the length and clears content, so the vector is empty
    /// </summary>
    public void Reset()
    {
        if (_len > 0)
        {
            Array.Clear(_data, 0, _len);
        }

        _len = 0;
    }

    /// <summary>
    /// Ensures that the array has the target number of empty positions.
    /// Increases the capacity in multiples of __step.
    /// </summary>
    /// <param name="n">Empty positions</param>
    /// <param name="step">Increase</param>
    public void Ensure(int n, int step)
    {
        int empty = _max - _len;
        if (n > empty)
        {
            while (n > empty)
            {
                empty += step;
            }

            Resize(empty + _len);
        }
    }

    /// <summary>
    /// Inserts a value in the end of the vector. (using the '=' operator)
    /// Assumes there is enough capacity.
    /// </summary>
    /// <param name="value"></param>
    private void PushInternal(T value)
    {
        _data[_len] = value;
        ++_len;
    }

    /// <summary>
    /// Removes and returns the last value of the vector.
    /// Assumes the array is not empty.
    /// </summary>
    public T Pop()
    {
        return _data[--_len];
    }

    public void Insert(int index, T value)
    {
        if (index < _len)
        {
            Array.Copy(_data, index, _data, index + 1, _len - index);
        }

        _data[index] = value;
        _len++;
    }

    public void InsertCopy(int index, T value)
    {
        InsertArray(index, new T[] { value }, 1);
    }

    public void InsertArray(int index, T[] values, int n)
    {
        if (index < _len)
        {
            Array.Copy(_data, index, _data, index + n, _len - index);
        }

        Array.Copy(values, 0, _data, index, n);
        _len += n;
    }

    public void Erase(int index)
    {
        EraseN(index, 1);
    }

    public void EraseN(int index, int n)
    {
        Array.Copy(_data, index + n, _data, index, _len - (index + n));
        _len -= n;
    }

    public void Remove(int index, int n)
    {
        if (index < 0 || index >= _len || n <= 0)
        {
            return;
        }

        if (index + n > _len)
        {
            n = _len - index;
        }

        Array.Copy(_data, index + n, _data, index, _len - (index + n));
        Array.Clear(_data, _len - n, n);
        _len -= n;
    }

    /// <summary>
    /// Returns the top value of the heap.
    /// Assumes the heap is not empty.
    /// </summary>
    /// <returns>Value at the top</returns>
    public T Peek()
    {
        return Data[0];
    }

    /// <summary>
    /// Inserts a value in the heap. (using the '=' operator)
    /// Assumes there is enough capacity.
    ///
    /// The comparator takes two values as arguments, returns:
    /// - negative if the first value is on the top
    /// - positive if the second value is on the top
    /// - 0 if they are equal
    /// </summary>
    /// <param name="val"></param>
    /// <param name="topCmp"></param>
    public void BHeapPush(T val, Func<T, T, long> topCmp)
    {
        var i = Length;

        PushInternal(val);

        while (i != 0)
        {
            var parent = (i - 1) / 2;
            if (topCmp(Data[parent], Data[i]) < 0)
            {
                break;
            }

            SwapInternal(parent, i);
            i = parent;
        }
    }

    /// <summary>
    /// Version used by A* implementation, matching client bheap.
    /// </summary>
    /// <param name="val"></param>
    /// <param name="topCmp"></param>
    public void BHeapPush2(T val, Func<T, T, long> topCmp)
    {
        var i = Length;
        PushInternal(val);
        BHeapSiftDown(0, i, topCmp);
    }

    /// <summary>
    /// Removes the top value of the heap. (using the '=' operator)
    /// Assumes the heap is not empty.
    ///
    /// The comparator takes two values as arguments, returns:
    /// - negative if the first value is on the top
    /// - positive if the second value is on the top
    /// - 0 if they are equal
    /// </summary>
    /// <param name="topCmp"></param>
    public void BHeapPop(Func<T, T, long> topCmp)
    {
        BHeapPopIndex(0, topCmp);
    }

    /// <summary>
    /// Version used by A* implementation, matching client bheap.
    /// </summary>
    /// <param name="topCmp"></param>
    public void BHeapPop2(Func<T, T, long> topCmp)
    {
        Data[0] = Pop(); // put last at index
        if (Length == 0) // removed last, nothing to do
        {
            return;
        }

        BHeapSiftUp(0, topCmp);
    }

    /// <summary>
    /// Removes the target value of the heap. (using the '=' operator)
    /// Assumes the index exists.
    ///
    /// The comparator takes two values as arguments, returns:
    /// - negative if the first value is on the top
    /// - positive if the second value is on the top
    /// - 0 if they are equal
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="topCmp"></param>
    public void BHeapPopIndex(long idx, Func<T, T, long> topCmp)
    {
        long i = idx;
        Data[idx] = Pop(); // put last at index
        if (i >= Length) // removed last, nothing to do
        {
            return;
        }

        while (i != 0)
        {
            // restore heap property in parents
            long parent = (i - 1) / 2;
            if (topCmp(Data[parent], Data[i]) < 0)
            {
                break; // done
            }

            //swap(Data[parent], Data[i]);
            SwapInternal(parent, i);
            i = parent;
        }

        while (i < Length)
        {
            // restore heap property in childs
            long lchild = i * 2 + 1;
            long rchild = i * 2 + 2;
            if ((lchild >= Length || topCmp(Data[i], Data[lchild]) <= 0) &&
                (rchild >= Length || topCmp(Data[i], Data[rchild]) <= 0))
            {
                // done
                break;
            }
            else if (rchild >= Length || topCmp(Data[lchild], Data[rchild]) <= 0)
            {
                // left child
                SwapInternal(i, lchild);
                i = lchild;
            }
            else
            {
                // right child
                SwapInternal(i, rchild);
                i = rchild;
            }
        }
    }

    /// <summary>
    /// Follow path up towards (but not all the way to) the root, swapping nodes until finding
    /// a place where the new item that was placed at __idx fits.
    /// Only goes as high as __startidx (usually 0).
    /// </summary>
    /// <param name="startId"></param>
    /// <param name="id"></param>
    /// <param name="topCmp"></param>
    public void BHeapSiftDown(long startId, long id, Func<T, T, long> topCmp)
    {
        var i = id;
        while (i > startId)
        {
            var parent = (i - 1) / 2;
            if (topCmp(Data[parent], Data[i]) <= 0)
            {
                break;
            }

            SwapInternal(parent, i);
            i = parent;
        }
    }

    /// <summary>
    /// Swap two items in the underlying array.
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    private void SwapInternal(long index1, long index2)
    {
        (Data[index2], Data[index1]) = (Data[index1], Data[index2]);
    }

    /// <summary>
    /// Repeatedly swap the smaller child with parent, after placing a new item at __idx.
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="topCmp"></param>
    public void BHeapSiftUp(long idx, Func<T, T, long> topCmp)
    {
        long i = idx;
        long lchild = i * 2 + 1;

        while (lchild < Length)
        {
            // restore heap property in childs
            long rchild = i * 2 + 2;
            if (rchild >= Length || topCmp(Data[lchild], Data[rchild]) < 0)
            {
                // left child
                SwapInternal(i, lchild);
                i = lchild;
            }
            else
            {
                // right child
                SwapInternal(i, rchild);
                i = rchild;
            }

            lchild = i * 2 + 1;
        }

        BHeapSiftDown(idx, i, topCmp);
    }

    /// <summary>
    /// Call this after modifying the item at __idx__ to restore the heap
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="topCmp"></param>
    public void BHeapUpdate(long idx, Func<T, T, long> topCmp)
    {
        BHeapSiftDown(0, idx, topCmp);
        BHeapSiftUp(idx, topCmp);
    }
}