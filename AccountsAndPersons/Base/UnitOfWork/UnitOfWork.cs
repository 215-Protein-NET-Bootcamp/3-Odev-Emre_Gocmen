using System;

namespace AccountsAndPersons
{
    public class UnitOfWork : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
