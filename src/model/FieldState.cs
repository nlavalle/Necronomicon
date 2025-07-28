namespace necronomicon.model;

public class FieldState
{
    private object[] state;

    public FieldState()
    {
        state = new object[8];
    }

    public object? Get(FieldPath fp)
    {
        var x = this;
        int z = 0;

        for (int i = 0; i <= fp.Last; i++)
        {
            z = fp.Path[i];

            if (x.state.Length < z + 2)
                return null;

            if (i == fp.Last)
                return x.state[z];

            if (x.state[z] is not FieldState next)
                return null;

            x = next;
        }

        return null;
    }

    public void Set(FieldPath fp, object v)
    {
        var x = this;
        for (int i = 0; i <= fp.Last; i++)
        {
            int z = fp.Path[i];
            if (x.state.Length < z + 2)
                ResizeState(x, z + 2);

            if (i == fp.Last)
            {
                if (x.state[z] is not FieldState)
                    x.state[z] = v;

                return;
            }

            if (x.state[z] is not FieldState child)
            {
                child = new FieldState();
                x.state[z] = child;
            }

            x = child;
        }
    }

    private void ResizeState(FieldState fs, int minLength)
    {
        int newSize = Math.Max(minLength, fs.state.Length * 2);
        var newArray = new object[newSize];
        Array.Copy(fs.state, newArray, fs.state.Length);
        fs.state = newArray;
    }
}
