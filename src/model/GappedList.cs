using System.Collections;
using System.Numerics;

namespace necronomicon.model;

public class GappedList<T>
    : IList<T>
    where T : class, new()
{
    private const uint ReasonableMaximum = 1024 * 1024;

    private T?[] _expandoArray;

    private int _count = 0;

    public int Capacity => _expandoArray.Length;

    public int Count => _count;

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get
        {
            var array = GetterSetterPreamble(index);

            var value = array[index];
            if (value is null)
            {
                value = new T();
                array[index] = value;
                _count++;
            }

            return value;
        }

        set
        {
            var array = GetterSetterPreamble(index);

            if (array[index] is null)
                _count++;

            array[index] = value;
        }
    }

    private T?[] GetterSetterPreamble(int index)
    {
        if ((uint)index >= ReasonableMaximum)
            throw new ArgumentOutOfRangeException(nameof(index));

        var array = _expandoArray;
        if (index >= array.Length)
        {
            var capacity = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);

            var from = array;
            array = new T[capacity];

            from.AsSpan().CopyTo(array);

            _expandoArray = array;
        }

        return array;
    }

    public GappedList()
        : this(16) { }

    public GappedList(int capacity)
    {
        if ((uint)capacity >= ReasonableMaximum)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        capacity = (int)BitOperations.RoundUpToPowerOf2((uint)capacity);
        _expandoArray = new T?[capacity];
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(_expandoArray, item);
    }

    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        if ((uint)index < (uint)Capacity)
        {
            _expandoArray[index] = null;
            _count--;
        }
    }

    public void Add(T item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        _expandoArray.AsSpan().Clear();
    }

    public bool Contains(T item)
    {
        return _expandoArray.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);

        if ((uint)index < (uint)Capacity)
        {
            _expandoArray[index] = null;
            _count--;

            return true;
        }

        return false;
    }

    public struct Enumerator
        : IEnumerator<T>
    {
        private readonly T?[] _array;

        private int _count;

        private int _index;

        public Enumerator(T?[] array, int count)
        {
            _array = array;
            _count = count;
            _index = -1;
        }

        public T Current => _array[_index]!;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            var count = _count;
            if (count != 0)
            {
                var array = _array;
                var index = _index;

                while (++index < _array.Length)
                {
                    if (array[index] is not null)
                    {
                        _count = count - 1;
                        _index = index;

                        return true;
                    }
                }
            }

            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_expandoArray, _count);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }
}
