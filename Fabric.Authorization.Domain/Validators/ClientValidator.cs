using Fabric.Authorization.Domain.Models;
using FluentValidation;
using System;
using Fabric.Authorization.Domain.Services;

namespace Fabric.Authorization.Domain.Validators
{
    public class ClientValidator : AbstractValidator<Client>
    {
        private readonly ClientService _clientService;

        public ClientValidator(ClientService clientService)
        {
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(client => client.Id)
                .NotEmpty()
                .WithMessage("Please specify an Id for this client")
                .WithState(c => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(client => client.Id)
                .Must(BeUnique)
                .When(client => !string.IsNullOrEmpty(client.Id))
                .WithMessage(c => $"Client {c.Id} already exists. Please provide a new client id.")
                .WithState(c=> ValidationEnums.ValidationState.Duplicate);

            RuleFor(client => client.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this client")
                .WithState(c => ValidationEnums.ValidationState.MissingRequiredField);
        }

        private bool BeUnique(string clientId)
        {
            return !_clientService.Exists(clientId).Result;
        }
    }
}
