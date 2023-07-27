using FluentValidation;
using NopStation.Plugin.Misc.Core.Models;
using Nop.Web.Framework.Validators;

namespace NopStation.Plugin.Misc.Core.Validators
{
    public class LocaleResourceValidator : BaseNopValidator<CoreLocaleResourceModel>
    {
        public LocaleResourceValidator()
        {
            RuleFor(x => x.ResourceName).NotEmpty().WithMessage("Admin.Configuration.Languages.Resources.Fields.Name.Required");
        }
    }
}
