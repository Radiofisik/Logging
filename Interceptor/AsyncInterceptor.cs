using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Interceptor
{
    public abstract class AsyncInterceptor: IInterceptor
    {
        protected abstract void InterceptSync(IInvocation invocation);

        protected abstract Task InterceptAsync(IInvocation invocation, Type methodReturnType);

        void IInterceptor.Intercept(IInvocation invocation)
        {
            if (!typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
            {
                InterceptSync(invocation);
                return;
            }
            try
            {
                var method = invocation.Method;

                if ((method != null) && typeof(Task).IsAssignableFrom(method.ReturnType))
                {
                    Task.Factory.StartNew(
                        async () => { await InterceptAsync(invocation, method.ReturnType).ConfigureAwait(true); }
                        , CancellationToken.None).Wait();
                }
            }
            catch (Exception ex)
            {
                //this is not really burring the exception
                //excepiton is going back in the invocation.ReturnValue which 
                //is a Task that failed. with the same excpetion 
                //as ex.
            }
        }
    }
}
