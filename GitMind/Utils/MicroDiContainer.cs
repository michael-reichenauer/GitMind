using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace GitMind.Utils
{
	/// <summary>
	/// MicroDIContainer is a simple dependency injection container similar to e.g. AutoFac.
	/// This can be used to remove dependency on an external DI container 
	/// </summary>
	internal class MicroDiContainer
	{
		// Contains all registered types and their factory methods. Multiple different factories are
		// supported for one type, which is useful for supporting "IEnumerable<T>" parameters
		private readonly Dictionary<Type, List<Func<object>>> registeredTypes =
			new Dictionary<Type, List<Func<object>>>();


		/// <summary>
		/// Resolves the specified type.
		/// </summary>
		public T Resolve<T>()
		{
			return (T)ResolveInstance(typeof(T));
		}


		/// <summary>
		/// Registers the types.
		/// </summary>
		public void RegisterTypes(IEnumerable<Type> types)
		{
			foreach (var type in types)
			{
				if (type.IsClass && !registeredTypes.ContainsKey(type))
				{
					// Register the class type
					Func<object> provider = GetInstanceProvider(type);
					Register(type, provider);

					// Register all the interfaces this class implements
					Type[] interfaceTypes = type.GetInterfaces();
					foreach (Type interfaceType in interfaceTypes)
					{
						Register(interfaceType, provider);
					}
				}
			}
		}


		/// <summary>
		/// Gets the instance provider for the type.
		/// </summary>
		private Func<object> GetInstanceProvider(Type type)
		{
			ConstructorInfo constructor = null;
			Type[] parameterTypes = null;

			// Select the constructor with most parameters
			foreach (ConstructorInfo currentConstructor in type.GetConstructors())
			{
				ParameterInfo[] currentParameterInfos = currentConstructor.GetParameters();
				if (constructor == null || parameterTypes.Length < currentParameterInfos.Length)
				{
					constructor = currentConstructor;
					parameterTypes = currentConstructor.GetParameters()
						.Select(p => p.ParameterType)
						.ToArray();
				}
			}

			// The provider function, which will start with resolving each parameter
			Func<object> provider = () =>
			{
				object[] parameters = parameterTypes
					.Select(parameterType => ResolveInstance(parameterType))
					.ToArray();

				// Create the instance by calling the constructor with all resolved parameters
				return constructor.Invoke(parameters);
			};

			// Support the [SingleInstance] attribute
			if (null != type.GetCustomAttributes(false)
				.FirstOrDefault(a => a.GetType().Name == "SingleInstanceAttribute"))
			{
				// Wrapping the instance provide in a Lazy<> to ensure provide is only run once
				Lazy<object> singleInstanceProvider = new Lazy<object>(provider);
				provider = () => singleInstanceProvider.Value;
			}

			return provider;
		}


		/// <summary>
		/// Registers the specified type and an instance provider for the type.
		/// </summary>
		private void Register(Type type, Func<object> provider)
		{
			List<Func<object>> providers;
			if (!registeredTypes.TryGetValue(type, out providers))
			{
				providers = new List<Func<object>>();
				registeredTypes[type] = providers;
			}

			providers.Add(provider);
		}


		/// <summary>
		/// Resolves an instance for the specified type.
		/// </summary>
		private object ResolveInstance(Type type)
		{
			List<Func<object>> providers;
			if (IsLazy(type))
			{
				// The type is a Lazy<T>, lets resolve the provider for T
				Type genericArgument = type.GetGenericArguments()[0];

				if (registeredTypes.TryGetValue(genericArgument, out providers))
				{
					Func<object> provider = providers.Last();

					// Create a Lazy<T> provider, which resolves the instance of T once when user needs it
					return DynamicCreateLazy(genericArgument, provider);
				}
			}

			if (IsEnumerable(type))
			{
				// The type is an IEnumerable<T>, lets return all providers for type T
				Type genericArgument = type.GetGenericArguments()[0];
				if (registeredTypes.TryGetValue(genericArgument, out providers))
				{
					IEnumerable<object> instances = providers.Select(provider => provider());
					return DynamicCastToTypedList(genericArgument, instances);
				}
				else
				{
					// No registered types, return empty list
					return DynamicCastToTypedList(genericArgument, Enumerable.Empty<object>());
				}
			}
			else if (registeredTypes.TryGetValue(type, out providers))
			{
				// Return a created object
				Func<object> provider = providers.Last();
				return provider();
			}

			throw Asserter.FailFast("Not supported type " + type.FullName);
		}

		private static bool IsEnumerable(Type type)
		{
			return
				type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		}


		private static bool IsLazy(Type type)
		{
			return
				type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof(Lazy<>);
		}



		/// <summary>
		/// Dynamically casts a IEnumerable'object' to a typed IEnumerable'T'.
		/// </summary>
		private static object DynamicCastToTypedList(Type type, IEnumerable<object> list)
		{
			BindingFlags methodBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

			MethodInfo castMethod = typeof(MicroDiContainer)
				.GetMethod(nameof(ToTypedList), methodBindingFlags)
				.MakeGenericMethod(type);
			return castMethod.Invoke(null, new object[] { list });
		}

		/// <summary>
		/// This function is used by DynamicCastToTypedList using reflection
		/// Cast a untyped list to a typed list. 
		/// </summary>
		private static IEnumerable<T> ToTypedList<T>(IEnumerable<object> list)
		{
			return list.Cast<T>();
		}

		private static object DynamicCreateLazy(Type type, Func<object> provider)
		{
			BindingFlags methodBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

			MethodInfo toLazyMethod = typeof(MicroDiContainer)
				.GetMethod(nameof(ToTypedLazy), methodBindingFlags)
				.MakeGenericMethod(type);
			return toLazyMethod.Invoke(null, new object[] { provider });
		}

		/// <summary>
		/// This function is used by DynamicCastToLazy using reflection
		/// Creates a Lazy'T'
		/// </summary>
		private static Lazy<T> ToTypedLazy<T>(Func<object> provider)
		{
			return new Lazy<T>(() => (T)provider());
		}
	}
}