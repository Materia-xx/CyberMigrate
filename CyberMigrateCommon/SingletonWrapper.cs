using System;

namespace CyberMigrateCommom
{
    /// <summary>
    /// Lazy generic singleton
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonWrapper<T>
    {
        private T instance;
        private Func<T> constructor;

// mcbtodo: just convert this to use actual Lazy<T> if possible
        public T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = constructor();
                }
                return instance;
            }
        }

        public SingletonWrapper(Func<T> constructionFunction)
        {
            constructor = constructionFunction;
        }
    }
}
