using System.Diagnostics;

namespace necronomicon.model;

/*
How FieldPathList works:

This is an upward growing stack (as in a hardware stack, not just A stack), a large pool of ints is that stack.

|XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX|
 growing this way -> (X represents "don't care")

When referring to memory areas the entire pool is referred to as the origin, and the current pool is the stack.
The origin is filled with sequential (packed) stacks.

A stack is a base-pointer jump value, giving the length of the stack, followed by the values on said stack.

|J...|
 ^ here J represents the base pointer jump (... represents an undefined length)

|N...|
 ^ here N represents the base pointer jump being -1 meaning it is the current stack

|NB...|
  ^ here B represents the base pointer, this is the bottom of the stack

|NB...S|
      ^ here S represents the stack pointer, it is the current top of the stack

|NS|
  ^ when a stack is only the base pointer and a single value, B and S are the same

|NN|
  ^ when the origin is initialized, -1 and -1 are placed in the first two positions

|NBXXXS|
|NBXXXX000S|
       ^ when growing the stack the values above behave as though they are 0, this can be deferred

|NBXXXS|
|NBXXXXS|
       ^ a push is an increment of the stack pointer and a setting of that value, at the same time

|NBXXXXXS|
|NBXS|
    ^ when shrinking the stack the values above become undefined and inaccessible and the stack pointer is moved

|NBXXXXXS|
|NBXXXXS?|
        ^ a pop is a decrement of the stack pointer and a return of the value in the previous stack position

|NBXXXS|
|NBXXXSNXXXXX|
       ^ when a stack is copied, the entire length is moved to the end (S+1)

|JBXXXSNXXXXX|
 ^ when a stack is complete, the stack pointer is placed into the base pointer jump (J)

|JXXXXXNBXXXS|
       ^ the reference frame for the stack is then moved forward

|0B...S|
 ^ when an origin is marked as complete, 0 is placed in J indicating that the jump is 0 (no further movement is possible)

|1X2XX2XX3XXX4XXXX3XXX3XXX2XX2XX2XX2XX1X0|
 ^ when the origin is complete, it ends up looking something like this

|JB....|
 ^ to read the stacks back, J is read and now represents the length of the stack

|B....|
 ^ the stack is returned as the base pointer ranging to the length

|B...S|
     ^ at any time S can be recovered by taking the length - 1

|0|
 ^ if J is ever 0, the end of the origin is reached
*/

internal ref struct FieldPathList
{
    private const int BasePointer = 1;

    private readonly Span<int> _origin;
    private Span<int> _stack;
    private int _stackPointer;

    public readonly bool IsDone => _stack[BasePointer - 1] >= 0;
    public readonly int Length => _stackPointer;
    public readonly Span<int> CurrentStack => _stack.Slice(BasePointer, _stackPointer);

    public ref int this[int offset]
    {
        get
        {
            Debug.Assert((uint)offset <= (uint)_stackPointer - BasePointer);
            Debug.Assert(_stack[BasePointer - 1] == -1);

            return ref _stack[_stackPointer - offset];
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
        _stack[BasePointer - 1] = -1;
        _stack[BasePointer] = -1;
    }

    public void Push(int value = 0)
    {
        Debug.Assert(_stack[BasePointer - 1] == -1);

        _stack[++_stackPointer] = value;
    }

    public int Pop()
    {
        Debug.Assert(_stack[BasePointer - 1] == -1);

        return _stack[_stackPointer--];
    }

    public void Grow(int count = 1)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_stack[BasePointer - 1] == -1);

        var sp = _stackPointer;

        _stack.Slice(sp + 1, count).Clear();
        _stackPointer = sp + count;
    }

    public void Shrink(int count = 1)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_stack[BasePointer - 1] == -1);

        _stackPointer -= count;
    }

    public void SizeTo(int count)
    {
        Debug.Assert(count > 0);
        Debug.Assert(_stack[BasePointer - 1] == -1);

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
        Debug.Assert(_stack[BasePointer - 1] == -1);

        var sp = _stackPointer;
        var length = sp + 1;

        var next = _stack.Slice(length);
        _stack[..length].CopyTo(next);

        _stack[BasePointer - 1] = sp;
        _stack = next;
    }

    public void Done()
    {
        Debug.Assert(_stack[BasePointer - 1] == -1);

        _stack[BasePointer - 1] = 0;
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
