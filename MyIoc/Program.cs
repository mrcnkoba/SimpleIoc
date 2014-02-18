
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using PK.Container;

namespace MyIoc
{
    class Program
    {
        private static void Main(string[] args)
        {
            IContainer container = new MyContainer();
            container.Register(typeof(Concrete));
            var obj = container.Resolve<Interface>();
            obj.DoWork();
        }
    }

    class Concrete : Interface
    {
        public void DoWork()
        {
            Console.WriteLine("WORK");
        }
    }

    interface Interface
    {
        void DoWork();
    }

    public class MyContainer : IContainer
    {
        private readonly Dictionary<Type, Type> _dictionary;
        private readonly Dictionary<Type, object> _factory;

        public MyContainer()
        {
            this._dictionary = new Dictionary<Type, Type>();
            this._factory = new Dictionary<Type, Object>();
        }

        public void Register(Assembly assembly)
        {
            var classes = assembly.GetTypes().Where(t => t.IsClass && t.IsPublic && !t.IsAbstract);

            foreach (var @class in classes)
            {
                this.RegisterHelper(@class);
            }
        }

        public void Register(Type type)
        {
            this.RegisterHelper(type);
        }

        public void Register<T>(T impl) where T : class
        {
            var type = impl.GetType();

            var interfaces = type.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                this._factory.Add(@interface, impl);
            }
        }

        public void Register<T>(Func<T> provider) where T : class
        {
            var type = provider().GetType();

            this.RegisterHelper(type);
        }

        public T Resolve<T>() where T : class
        {
            var type = typeof (T);
            return this.ResolveHelper(type) as T;
        }

        public object Resolve(Type type)
        {
            return this.ResolveHelper(type);
        }

        private object ResolveHelper(Type type, bool isFromConstructor = false)
        {
            if (_dictionary.Keys.Any(x => x == type))
            {
                var constructors = this._dictionary[type].GetConstructors();
                var parameters = this.GetParameters(constructors);

                return Activator.CreateInstance(_dictionary[type], parameters);
            }
            else if (_factory.Keys.Any(x => x == type))
            {
                return _factory[type];
            }
            else
            {
                if (isFromConstructor)
                {
                    throw new UnresolvedDependenciesException();
                }

                return null;
            }
        }

        private object[] GetParameters(ConstructorInfo[] constructors)
        {
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                if (parameters.All(x => x.ParameterType.IsInterface))
                {
                    var arguments = new List<Object>();

                    foreach (var argument in parameters)
                    {
                        arguments.Add(ResolveHelper(argument.ParameterType, true));
                    }

                    return arguments.ToArray();
                }
            }

            return null;
        }

        private void RegisterHelper(Type type)
        {
            var interfaces = type.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                if (this._dictionary.Keys.Any(x => x == @interface))
                {
                    this._dictionary.Remove(@interface);
                }

                _dictionary.Add(@interface, type);
            }
        }
    }
}
