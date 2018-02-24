using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;


namespace GitMindTest.AutoMocking.Private
{
	internal class AutofacMoqSource : IRegistrationSource, IDisposable
	{
		private readonly Autofac.Extras.Moq.AutoMock autofacAutoMock;


		public AutofacMoqSource()
		{
			autofacAutoMock = Autofac.Extras.Moq.AutoMock.GetStrict();
		}


		public bool IsAdapterForIndividualComponents => false;


		public IEnumerable<IComponentRegistration> RegistrationsFor(
			Service service,
			Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
		{
			var typedService = service as TypedService;

			if (typedService == null ||
					!CanMockService(typedService) ||
					registrationAccessor(service).Any())
			{
				return Enumerable.Empty<IComponentRegistration>();
			}

			var registration = new ComponentRegistration(
				Guid.NewGuid(),
				DelegateMockActivatorFor(typedService),
				new CurrentScopeLifetime(),
				InstanceSharing.Shared, InstanceOwnership.OwnedByLifetimeScope,
				new[] { service },
				new Dictionary<string, object>());

			return new IComponentRegistration[] { registration };
		}


		public void Dispose()
		{
			autofacAutoMock.Dispose();
		}


		private DelegateActivator DelegateMockActivatorFor(IServiceWithType typedService)
		{
			return new DelegateActivator(
				typedService.ServiceType,
				(c, p) => autofacAutoMock.Container.Resolve(typedService.ServiceType));
		}

		private static bool CanMockService(IServiceWithType typedService)
		{
			return !typeof(IStartable).IsAssignableFrom(
				typedService.ServiceType) && !IsConcrete(typedService.ServiceType);
		}

		private static bool IsConcrete(Type serviceType)
		{
			return serviceType.IsClass && !serviceType.IsAbstract && !serviceType.IsInterface;
		}
	}
}