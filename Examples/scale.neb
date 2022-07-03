
///// Scale helper class and utilities /////

public class Scale : ScriptBase
{
    int _count = 0;
    List<int> _scaleNotes;
    int[] _noteWeights;
    int _totalWeight = 100;
    int _down = 0;
    int _up = 0;

    public Scale(string scale, string root, int octDown, int octUp)
    {
        _scaleNotes = GetNotesFromString($"{root}.{scale}");
        _count = _scaleNotes.Count();
        _noteWeights = new int[_count];
        _down = octDown;
        _up = octUp;

        // Set default weights.
        for(int i = 0; i < _count; i++)
        {
           _noteWeights[i] = 100 / _count;
        }        
    }

    public void setWeight(int index, int weight)
    {
        if(index < _count)
        {
            _noteWeights[index] = weight;
        }

        // Recalc total weight.
        _totalWeight = 0;
        for(int i = 0; i < _count; i++)
        {
            _totalWeight += _noteWeights[i];
        }        
    }

    public int randomNote()
    {
        int note = 0;
        int r = Random(_totalWeight);

        int offset = 0;

        for(int i = 0; i < _count; i++)
        {
            offset += _noteWeights[i];
            if(r < offset)
            {
                note = _scaleNotes[i];
                break;
            }
        }

        // Which octave?
        int oct = Random(0, 3) - 1;
        note += oct * 12;

        return note;
    }
}
