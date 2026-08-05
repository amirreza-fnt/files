namespace FileStorage.Application.Validation;

using FileStorage.Application.Dtos;
using FluentValidation;

public sealed class PrepareUploadRequestValidator : AbstractValidator<PrepareUploadRequest>
{
    public PrepareUploadRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(10L * 1024 * 1024 * 1024); // hard ceiling 10 GB

        RuleFor(x => x.AccessType)
            .IsInEnum();
    }
}