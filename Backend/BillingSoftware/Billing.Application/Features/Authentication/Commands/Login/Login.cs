using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;

namespace Billing.Application.Features.Authentication.Commands.Login
{
    public sealed record LoginRequest(string Email,
    string Password) : IRequest<LoginResponse>;

    public sealed record LoginResponse(bool Success,
        string Token,string Message);
    public sealed class Validator : AbstractValidator<LoginRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(6);
        }
    }

    public sealed class Handler : IRequestHandler<LoginRequest, LoginResponse>
    {
        public Task<LoginResponse> Handle(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
