using System;

namespace procom_tagger
{
    public class Variant<T1, T2>
        where T1 : notnull
        where T2 : notnull
    {
        private object _value;

        public Variant(T1 value)
        {
             _value = value;
        }

        public Variant(T2 value)
        {
            _value = value;
        }

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
        {
            return _value;
        }

        public bool Is<T>()
        {
            return _value is T;
        }

        public Variant<T1, T2> Assign(T1 value)
        {
            _value = value;
            return this;
        }

        public Variant<T1, T2> Assign(T2 value)
        {
            _value = value;
            return this;
        }
    }
}
