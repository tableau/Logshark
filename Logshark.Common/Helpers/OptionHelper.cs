using Optional;
using System;

namespace Logshark.Common.Helpers
{
    public static class OptionHelper
    {
        public static Option<TR> Reduce<T1, T2, TR>(Option<T1> first,
                                                    Option<T2> second,
                                                    Func<T1, T2, TR> reducer)
        {
            return first.FlatMap(f => second.Map(s => reducer(f, s)));
        }

        public static Option<TR> Reduce<T1, T2, TR>(Option<T1> first,
                                                    Option<T2> second,
                                                    Func<T1, T2, Option<TR>> reducer)
        {
            return first.FlatMap(f => second.FlatMap(s => reducer(f, s)));
        }

        public static Option<TR> Reduce<T1, T2, T3, TR>(Option<T1> first,
                                                        Option<T2> second,
                                                        Option<T3> third,
                                                        Func<T1, T2, T3, TR> reducer)
        {
            return first.FlatMap(f => second.FlatMap(s => third.Map(t => reducer(f, s, t))));
        }

        public static Option<TR> Reduce<T1, T2, T3, TR>(Option<T1> first,
                                                        Option<T2> second,
                                                        Option<T3> third,
                                                        Func<T1, T2, T3, Option<TR>> reducer)
        {
            return first.FlatMap(f => second.FlatMap(s => third.FlatMap(t => reducer(f, s, t))));
        }
    }
}