using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser
{
    /// <summary>
    /// Validator for AuthenticateUserCommand. Login only checks presence and format, never password
    /// strength: rejecting a short password before the handler runs would tell an attacker their guess
    /// failed length rules rather than credentials, leaking the sign-up password policy through a 400
    /// instead of the generic 401 a wrong password gets.
    /// </summary>
    public class AuthenticateUserValidator : AbstractValidator<AuthenticateUserCommand>
    {
        public AuthenticateUserValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
