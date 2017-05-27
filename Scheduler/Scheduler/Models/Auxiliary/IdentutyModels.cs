using Microsoft.AspNet.Identity;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class CustomUser : IUser<ObjectId>
    {
        public string CustomProperty { get; set; }

        public ObjectId Id { get; }

        public string UserName { get; set; }
    }

    public class AuthorizationManager : UserManager<CustomUser, ObjectId>, IAuthorizationManager
    {
        private readonly ICustomUserMongoRepository repository;
        private readonly ICustomEmailService emailService;
        private readonly ICustomTokenProvider tokenProvider;

        // Parameters being injected by Unity.
        // container.RegisterType<ICustomUserMongoRepository, CustomUserMongoRepository>();
        // ..
        // ..
        public AuthorizationManager(
                     ICustomUserMongoRepository repository,
                     ICustomEmailService emailService,
                     ICustomTokenProvider tokenProvider
                     )
                                     // calling base constructor passing
                                     // a repository which implements
                                     // IUserStore, among others.
                                     : base(repository)
        {
            this.repository = repository;

            // this.EmailService is a property of UserManager and
            // it has to be set to send emails by your class
            this.EmailService = emailService;

            // this.UserTokenProvider is a property of UserManager and
            // it has to be set to generate tokens for user password
            // recovery and confirmation tokens
            this.UserTokenProvider = tokenProvider;
        }
    }
}