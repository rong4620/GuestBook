using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GuestBookProject.EntityModel;
using Microsoft.AspNet.Identity;

namespace GuestBookProject.Identity
{
    /// <summary>
    /// 因應使用 DataBase First 實作 Identity 的操作介面
    /// </summary>
    public class UserStore :
        IQueryableUserStore<IdentityUser, int>, IUserPasswordStore<IdentityUser, int>, IUserLoginStore<IdentityUser, int>,
        IUserClaimStore<IdentityUser, int>, IUserRoleStore<IdentityUser, int>, IUserSecurityStampStore<IdentityUser, int>,
        IUserEmailStore<IdentityUser, int>, IUserPhoneNumberStore<IdentityUser, int>, IUserTwoFactorStore<IdentityUser, int>,
        IUserLockoutStore<IdentityUser, int>
    {
        /// <summary>
        /// The database
        /// </summary>
        private readonly GuestBookProjectContext db;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserStore"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <exception cref="ArgumentNullException">db</exception>
        public UserStore(GuestBookProjectContext db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #region -- IQueryableUserStore<IdentityUser, int> --

        /// <inheritdoc />
        /// <summary>
        /// IQueryable users
        /// </summary>
        /// <value>The users.</value>
        public IQueryable<IdentityUser> Users => this.db.IdentityUsers;

        #endregion

        #region -- IUserStore<IdentityUser, Key> --

        /// <inheritdoc />
        /// <summary>
        /// Insert a new user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        public Task CreateAsync(IdentityUser user)
        {
            this.db.IdentityUsers.Add(user);
            return this.db.SaveChangesAsync();
        }

        /// <inheritdoc />
        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        public Task DeleteAsync(IdentityUser user)
        {
            this.db.IdentityUsers.Remove(user);
            return this.db.SaveChangesAsync();
        }

        /// <summary>
        /// Finds a user
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task&lt;IdentityUser&gt;.</returns>
        public Task<IdentityUser> FindByIdAsync(int userId)
        {
            return this.db.IdentityUsers
                .Include(u => u.IdentityUserLogins).Include(u => u.IdentityRoles).Include(u => u.IdentityUserClaims)
                .FirstOrDefaultAsync(u => u.Id.Equals(userId));
        }

        /// <summary>
        /// Find a user by name
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns>Task&lt;IdentityUser&gt;.</returns>
        public Task<IdentityUser> FindByNameAsync(string userName)
        {
            return this.db.IdentityUsers
                .Include(u => u.IdentityUserLogins).Include(u => u.IdentityRoles).Include(u => u.IdentityUserClaims)
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        /// <summary>
        /// Update a user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        public Task UpdateAsync(IdentityUser user)
        {
            this.db.Entry<IdentityUser>(user).State = EntityState.Modified;
            return this.db.SaveChangesAsync();
        }

        #endregion

        #region -- IUserPasswordStore<IdentityUser, Key> --

        /// <summary>
        /// Get the user password hash
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<string> GetPasswordHashAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash);
        }

        /// <summary>
        /// Returns true if a user has a password set
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> HasPasswordAsync(IdentityUser user)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        /// <summary>
        /// Set the user password hash
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="passwordHash">The password hash.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetPasswordHashAsync(IdentityUser user, string passwordHash)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserLoginStore<IdentityUser, Key> --

        /// <summary>
        /// Adds a user login with the specified provider and key
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="login">The login.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">
        /// user
        /// or
        /// login
        /// </exception>
        public Task AddLoginAsync(IdentityUser user, UserLoginInfo login)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            var userLogin = Activator.CreateInstance<IdentityUserLogin>();
            userLogin.UserId = user.Id;
            userLogin.LoginProvider = login.LoginProvider;
            userLogin.ProviderKey = login.ProviderKey;
            user.IdentityUserLogins.Add(userLogin);
            return Task.FromResult(0);
        }

        /// <summary>
        /// find as an asynchronous operation.
        /// </summary>
        /// <param name="login">The login.</param>
        /// <returns>Task&lt;IdentityUser&gt;.</returns>
        /// <exception cref="ArgumentNullException">login</exception>
        public async Task<IdentityUser> FindAsync(UserLoginInfo login)
        {
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            var provider = login.LoginProvider;
            var key = login.ProviderKey;

            var userLogin = await this.db.IdentityUserLogins.FirstOrDefaultAsync(l => l.LoginProvider == provider && l.ProviderKey == key);

            if (userLogin == null)
            {
                return default(IdentityUser);
            }

            return await this.db.IdentityUsers
                .Include(u => u.IdentityUserLogins).Include(u => u.IdentityRoles).Include(u => u.IdentityUserClaims)
                .FirstOrDefaultAsync(u => u.Id.Equals(userLogin.UserId));
        }

