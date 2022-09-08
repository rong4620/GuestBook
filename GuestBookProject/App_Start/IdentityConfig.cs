using GuestBookProject.EntityModel;
using GuestBookProject.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GuestBookProject
{
    /// <inheritdoc />
    /// <summary>
    /// Class EmailService.
    /// </summary>
    public class EmailService : IIdentityMessageService
    {
        /// <inheritdoc />
        /// <summary>
        /// This method should send the message
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Class SmsService.
    /// </summary>
    /// <seealso cref="T:Microsoft.AspNet.Identity.IIdentityMessageService" />
    public class SmsService : IIdentityMessageService
    {
        /// <inheritdoc />
        /// <summary>
        /// This method should send the message
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your sms service here to send a text message.
            return Task.FromResult(0);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Class ApplicationUserManager.
    /// </summary>
    /// <seealso cref="T:System.Int32" />
    public class ApplicationUserManager : UserManager<IdentityUser, int>
    {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="store">The IUserStore is responsible for commiting changes via the UpdateAsync/CreateAsync methods</param>
        public ApplicationUserManager(IUserStore<IdentityUser, int> store)
            : base(store)
        {
        }

        /// <summary>
        /// Creates the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="context">The context.</param>
        /// <returns>ApplicationUserManager.</returns>
        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options,
            IOwinContext context)
        {
            var manager = new ApplicationUserManager(new UserStore(context.Get<GuestBookProjectContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<IdentityUser, int>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 4,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug in here.
            manager.RegisterTwoFactorProvider("PhoneCode", new PhoneNumberTokenProvider<IdentityUser, int>
            {
                MessageFormat = "Your security code is: {0}"
            });
            manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider<IdentityUser, int>
            {
                Subject = "SecurityCode",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<IdentityUser, int>(dataProtectionProvider.Create("IMP Identity token"));
            }
            return manager;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Class ApplicationSignInManager.
    /// </summary>
    /// <seealso cref="T:System.Int32" />
    public class ApplicationSignInManager : SignInManager<IdentityUser, int>
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IMP.Web.ApplicationSignInManager" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="authenticationManager">The authentication manager.</param>
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager) :
            base(userManager, authenticationManager)
        { }

        /// <inheritdoc />
        /// <summary>
        /// Called to generate the ClaimsIdentity for the user, override to add additional claims before SignIn
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;ClaimsIdentity&gt;.</returns>
        public override Task<ClaimsIdentity> CreateUserIdentityAsync(IdentityUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        /// <summary>
        /// Creates the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="context">The context.</param>
        /// <returns>ApplicationSignInManager.</returns>
        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }

    // Configure the RoleManager used in the application. RoleManager is defined in the ASP.NET Identity core assembly
    /// <inheritdoc />
    /// <summary>
    /// Class ApplicationRoleManager.
    /// </summary>
    /// <seealso cref="T:System.Int32" />
    public class ApplicationRoleManager : RoleManager<IdentityRole, int>
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:GuestBookProject.ApplicationRoleManager" /> class.
        /// </summary>
        /// <param name="roleStore">The role store.</param>
        public ApplicationRoleManager(IRoleStore<IdentityRole, int> roleStore)
            : base(roleStore)
        {
        }

        /// <summary>
        /// Creates the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="context">The context.</param>
        /// <returns>ApplicationRoleManager.</returns>
        public static ApplicationRoleManager Create(IdentityFactoryOptions<ApplicationRoleManager> options, IOwinContext context)
        {
            return new ApplicationRoleManager(new RoleStore(context.Get<GuestBookProjectContext>()));
        }
    }
}