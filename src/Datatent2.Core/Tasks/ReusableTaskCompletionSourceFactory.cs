//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Datatent2.Core.Tasks
//{
//    internal static class ReusableTaskCompletionSourceFactory<T>
//    {
//        private static ConcurrentStack<ReusableTaskCompletionSource<T>> _reusableCache = new ConcurrentStack<ReusableTaskCompletionSource<T>>();

//        public static ReusableTaskCompletionSource<T> Get()
//        {
//            if (_reusableCache.TryPop(out var source))
//            {
//                return source;
//            }
//            return new ReusableTaskCompletionSource<T>();
//        }

//        public static void Return(ReusableTaskCompletionSource<T> source)
//        {
//            _reusableCache.Push(source.Reset());
//        }
//    }
//}
