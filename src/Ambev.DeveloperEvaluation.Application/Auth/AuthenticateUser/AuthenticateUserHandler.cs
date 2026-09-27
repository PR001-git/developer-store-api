using System.Threading;
using System.Threading.Tasks;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Specifications;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser
{
    public class AuthenticateUserHandler : IRequestHandler<AuthenticateUserCommand, AuthenticateUserResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IMapper _mapper;

        public AuthenticateUserHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
        }

        public async Task<AuthenticateUserResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            // Always verify, even for an unknown email, against a fixed dummy hash of the same cost as a real
            // one. Short-circuiting on user == null would skip BCrypt entirely and answer faster than a known
            // email with a wrong password, letting a caller enumerate which emails are signed up.
            var passwordMatches = _passwordHasher.VerifyPassword(request.Password, user?.Password ?? _passwordHasher.DummyHash);

            if (user == null || !passwordMatches)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var activeUserSpec = new ActiveUserSpecification();
            if (!activeUserSpec.IsSatisfiedBy(user))
            {
                throw new UnauthorizedAccessException("User is not active");
            }

            var result = _mapper.Map<AuthenticateUserResult>(user);
            result.Token = _jwtTokenGenerator.GenerateToken(user);
            return result;
        }
    }
}