        /// <summary>
        /// Returns the linked accounts for this user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;IList&lt;UserLoginInfo&gt;&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult<IList<UserLoginInfo>>(user.IdentityUserLogins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey)).ToList());
        }

        /// <summary>
        /// Removes the user login with the specified combination if it exists
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="login">The login.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">
        /// user
        /// or
        /// login
        /// </exception>
        public Task RemoveLoginAsync(IdentityUser user, UserLoginInfo login)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            var provider = login.LoginProvider;
            var key = login.ProviderKey;

            var item = user.IdentityUserLogins.SingleOrDefault(l => l.LoginProvider == provider && l.ProviderKey == key);

            if (item != null)
            {
                user.IdentityUserLogins.Remove(item);
            }

            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserClaimStore<IdentityUser, int> --

        /// <summary>
        /// Add a new user claim
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="claim">The claim.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// user
        /// or
        /// claim
        /// </exception>
        public Task AddClaimAsync(IdentityUser user, Claim claim)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var item = Activator.CreateInstance<IdentityUserClaim>();
            item.UserId = user.Id;
            item.ClaimType = claim.Type;
            item.ClaimValue = claim.Value;
            user.IdentityUserClaims.Add(item);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the claims for the user with the issuer set
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;IList&lt;Claim&gt;&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<IList<Claim>> GetClaimsAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult<IList<Claim>>(user.IdentityUserClaims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList());
        }

        /// <summary>
        /// Remove a user claim
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="claim">The claim.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">
        /// user
        /// or
        /// claim
        /// </exception>
        public Task RemoveClaimAsync(IdentityUser user, Claim claim)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            foreach (var item in user.IdentityUserClaims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList())
            {
                user.IdentityUserClaims.Remove(item);
            }

            foreach (var item in this.db.IdentityUserClaims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList())
            {
                this.db.IdentityUserClaims.Remove(item);
            }

            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserRoleStore<IdentityUser, int> --

        /// <summary>
        /// Adds a user to a role
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException">roleName</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Task AddToRoleAsync(IdentityUser user, string roleName)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(WarningStr.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            var userRole = this.db.IdentityRoles.SingleOrDefault(r => r.Name == roleName);

            if (userRole == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, WarningStr.RoleNotFound, new object[] { roleName }));
            }

            user.IdentityRoles.Add(userRole);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the roles for this user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;IList&lt;System.String&gt;&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<IList<string>> GetRolesAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult<IList<string>>(user.IdentityRoles.Join(this.db.IdentityRoles, ur => ur.Id, r => r.Id, (ur, r) => r.Name).ToList());
        }

        /// <summary>
        /// Returns true if a user is in the role
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException">roleName</exception>
        public Task<bool> IsInRoleAsync(IdentityUser user, string roleName)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(WarningStr.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            return
                Task.FromResult<bool>(
                    this.db.IdentityRoles.Any(r => r.Name == roleName && r.IdentityUsers.Any(u => u.Id.Equals(user.Id))));
        }

        /// <summary>
        /// Removes the role for the user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException">roleName</exception>
        public Task RemoveFromRoleAsync(IdentityUser user, string roleName)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(WarningStr.ValueCannotBeNullOrEmpty, nameof(roleName));
            }

            var userRole = user.IdentityRoles.SingleOrDefault(r => r.Name == roleName);

            if (userRole != null)
            {
                user.IdentityRoles.Remove(userRole);
            }

            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserSecurityStampStore<IdentityUser, int> --

        /// <summary>
        /// Get the user security stamp
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<string> GetSecurityStampAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.SecurityStamp);
        }

        /// <summary>
        /// Set the security stamp for the user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="stamp">The stamp.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetSecurityStampAsync(IdentityUser user, string stamp)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserEmailStore<IdentityUser, int> --

        /// <summary>
        /// Returns the user associated with this email
        /// </summary>
        /// <param name="email">The email.</param>
        /// <returns>Task&lt;IdentityUser&gt;.</returns>
        public Task<IdentityUser> FindByEmailAsync(string email)
        {
            return this.db.IdentityUsers
                .Include(u => u.IdentityUserLogins).Include(u => u.IdentityRoles).Include(u => u.IdentityUserClaims)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Get the user email
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<string> GetEmailAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.Email);
        }

        /// <summary>
        /// Returns true if the user email is confirmed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<bool> GetEmailConfirmedAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.EmailConfirmed);
        }

        /// <summary>
        /// Set the user email
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="email">The email.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetEmailAsync(IdentityUser user, string email)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.Email = email;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets whether the user email is confirmed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="confirmed">if set to <c>true</c> [confirmed].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserPhoneNumberStore<IdentityUser, int> --

        /// <summary>
        /// Get the user phone number
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<string> GetPhoneNumberAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PhoneNumber);
        }

        /// <summary>
        /// Returns true if the user phone number is confirmed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<bool> GetPhoneNumberConfirmedAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        /// <summary>
        /// Set the user's phone number
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="phoneNumber">The phone number.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetPhoneNumberAsync(IdentityUser user, string phoneNumber)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets whether the user phone number is confirmed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="confirmed">if set to <c>true</c> [confirmed].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetPhoneNumberConfirmedAsync(IdentityUser user, bool confirmed)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserTwoFactorStore<IdentityUser, int> --

        /// <summary>
        /// Returns whether two factor authentication is enabled for the user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<bool> GetTwoFactorEnabledAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.TwoFactorEnabled);
        }

        /// <summary>
        /// Sets whether two factor authentication is enabled for the user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="enabled">if set to <c>true</c> [enabled].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetTwoFactorEnabledAsync(IdentityUser user, bool enabled)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.TwoFactorEnabled = enabled;
            return Task.FromResult(0);
        }

        #endregion

        #region -- IUserLockoutStore<IdentityUser, int> --

        /// <summary>
        /// Returns the current number of failed access attempts.  This number usually will be reset whenever the password is
        /// verified or the account is locked out.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Int32&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<int> GetAccessFailedCountAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>
        /// Returns whether the user can be locked out.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<bool> GetLockoutEnabledAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.LockoutEnabled);
        }

        /// <summary>
        /// Returns the DateTimeOffset that represents the end of a user's lockout, any time in the past should be considered
        /// not locked out.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;DateTimeOffset&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<DateTimeOffset> GetLockoutEndDateAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(
                user.LockoutEndDateUtc.HasValue ?
                    new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc)) :
                    new DateTimeOffset());
        }

        /// <summary>
        /// Used to record when an attempt to access the user has failed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;System.Int32&gt;.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task<int> IncrementAccessFailedCountAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>
        /// Used to reset the access failed count, typically after the account is successfully accessed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task ResetAccessFailedCountAsync(IdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.AccessFailedCount = 0;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets whether the user can be locked out.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="enabled">if set to <c>true</c> [enabled].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetLockoutEnabledAsync(IdentityUser user, bool enabled)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.LockoutEnabled = enabled;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Locks a user out until the specified end date (set to a past date, to unlock a user)
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lockoutEnd">The lockout end.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset lockoutEnd)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.LockoutEndDateUtc = lockoutEnd == DateTimeOffset.MinValue ? null : new DateTime?(lockoutEnd.UtcDateTime);
            return Task.FromResult(0);
        }

        #endregion

        #region -- IDisposable --

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Class RoleStore.
    /// </summary>
    public class RoleStore : IQueryableRoleStore<IdentityRole, int>
    {
        /// <summary>
        /// The database
        /// </summary>
        private readonly GuestBookProjectContext db;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleStore"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        public RoleStore(GuestBookProjectContext db)
        {
            this.db = db;
        }

        #region -- IQueryableRoleStore<UserRole, TKey> --

        /// <summary>
        /// IQueryable Roles
        /// </summary>
        /// <value>The roles.</value>
        public IQueryable<IdentityRole> Roles
        {
            get { return this.db.IdentityRoles; }
        }

        #endregion

        #region -- IRoleStore<UserRole, TKey> --

        /// <summary>
        /// Create a new role
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">role</exception>
        public virtual Task CreateAsync(IdentityRole role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            this.db.IdentityRoles.Add(role);
            return this.db.SaveChangesAsync();
        }

        /// <summary>
        /// Delete a role
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">role</exception>
        public Task DeleteAsync(IdentityRole role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            this.db.IdentityRoles.Remove(role);
            return this.db.SaveChangesAsync();
        }

        /// <summary>
        /// Find a role by id
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        /// <returns>Task&lt;IdentityRole&gt;.</returns>
        public Task<IdentityRole> FindByIdAsync(int roleId)
        {
            return this.db.IdentityRoles.FindAsync(new[] { roleId });
        }

        /// <summary>
        /// Find a role by name
        /// </summary>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Task&lt;IdentityRole&gt;.</returns>
        public Task<IdentityRole> FindByNameAsync(string roleName)
        {
            return this.db.IdentityRoles.FirstOrDefaultAsync(r => r.Name == roleName);
        }

        /// <summary>
        /// Update a role
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">role</exception>
        public Task UpdateAsync(IdentityRole role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            this.db.Entry(role).State = EntityState.Modified;
            return this.db.SaveChangesAsync();
        }

        #endregion

        #region -- IDisposable --

        /// <inheritdoc />
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
        }

        #endregion
    }
}
