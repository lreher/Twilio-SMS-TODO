[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(TODOList.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(TODOList.App_Start.NinjectWebCommon), "Stop")]

namespace TODOList.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;
    using Domain.Abstract;
    using Domain.Concrete.EF;
    using Domain.Concrete.Twillio;
    using Domain.Entity.Identity;
    using Microsoft.AspNet.Identity.Owin;
    using Microsoft.AspNet.Identity;
    using Domain.Concrete.Owin;

    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel) {
            kernel.Bind<AbstractTodoListRepository>().To<EFTodoListRepository>();
            kernel.Bind<AbstractTodoTaskRepository>().To<EFTodoTaskRepository>();

            kernel.Bind<AbstractSMSRepository>().To<EFSMSRepository>();
            kernel.Bind<AbstractSMSParser>().To<TwillioSMSParser>();
            kernel.Bind<AbstractSMSSender>().To<TwillioSMSSender>();

            kernel.Bind<UserManager<ApplicationUser, long>>().ToMethod(context => {
                return HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }).InRequestScope();

            kernel.Bind<SignInManager<ApplicationUser, long>>().ToMethod(context => {
                return HttpContext.Current.GetOwinContext().Get<ApplicationSignInManager>();
            }).InRequestScope();
        }        
    }
}
