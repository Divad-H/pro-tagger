using ReacitveMvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTagger.Wpf
{
    public class EnumViewModel<TEnum> : IDisposable where TEnum : Enum
    {
        public List<TEnum> Values { get; }

        public ViewSubject<TEnum> Value { get; }

        public EnumViewModel(TEnum value)
        {
            Values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
            Value = new ViewSubject<TEnum>(value);
        }

        public void Dispose()
            => Value.Dispose();
    }
}
