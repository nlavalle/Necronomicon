namespace necronomicon.model;

public class FieldState
{
    private List<object> state;

    public FieldState()
    {
        state = new List<object>(new object[8]);
    }

    public object Get(FieldPath fp)
    {
        var x = this;
        int z = 0;

        for (int i = 0; i <= fp.Last; i++)
        {
            z = fp.Path[i];

            if (x.state.Count < z + 2)
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
        int z = 0;

        for (int i = 0; i <= fp.Last; i++)
        {
            z = fp.Path[i];

            if (x.state.Count < z + 2)
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
        int newSize = Math.Max(minLength, fs.state.Count * 2);
        while (fs.state.Count < newSize)
            fs.state.Add(null);
    }
}
