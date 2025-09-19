using System.Diagnostics;

namespace necronomicon.model;

internal ref struct FieldPathList
{
    private const int BasePointer = 1;

    private readonly Span<int> _origin;
    private Span<int> _stack;
    private int _stackPointer;

    public readonly bool IsDone => _stack[0] >= 0;
    public readonly int Length => _stackPointer;
    public readonly Span<int> CurrentStack => _stack.Slice(1, _stackPointer);

    public ref int this[int offset]
    {
        get
        {
            Debug.Assert((uint)offset <= (uint)_stackPointer - BasePointer);
            Debug.Assert(_stack[0] == -1);

            var index = _stackPointer - offset;
            return ref _stack[index];
        }
    }

    public FieldPathList(Span<int> stack)
    {
        Debug.Assert(stack.Length > 2);

        _origin = stack;
        _stack = stack;

        _stackPointer = 1;
    }

    public void Init()
    {
        _stack[0] = -1;
        _stack[BasePointer] = -1;
    }

    public void Push(int value = 0)
    {
        Debug.Assert(_stack[0] == -1);

        _stack[++_stackPointer] = value;
    }

    public int Pop()
    {
        Debug.Assert(_stack[0] == -1);

        return _stack[_stackPointer--];
    }

    public void Grow(int count = 1)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_stack[0] == -1);

        var sp = _stackPointer;
        var start = sp + 1;

        _stack.Slice(start, count).Clear();
        _stackPointer = sp + count;
    }

    public void Shrink(int count = 1)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_stack[0] == -1);

        _stackPointer -= count;
    }

    public void SizeTo(int count)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_stack[0] == -1);

        var sp = _stackPointer;
        var change = count - sp;
        if (change > 0)
        {
            // Growing
            _stack.Slice(sp + 1, change).Clear();
        }

        _stackPointer = count;
    }

    public void Copy()
    {
        Debug.Assert(_stack[0] == -1);

        var sp = _stackPointer;
        var length = sp + 1;

        var next = _stack.Slice(length);
        _stack[..length].CopyTo(next);

        _stack[0] = sp;
        _stack = next;
    }

    public void Done()
    {
        Debug.Assert(_stack[0] == -1);

        var sp = _stackPointer;

        _stack[0] = 0;
    }

    public readonly Enumerator GetEnumerator() => new Enumerator(_origin);

    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<int> _stack;
        private int _current;
        private int _length;

        internal Enumerator(ReadOnlySpan<int> stack)
        {
            _stack = stack;
            _current = 0;
            _length = 0;
        }

        public readonly ReadOnlySpan<int> Current => _stack.Slice(_current, _length);

        public bool MoveNext()
        {
            var current = _current + _length;
            _current = current + 1;
            _length = _stack[current];

            if (_length <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
