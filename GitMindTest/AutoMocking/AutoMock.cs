using System;
using System.Linq;
using Autofac;
using Autofac.Features.ResolveAnything;
using GitMindTest.AutoMocking.Private;
using Moq;


namespace GitMindTest.AutoMocking
{
	public class AutoMock : IDisposable
	{
		private readonly Lazy<IContainer> container;
		private readonly ContainerBuilder builder;
		private readonly AutofacMoqSource autofacMoqSource = new AutofacMoqSource();

		private bool disposed;


		public AutoMock()
			: this(new ContainerBuilder())
		{
		}


		public AutoMock(ContainerBuilder builder)
		{
			this.builder = builder;

			builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
			builder.RegisterSource(autofacMoqSource);

			container = new Lazy<IContainer>(() => builder.Build());
		}


		private IContainer Container => container.Value;

		private ContainerBuilder Builder
		{
			get
			{
				if (container.IsValueCreated)
				{
					throw new InvalidOperationException(
						"Cannot register after first use of Resolve<T>() or Mock<T>()");
				}

				return builder;
			}
		}


		public T Resolve<T>() => container.Value.Resolve<T>();


		public T Resolve<TArg1, T>(TArg1 arg1) =>
			Container.Resolve<Func<TArg1, T>>()(arg1);

		public T Resolve<TArg1, TArg2, T>(TArg1 arg1, TArg2 arg2) =>
			Container.Resolve<Func<TArg1, TArg2, T>>()(arg1, arg2);


		public T Resolve<TArg1, TArg2, TArg3, T>(TArg1 arg1, TArg2 arg2, TArg3 arg3) =>
			Container.Resolve<Func<TArg1, TArg2, TArg3, T>>()(arg1, arg2, arg3);


		public Mock<T> Mock<T>()
			where T : class
		{
			IMocked<T> obj = (IMocked<T>)Resolve<T>();
			return obj.Mock;
		}


		public void VerifyAll() => Container.Resolve<MockRepository>().VerifyAll();


		/// <summary>
		/// Register the specified Mock and mocked instance to overide already 
		/// registered type.
		/// </summary>
		public AutoMock RegisterMock<T>()
			where T : class
		{
			Mock<T> mock = new Mock<T>(MockBehavior.Strict);

			RegisterAsSingleInstance(Builder, mock);
			RegisterAsSingleInstance(Builder, mock.Object);

			return this;
		}

		public AutoMock RegisterType<T>()
			where T : class
		{
			RegisterTypes(Builder, new[] { typeof(T) });
			return this;
		}


		public AutoMock RegisterSingleInstance<T>(T instance)
			where T : class
		{
			Builder.RegisterInstance(instance)
				.As<T>()
				.AsSelf()
				.SingleInstance();

			RegisterAsSingleInstance(Builder, instance);
			return this;
		}

		public AutoMock RegisterNamespaceOf<T>()
		{
			string nameSpace = typeof(T).Namespace;

			var types = typeof(T).Assembly.GetTypes().Where(type =>
				type.Namespace != null &&
				nameSpace != null &&
				type.Namespace.StartsWith(nameSpace, StringComparison.Ordinal)).ToArray();

			RegisterTypes(Builder, types);

			return this;
		}

		public AutoMock RegisterAssemblyOf<T>()
		{
			RegisterTypes(Builder, typeof(T).Assembly.GetTypes());
			return this;
		}


		public void Dispose()
		{
			autofacMoqSource.Dispose();
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		private void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					Container.Dispose();
				}

				disposed = true;
			}
		}


		private static void RegisterAsSingleInstance(ContainerBuilder builder, object instance)
		{
			builder.RegisterInstance(instance)
				.AsImplementedInterfaces()
				.AsSelf()
				.SingleInstance();
		}


		private static void RegisterTypes(ContainerBuilder builder, Type[] types)
		{
			builder.RegisterTypes(types)
				.Where(t => !IsSingleInstance(t))
				.AsImplementedInterfaces()
				.AsSelf();

			builder.RegisterTypes(types)
				.Where(IsSingleInstance)
				.AsImplementedInterfaces()
				.AsSelf()
				.SingleInstance();
		}


		private static bool IsSingleInstance(Type type) =>
			type.GetCustomAttributes(true)
			.Any(t => t.GetType().Name == "SingleInstanceAttribute");
	}
}
