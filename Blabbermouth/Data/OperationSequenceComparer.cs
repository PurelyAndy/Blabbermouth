using System.Collections.Generic;

namespace Blabbermouth.Data;

public class OperationSequenceComparer : IEqualityComparer<OperationSequence>
{
    public bool Equals(OperationSequence? x, OperationSequence? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.EquivalentTo(y);
    }

    public int GetHashCode(OperationSequence obj)
    {
        return obj.GetHashCodeForEquivalence();
    }
}