using System.Collections;
using System.Collections.Generic;

namespace ProTagger.Utilities
{
    public interface IBatchList
    {
        void Modify(IEnumerable itemsToAdd, IEnumerable itemsToRemove, bool supportsRangeOperations);
        void ModifyNoDuplicates(IEnumerable itemsToAdd, IEnumerable itemsToRemove, bool supportsRangeOperations);
    }
    public interface IBatchList<T>
    {
        void Modify(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove, bool supportsRangeOperations);
        void ModifyNoDuplicates(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove, bool supportsRangeOperations);
    }
}
