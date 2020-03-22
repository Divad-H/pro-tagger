using System.Collections;
using System.Collections.Generic;

namespace ProTagger.Utilities
{
    public interface IBatchList
    {
        void Modify(IEnumerable itemsToAdd, IEnumerable itemsToRemove);
        void ModifyNoDuplicates(IEnumerable itemsToAdd, IEnumerable itemsToRemove);
    }
    public interface IBatchList<T>
    {
        void Modify(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove);
        void ModifyNoDuplicates(IEnumerable<T> itemsToAdd, IEnumerable<T> itemsToRemove);
    }
}
