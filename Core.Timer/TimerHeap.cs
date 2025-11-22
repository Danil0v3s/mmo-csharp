namespace Core.Timer;

/// <summary>
/// Binary min-heap for efficient timer queue management
/// </summary>
internal class TimerHeap
{
    private int[] _heap;
    private int _length;
    private readonly TimerData[] _timerData;

    public TimerHeap(TimerData[] timerData, int initialCapacity = 256)
    {
        _timerData = timerData;
        _heap = new int[initialCapacity];
        _length = 0;
    }

    public int Length => _length;

    public int Peek()
    {
        if (_length == 0)
            throw new InvalidOperationException("Heap is empty");
        return _heap[0];
    }

    public void Push(int timerId)
    {
        EnsureCapacity(_length + 1);
        _heap[_length] = timerId;
        SiftUp(_length);
        _length++;
    }

    public int Pop()
    {
        if (_length == 0)
            throw new InvalidOperationException("Heap is empty");

        int result = _heap[0];
        _length--;
        if (_length > 0)
        {
            _heap[0] = _heap[_length];
            SiftDown(0);
        }
        return result;
    }

    public void PopIndex(int index)
    {
        if (index < 0 || index >= _length)
            throw new ArgumentOutOfRangeException(nameof(index));

        _length--;
        if (index < _length)
        {
            _heap[index] = _heap[_length];
            
            // Try sifting down first
            int oldValue = _heap[index];
            SiftDown(index);
            
            // If position didn't change, try sifting up
            if (_heap[index] == oldValue)
                SiftUp(index);
        }
    }

    public int FindIndex(int timerId)
    {
        for (int i = 0; i < _length; i++)
        {
            if (_heap[i] == timerId)
                return i;
        }
        return -1;
    }

    public void Clear()
    {
        _length = 0;
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _heap.Length)
            return;

        int newCapacity = _heap.Length;
        while (newCapacity < required)
            newCapacity += 256;

        Array.Resize(ref _heap, newCapacity);
    }

    private void SiftUp(int index)
    {
        int child = index;
        int item = _heap[child];

        while (child > 0)
        {
            int parent = (child - 1) / 2;
            int parentItem = _heap[parent];

            if (Compare(item, parentItem) >= 0)
                break;

            _heap[child] = parentItem;
            child = parent;
        }

        _heap[child] = item;
    }

    private void SiftDown(int index)
    {
        int parent = index;
        int item = _heap[parent];
        int halfLength = _length / 2;

        while (parent < halfLength)
        {
            int child = 2 * parent + 1;
            int childItem = _heap[child];

            int rightChild = child + 1;
            if (rightChild < _length && Compare(_heap[rightChild], childItem) < 0)
            {
                child = rightChild;
                childItem = _heap[child];
            }

            if (Compare(item, childItem) <= 0)
                break;

            _heap[parent] = childItem;
            parent = child;
        }

        _heap[parent] = item;
    }

    private long Compare(int tid1, int tid2)
    {
        return _timerData[tid1].Tick - _timerData[tid2].Tick;
    }
}

