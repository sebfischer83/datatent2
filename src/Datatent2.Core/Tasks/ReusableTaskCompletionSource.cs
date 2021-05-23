//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Datatent2.Core.Tasks
//{
//    internal sealed class ReusableTaskCompletionSource<T> : INotifyCompletion
//    {
//        private Action? _continuation = null;
//        private T? _result;
//        private Exception? _exception = null;

//        public ReusableTaskCompletionSource<T> Task => this;

//        public bool IsCompleted
//        {
//            get;
//            private set;
//        }

//        public T? GetResult()
//        {
//            if (_exception != null)
//                throw _exception;
//            return _result;
//        }

//        public void OnCompleted(Action continuation)
//        {
//            if (_continuation != null)
//                throw new InvalidOperationException("This ReusableAwaiter instance has already been listened");
//            _continuation = continuation;
//        }

//        public void SetResult(T result)
//        {
//            if (!this.IsCompleted)
//            {
//                this.IsCompleted = true;
//                _result = result;

//                if (_continuation != null)
//                    _continuation();
//            }            
//        }

//        public ReusableTaskCompletionSource<T> Reset()
//        {
//            _result = default(T);
//            _continuation = null;
//            _exception = null;
//            IsCompleted = false;
//            return this;
//        }

//        public ReusableTaskCompletionSource<T> GetAwaiter()
//        {
//            return this;
//        }
//    }
//}
