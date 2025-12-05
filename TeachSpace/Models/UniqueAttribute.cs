using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;
using TeachSpace.Models;

namespace TeachSpace.Attributes
{
    public class UniqueAttribute : ValidationAttribute
    {
        private readonly Type _entityType;
        private readonly string _propertyName;

        // Get Model Name , Property Name Needs To Be Unique
        public UniqueAttribute(Type entityType, string propertyName = "Name")
        {
            _entityType = entityType;
            _propertyName = propertyName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            var newValue = value.ToString();

            var context = (ApplicationDbContext)validationContext.GetService(typeof(ApplicationDbContext));

            // Get Id to be exclude in edit
            var currentIdProperty = validationContext.ObjectInstance.GetType().GetProperty("Id");
            var currentId = (int?)(currentIdProperty?.GetValue(validationContext.ObjectInstance)) ?? 0;

            // Get Entity From DB By EntityType
            var setMethod = context.GetType().GetMethod("Set", Type.EmptyTypes)?.MakeGenericMethod(_entityType);
            var dbSet = (IQueryable<object>)setMethod?.Invoke(context, null);

            if (dbSet == null) return new ValidationResult("Error finding DbSet");


            foreach (var item in dbSet)
            {
                var itemType = item.GetType();
                var itemName = itemType.GetProperty(_propertyName)?.GetValue(item)?.ToString();
                var itemId = (int)itemType.GetProperty("Id")?.GetValue(item);

                if (itemName == newValue && itemId != currentId)
                {
                    return new ValidationResult($"{_propertyName} must be unique!");
                }
            }

            return ValidationResult.Success;
        }
    }
}