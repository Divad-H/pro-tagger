using System;
using System.Diagnostics.CodeAnalysis;

namespace ProTagger
{
    public abstract class Variant
    {
        protected readonly object _value;

        protected Variant(object value)
            => _value = value;
        
        public object Value => _value;

        public virtual int VariantIndex { get; } = 0;

        public override abstract bool Equals(object? obj);
        public override abstract int GetHashCode();
    }

    public class Variant<T1, T2> : Variant, IEquatable<Variant>, IEquatable<Variant<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
        public Variant(T1 value) 
            : base(value)
        {}

        public Variant(T2 value)
            : base(value)
        {}

        public TRet Visit<TRet>(Func<T1, TRet> f1, Func<T2, TRet> f2)
        {
            if (_value is T1 t1)
                return f1(t1);
            return f2(Get<T2>());
        }

        public void Visit(Action<T1> f1, Action<T2> f2)
        {
            if (_value is T1 t1)
                f1(t1);
            else
                f2(Get<T2>());
        }

        public T Get<T>()
        {
            if (_value is T t)
              return t;
            throw new ArgumentException("Generic argument does not match the value.", nameof(T));
        }

        public object Get()
            => _value;

        public T1 First => (T1)Value;

        public T2 Second => (T2)Value;

        public bool Is<T>()
            => _value is T;

        public override bool Equals(object? obj)
        {
            if (!(obj is Variant<T1, T2> other))
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
            => Value.GetHashCode();

        public bool Equals(Variant? obj)
        {
            if (!(obj is Variant<T1, T2> other))
                return false;
            return Equals(other);
        }

        public bool Equals(Variant<T1, T2>? obj)
            => !(obj is null) && Value.Equals(obj.Value);

        public override int VariantIndex => _value is T1 ? 0 : 1;

        public static bool operator ==(Variant<T1, T2>? first, Variant<T1, T2>? second)
            => first is null ? second is null : first.Equals(second);

        public static bool operator !=(Variant<T1, T2>? first, Variant<T1, T2>? second)
            => !(first == second);
    }
}
