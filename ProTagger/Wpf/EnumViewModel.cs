using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProTagger.Wpf
{
    public class EnumViewModel<TEnum> : INotifyPropertyChanged where TEnum : Enum
    {
        private TEnum _value;
        public TEnum Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<TEnum>.Default.Equals(_value, value))
                    return;
                _value = value;
                NotifyPropertyChanged();
            }
        }

        public List<TEnum> Values { get; }

        public IObservable<TEnum> ValueObservable { get; }

        public EnumViewModel(TEnum value)
        {
            _value = value;
            Values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
            ValueObservable = this.FromProperty(vm => vm.Value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
