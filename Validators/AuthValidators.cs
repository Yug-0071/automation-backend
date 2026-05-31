using AutomationBackend.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AutomationBackend.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");
            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("Mobile number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("A valid mobile number is required.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.MobileOrEmail)
                .NotEmpty().WithMessage("Mobile number or Email is required.")
                .Must(BeAValidEmailOrMobile).WithMessage("Please enter a valid email or mobile number.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }

        private bool BeAValidEmailOrMobile(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Check if it's an email (more robust regex)
            bool isEmail = Regex.IsMatch(input, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            
            // Check if it's a mobile number (allowing optional + and digits only)
            bool isMobile = Regex.IsMatch(input, @"^\+?[0-9]{7,15}$");

            return isEmail || isMobile;
        }
    }
}
