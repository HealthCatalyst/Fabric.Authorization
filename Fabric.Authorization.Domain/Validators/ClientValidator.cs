using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class ClientValidator : AbstractValidator<Client>
    {
        private readonly IClientStore _clientStore;

        public ClientValidator(IClientStore clientStore)
        {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(client => client.Id)
                .NotEmpty()
                .WithMessage("Please specify an Id for this client");

            RuleFor(client => client.Id)
                .Must(BeUnique)
                .When(client => string.IsNullOrEmpty(client.Id));

            RuleFor(client => client.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this client");
        }

        private bool BeUnique(string clientId)
        {
            return !_clientStore.ClientExists(clientId);
        }
    }
}
